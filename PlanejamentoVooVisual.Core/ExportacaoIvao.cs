using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PlanejamentoVooVisual.Core;

/// <summary>
/// Gera a URL que abre o formulário de plano de voo da IVAO já pré-preenchido.
/// A IVAO não importa arquivos: o plano vive no sistema web (fpl.ivao.aero) e é
/// pré-carregado via <c>?flightPlan=&lt;JSON-em-Base64&gt;</c> — a página faz
/// <c>atob()</c> + <c>JSON.parse</c>, então o JSON PRECISA vir em Base64 (passar o
/// JSON cru quebra a página). Todos os campos são opcionais; o piloto revisa e envia
/// no próprio site. Classe pura, sem dependência de UI.
/// Referência: https://wiki.ivao.aero/en/home/devops/api/flightplan
/// </summary>
public static class ExportacaoIvao
{
    public const string BaseUrl = "https://fpl.ivao.aero/flight-plans/create";

    /// <summary>Monta a URL de pré-preenchimento a partir do plano e do resultado calculado.</summary>
    public static string GerarUrl(PlanoDeVoo plano, ResultadoPlano resultado)
    {
        ArgumentNullException.ThrowIfNull(plano);
        ArgumentNullException.ThrowIfNull(resultado);

        var fp = new Dictionary<string, object>();

        Adicionar(fp, "callsign", plano.Aeronave);
        fp["flightRules"] = LetraRegras(plano.Regras);
        fp["flightType"] = "G"; // aviação geral (padrão razoável para VFR)
        fp["aircraftNumber"] = 1;
        Adicionar(fp, "aircraftId", plano.AeronaveIcaoTipo);

        Adicionar(fp, "departureId", Codigo(plano.OrigemNome));
        Adicionar(fp, "arrivalId", Codigo(plano.DestinoNome));
        if (plano.Pernas.Count > 0)
            fp["route"] = MontarRota(plano);

        var eobt = ParseEobtSegundos(plano.EobtUtc);
        if (eobt.HasValue) fp["departureTime"] = eobt.Value;

        var vel = VelocidadeCruzeiroKt(plano, resultado);
        if (vel > 0)
        {
            fp["cruisingSpeedType"] = "N";
            fp["cruisingSpeed"] = vel;
        }

        var nivel = NivelCentenasDePes(plano);
        if (nivel > 0)
        {
            fp["altitudeType"] = "A";
            fp["altitude"] = nivel;
        }

        int eet = (int)Math.Round(resultado.Totais.TempoMin * 60.0);
        if (eet > 0) fp["eet"] = eet;

        if (plano.PessoasABordo > 0) fp["pob"] = plano.PessoasABordo;

        string json = JsonSerializer.Serialize(fp);
        // A IVAO exige o JSON em Base64 (ela faz atob() na página). O Base64 é então
        // percent-encoded para blindar os caracteres '+', '/' e '=' na query string.
        string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return $"{BaseUrl}?flightPlan={Uri.EscapeDataString(base64)}";
    }

    private static void Adicionar(Dictionary<string, object> fp, string chave, string valor)
    {
        if (!string.IsNullOrWhiteSpace(valor))
            fp[chave] = valor.Trim();
    }

    private static string LetraRegras(RegrasVoo r) => r switch
    {
        RegrasVoo.VFR => "V",
        RegrasVoo.IFR => "I",
        RegrasVoo.Y => "Y",
        RegrasVoo.Z => "Z",
        _ => "V"
    };

    private static string Codigo(string? s) => (s ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>Rota = waypoints intermediários (exclui partida e destino), ligados por DCT.</summary>
    private static string MontarRota(PlanoDeVoo plano)
    {
        // O "Para" de cada perna, menos o da última (que é o destino).
        var intermediarios = plano.Pernas
            .Take(plano.Pernas.Count - 1)
            .Select(p => Codigo(p.Para))
            .Where(s => s.Length > 0)
            .ToList();

        return intermediarios.Count > 0 ? string.Join(" DCT ", intermediarios) : "DCT";
    }

    /// <summary>EOBT "HH:mm" ou "HHmm" → segundos desde a meia-noite UTC; null se vazio/inválido.</summary>
    private static int? ParseEobtSegundos(string? eobt)
    {
        if (string.IsNullOrWhiteSpace(eobt)) return null;
        string s = eobt.Trim().Replace(":", "");
        if (s.Length is 3 or 4 && int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out int hhmm))
        {
            int h = hhmm / 100, m = hhmm % 100;
            if (h is >= 0 and < 24 && m is >= 0 and < 60)
                return h * 3600 + m * 60;
        }
        return null;
    }

    private static int VelocidadeCruzeiroKt(PlanoDeVoo plano, ResultadoPlano resultado)
    {
        var cruzeiro = resultado.Pernas
            .Where(p => p.Resolvida && p.Fase == Fase.Cruzeiro && p.TasKt.HasValue)
            .Select(p => p.TasKt!.Value)
            .ToList();
        if (cruzeiro.Count > 0) return (int)Math.Round(cruzeiro.Average());

        // Sem perna de cruzeiro resolvida: usa a maior TAS disponível, senão a IAS do perfil.
        var tas = resultado.Pernas.Where(p => p.Resolvida && p.TasKt.HasValue).Select(p => p.TasKt!.Value).ToList();
        if (tas.Count > 0) return (int)Math.Round(tas.Max());

        return (int)Math.Round(plano.PerfilDe(Fase.Cruzeiro)?.IasKt ?? 0);
    }

    private static int NivelCentenasDePes(PlanoDeVoo plano)
        => (int)Math.Round(plano.AltitudeCruzeiroFt / 100.0);
}
