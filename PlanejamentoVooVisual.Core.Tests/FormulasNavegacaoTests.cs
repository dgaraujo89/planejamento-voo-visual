using PlanejamentoVooVisual.Core;
using Xunit;

namespace PlanejamentoVooVisual.Core.Tests;

/// <summary>
/// Valida as fórmulas de navegação (TAS pela densidade, triângulo do vento, tempo,
/// razão) usando planos nivelados a uma altitude conhecida — assim cada segmento
/// tem altitude fixa e os valores podem ser comparados com a planilha de referência.
/// </summary>
public class FormulasNavegacaoTests
{
    private const double Tol = 0.1;

    /// <summary>Plano nivelado (partida=cruzeiro=destino) na altitude dada, com IAS/consumo de cruzeiro.</summary>
    private static PlanoDeVoo Nivelado(double altFt, double ias, double consumo,
        double ventoDir = 270, double ventoVel = 15)
    {
        var plano = new PlanoDeVoo
        {
            ElevacaoPartidaFt = altFt,
            AltitudeCruzeiroFt = altFt,
            ElevacaoDestinoFt = altFt,
            Atmosfera = new CondicoesAtmosfera
            {
                VentoDirGrausMag = ventoDir, VentoVelKt = ventoVel,
                AltRefFt = 2400, OatRefC = 22, GradienteCPor1000Ft = 1.98
            },
            Combustivel = new ParametrosCombustivel()
        };
        plano.Perfis.Add(new PerfilFase { Fase = Fase.Subida, IasKt = 80, ConsumoKgH = 30, RazaoTipicaFtMin = 700 });
        plano.Perfis.Add(new PerfilFase { Fase = Fase.Cruzeiro, IasKt = ias, ConsumoKgH = consumo, RazaoTipicaFtMin = 0 });
        plano.Perfis.Add(new PerfilFase { Fase = Fase.Descida, IasKt = 120, ConsumoKgH = 15, RazaoTipicaFtMin = -500 });
        return plano;
    }

    [Fact]
    public void Cruzeiro5500_CursosDaReferencia_BatemTasWcaGsTempo()
    {
        var plano = Nivelado(5500, ias: 110, consumo: 22);
        plano.Pernas.Add(new Perna { Para = "C", DistanciaNm = 30, CursoMag = 274 });
        plano.Pernas.Add(new Perna { Para = "D", DistanciaNm = 14, CursoMag = 251 });

        var r = CalculadoraNavegacao.Calcular(plano);

        var p2 = r.Pernas[0];
        Assert.Equal(15.86, p2.OatC!.Value, Tol);
        Assert.Equal(121.91, p2.TasKt!.Value, Tol);
        Assert.Equal(-0.49, p2.WcaGraus!.Value, Tol);
        Assert.Equal(273.5, p2.ProaMag!.Value, Tol);
        Assert.Equal(106.94, p2.GsKt!.Value, Tol);
        Assert.Equal(16.83, p2.TempoMin!.Value, Tol);
        Assert.Equal(0.0, p2.RazaoExigidaFtMin!.Value, Tol); // nivelado

        var p3 = r.Pernas[1];
        Assert.Equal(121.91, p3.TasKt!.Value, Tol);
        Assert.Equal(2.30, p3.WcaGraus!.Value, Tol);
        Assert.Equal(253.3, p3.ProaMag!.Value, Tol);
        Assert.Equal(107.63, p3.GsKt!.Value, Tol);
        Assert.Equal(7.80, p3.TempoMin!.Value, Tol);
    }

    [Theory]
    [InlineData(3950, 80, 86.59)]   // TAS da subida de referência (altMédia 3950)
    [InlineData(3850, 120, 129.68)] // TAS da descida de referência (altMédia 3850)
    [InlineData(5500, 110, 121.91)] // TAS de cruzeiro
    public void Tas_PelaDensidade_BateEmVariasAltitudes(double alt, double ias, double tasEsperada)
    {
        var plano = Nivelado(alt, ias, consumo: 20);
        plano.Pernas.Add(new Perna { Para = "Y", DistanciaNm = 10, CursoMag = 0, VentoVelOverride = 0 });

        var r = CalculadoraNavegacao.Calcular(plano).Pernas[0];

        Assert.Equal(tasEsperada, r.TasKt!.Value, Tol);
    }

    [Fact]
    public void VentoDeCauda_GsMaiorQueTas()
    {
        var plano = Nivelado(3000, 100, 20, ventoDir: 180, ventoVel: 25);
        plano.Pernas.Add(new Perna { Para = "Y", DistanciaNm = 60, CursoMag = 0 });

        var r = CalculadoraNavegacao.Calcular(plano).Pernas[0];

        Assert.True(r.Resolvida);
        Assert.True(r.GsKt > r.TasKt);
    }

    [Fact]
    public void VentoDeTravesPuro_WcaPositivaEGsMenorQueTas()
    {
        var plano = Nivelado(3000, 100, 20, ventoDir: 90, ventoVel: 20);
        plano.Pernas.Add(new Perna { Para = "Y", DistanciaNm = 50, CursoMag = 0 });

        var r = CalculadoraNavegacao.Calcular(plano).Pernas[0];

        Assert.True(r.Resolvida);
        Assert.True(r.WcaGraus > 0);
        Assert.True(r.GsKt < r.TasKt);
    }

    [Fact]
    public void VentoMaisForteQueTas_SegmentoInsoluvel_ForaDosTotais()
    {
        var plano = Nivelado(3000, 100, 20, ventoDir: 90, ventoVel: 200);
        plano.Pernas.Add(new Perna { Para = "Y", DistanciaNm = 40, CursoMag = 0 });

        var r = CalculadoraNavegacao.Calcular(plano);

        Assert.False(r.Pernas[0].Resolvida);
        Assert.Null(r.Pernas[0].GsKt);
        Assert.False(string.IsNullOrWhiteSpace(r.Pernas[0].Motivo));
        Assert.Equal(0, r.Totais.DistanciaNm, Tol);
    }

    [Fact]
    public void RotaVazia_TotaisZerados_SemExcecao()
    {
        var r = CalculadoraNavegacao.Calcular(Nivelado(3000, 100, 20));

        Assert.Empty(r.Pernas);
        Assert.Equal(0, r.Totais.DistanciaNm);
        Assert.Equal(0, r.Combustivel.RotaKg);
        Assert.Null(r.Toc);
        Assert.Null(r.Tod);
    }

    [Fact]
    public void Combustivel_AlternativaEReservaUsamConsumoDeCruzeiro()
    {
        var plano = Nivelado(3000, 100, consumo: 20);
        plano.Combustivel.AlternativaMin = 60;
        plano.Combustivel.ReservaMin = 30;
        plano.Pernas.Add(new Perna { Para = "Y", DistanciaNm = 20, CursoMag = 0 });

        var c = CalculadoraNavegacao.Calcular(plano).Combustivel;

        Assert.Equal(20, c.AlternativaKg, Tol); // 60 min × 20 kg/h
        Assert.Equal(10, c.ReservaKg, Tol);     // 30 min × 20 kg/h
    }
}
