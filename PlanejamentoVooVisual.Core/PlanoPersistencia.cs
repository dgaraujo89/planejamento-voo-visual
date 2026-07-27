using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlanejamentoVooVisual.Core;

/// <summary>
/// Salva e abre planos em JSON (extensão .vfrplan) e exporta a rota para CSV.
/// Persiste apenas as entradas — nunca os valores calculados. Serialização em
/// invariant culture; enums como texto para legibilidade.
/// </summary>
public static class PlanoPersistencia
{
    public const string Extensao = ".vfrplan";

    private static readonly JsonSerializerOptions Opcoes = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serializar(PlanoDeVoo plano)
        => JsonSerializer.Serialize(plano, Opcoes);

    public static PlanoDeVoo Desserializar(string json)
        => JsonSerializer.Deserialize<PlanoDeVoo>(json, Opcoes)
           ?? throw new InvalidDataException("Arquivo de plano vazio ou inválido.");

    public static void Salvar(PlanoDeVoo plano, string caminho)
        => File.WriteAllText(caminho, Serializar(plano), new UTF8Encoding(false));

    public static PlanoDeVoo Abrir(string caminho)
        => Desserializar(File.ReadAllText(caminho));

    /// <summary>Exporta a rota calculada para CSV (separador ';' e números pt-BR, p/ Excel).</summary>
    public static void ExportarCsv(ResultadoPlano resultado, string caminho)
    {
        var pt = CultureInfo.GetCultureInfo("pt-BR");
        var sb = new StringBuilder();

        string[] cabecalho =
        {
            "Nº", "De", "Para", "Fase", "Dist (NM)", "Curso Mag", "Alt ini (ft)", "Alt fim (ft)",
            "Alt méd (ft)", "OAT (°C)", "IAS (kt)", "Vento dir", "Vento vel (kt)", "TAS (kt)",
            "WCA (°)", "Proa Mag", "GS (kt)", "Tempo (min)", "Razão (ft/min)", "Consumo (kg/h)",
            "Comb (kg)", "Tempo acum (min)", "Comb acum (kg)", "Situação"
        };
        sb.AppendLine(string.Join(';', cabecalho));

        string N(double? v, string f = "0.0") => v.HasValue ? v.Value.ToString(f, pt) : "";

        foreach (var p in resultado.Pernas)
        {
            string[] campos =
            {
                p.Ordem.ToString(pt), Escapar(p.De), Escapar(p.Para), p.Fase.ToString(),
                N(p.DistanciaNm), N(p.CursoMag), N(p.AltInicialFt, "0"), N(p.AltFinalFt, "0"),
                N(p.AltMediaFt, "0"), N(p.OatC, "0.0"), N(p.IasKt, "0"), N(p.VentoDirGrausMag, "0"),
                N(p.VentoVelKt, "0"), N(p.TasKt, "0.0"), N(p.WcaGraus, "0.0"), N(p.ProaMag, "0.0"),
                N(p.GsKt, "0.0"), N(p.TempoMin, "0.00"), N(p.RazaoExigidaFtMin, "0"), N(p.ConsumoKgH, "0.0"),
                N(p.CombustivelKg, "0.00"), N(p.TempoAcumuladoMin, "0.00"), N(p.CombustivelAcumuladoKg, "0.00"),
                p.Resolvida ? "OK" : $"INSOLÚVEL: {p.Motivo}"
            };
            sb.AppendLine(string.Join(';', campos));
        }

        var t = resultado.Totais;
        sb.AppendLine();
        sb.AppendLine($"TOTAIS;;;;{t.DistanciaNm.ToString("0.0", pt)};;;;;;;;;;;;{t.GsMediaKt.ToString("0.0", pt)};{t.TempoMin.ToString("0.00", pt)};;;{t.CombustivelKg.ToString("0.00", pt)}");

        var c = resultado.Combustivel;
        sb.AppendLine();
        sb.AppendLine("COMBUSTÍVEL (kg)");
        sb.AppendLine($"Partida/táxi;{c.PartidaTaxiKg.ToString("0.00", pt)}");
        sb.AppendLine($"Rota;{c.RotaKg.ToString("0.00", pt)}");
        sb.AppendLine($"Contingência;{c.ContingenciaKg.ToString("0.00", pt)}");
        if (resultado.Alternado is { Resolvida: true } alt)
            sb.AppendLine($"Alternativa ({Escapar(alt.Para)}, {alt.DistanciaNm.ToString("0", pt)} NM, {alt.TempoMin!.Value.ToString("0", pt)} min);{c.AlternativaKg.ToString("0.00", pt)}");
        else
            sb.AppendLine($"Alternativa;{c.AlternativaKg.ToString("0.00", pt)}");
        sb.AppendLine($"Reserva;{c.ReservaKg.ToString("0.00", pt)}");
        sb.AppendLine($"TOTAL MÍNIMO A BORDO;{c.TotalKg.ToString("0.00", pt)}");

        if (resultado.Toc is not null || resultado.Tod is not null)
        {
            sb.AppendLine();
            sb.AppendLine("PONTOS NOTÁVEIS;Entre;Dist acum (NM);Tempo acum (min);Alt (ft);Rest (NM)");
            if (resultado.Toc is { } toc)
                sb.AppendLine($"TOC (topo de subida);{Escapar(Entre(toc))};{toc.DistanciaAcumuladaNm.ToString("0.0", pt)};{toc.TempoAcumuladoMin.ToString("0.00", pt)};{toc.AltitudeFt.ToString("0", pt)};{toc.DistanciaRestanteNm.ToString("0.0", pt)}");
            if (resultado.Tod is { } tod)
                sb.AppendLine($"TOD (início de descida);{Escapar(Entre(tod))};{tod.DistanciaAcumuladaNm.ToString("0.0", pt)};{tod.TempoAcumuladoMin.ToString("0.00", pt)};{tod.AltitudeFt.ToString("0", pt)};{tod.DistanciaRestanteNm.ToString("0.0", pt)}");
        }

        File.WriteAllText(caminho, sb.ToString(), new UTF8Encoding(true)); // BOM p/ Excel abrir acentos
    }

    private static string Entre(PontoNotavel p)
        => p.EntreDe is not null && p.EntrePara is not null ? $"{p.EntreDe} → {p.EntrePara}" : p.Waypoint;

    private static string Escapar(string? campo)
    {
        campo ??= "";
        if (campo.Contains(';') || campo.Contains('"') || campo.Contains('\n'))
            return '"' + campo.Replace("\"", "\"\"") + '"';
        return campo;
    }
}
