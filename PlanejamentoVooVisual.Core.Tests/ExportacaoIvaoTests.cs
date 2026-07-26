using System.Text.Json;
using PlanejamentoVooVisual.Core;
using Xunit;

namespace PlanejamentoVooVisual.Core.Tests;

public class ExportacaoIvaoTests
{
    private const string Base = "https://fpl.ivao.aero/flight-plans/create?flightPlan=";

    private static (string url, JsonElement fp) Gerar(Action<PlanoDeVoo>? ajuste = null)
    {
        var plano = PlanoReferencia.Criar();
        plano.Aeronave = "PT-ABC";
        plano.AeronaveIcaoTipo = "C172";
        plano.Regras = RegrasVoo.VFR;
        plano.EobtUtc = "13:00";
        plano.PessoasABordo = 2;
        ajuste?.Invoke(plano);

        var resultado = CalculadoraNavegacao.Calcular(plano);
        var url = ExportacaoIvao.GerarUrl(plano, resultado);

        Assert.StartsWith(Base, url);
        // A IVAO espera o JSON em Base64 (percent-encoded na query). Desfaz as duas camadas.
        var base64 = Uri.UnescapeDataString(url.Substring(Base.Length));
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        var fp = JsonDocument.Parse(json).RootElement;
        return (url, fp);
    }

    [Fact]
    public void Url_TemBaseDaIvaoEParametroFlightPlan()
    {
        var (url, _) = Gerar();
        Assert.Contains("fpl.ivao.aero/flight-plans/create?flightPlan=", url);
    }

    [Fact]
    public void FlightPlan_EstaCodificadoEmBase64_NaoJsonCru()
    {
        // Regressão: a IVAO faz atob() na página; passar o JSON cru quebra o site.
        var (url, _) = Gerar();
        var payload = Uri.UnescapeDataString(url.Substring(Base.Length));
        Assert.DoesNotContain("{", payload); // não é JSON cru
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        Assert.StartsWith("{", json.TrimStart()); // decodifica para JSON válido
    }

    [Fact]
    public void DepartureEArrival_SaemDoPrimeiroEUltimoWaypoint()
    {
        // O fixture de referência usa os waypoints A→B→C→D→E.
        var (_, fp) = Gerar();
        Assert.Equal("A", fp.GetProperty("departureId").GetString());
        Assert.Equal("E", fp.GetProperty("arrivalId").GetString());
    }

    [Fact]
    public void Rota_ContemWaypointsIntermediariosComDct()
    {
        var (_, fp) = Gerar();
        var rota = fp.GetProperty("route").GetString();
        // Intermediários B, C, D ligados por DCT; partida (A) e destino (E) fora.
        Assert.Equal("B DCT C DCT D", rota);
        Assert.DoesNotContain("A DCT", rota);
        Assert.DoesNotContain("DCT E", rota);
    }

    [Fact]
    public void CamposEssenciais_SaoPreenchidos()
    {
        var (_, fp) = Gerar();
        Assert.Equal("V", fp.GetProperty("flightRules").GetString());
        Assert.Equal("C172", fp.GetProperty("aircraftId").GetString());
        Assert.Equal("PT-ABC", fp.GetProperty("callsign").GetString());
        Assert.Equal(2, fp.GetProperty("pob").GetInt32());
        Assert.Equal(13 * 3600, fp.GetProperty("departureTime").GetInt32()); // 13:00 UTC em segundos
    }

    [Fact]
    public void VelocidadeENivel_SaemDoCruzeiro()
    {
        var (_, fp) = Gerar();
        // TAS de cruzeiro ~122 kt; nível 5500 ft → 55 (centenas de pés).
        Assert.Equal("N", fp.GetProperty("cruisingSpeedType").GetString());
        Assert.InRange(fp.GetProperty("cruisingSpeed").GetInt32(), 118, 126);
        Assert.Equal("A", fp.GetProperty("altitudeType").GetString());
        Assert.Equal(55, fp.GetProperty("altitude").GetInt32());
    }

    [Fact]
    public void Eet_ReflicteOTempoTotalEmSegundos()
    {
        var (_, fp) = Gerar();
        // Tempo total da rota (perfil calculado) — algumas dezenas de minutos.
        Assert.InRange(fp.GetProperty("eet").GetInt32(), 2400, 3600);
    }

    [Fact]
    public void Ifr_MapeiaParaLetraI()
    {
        var (_, fp) = Gerar(p => p.Regras = RegrasVoo.IFR);
        Assert.Equal("I", fp.GetProperty("flightRules").GetString());
    }

    [Fact]
    public void EobtVazio_NaoIncluiDepartureTime()
    {
        var (_, fp) = Gerar(p => p.EobtUtc = "");
        Assert.False(fp.TryGetProperty("departureTime", out _));
    }
}
