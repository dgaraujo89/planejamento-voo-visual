using PlanejamentoVooVisual.Core;
using Xunit;

namespace PlanejamentoVooVisual.Core.Tests;

/// <summary>
/// Testa o perfil vertical calculado: TOC/TOD localizados pela razão (podendo
/// cair no meio de uma perna), fatiamento em segmentos e fases automáticas.
/// </summary>
public class PerfilVerticalTests
{
    [Fact]
    public void Referencia_DivideAsPernasNoTocENoTod()
    {
        var r = CalculadoraNavegacao.Calcular(PlanoReferencia.Criar());

        // Subida termina cedo (razão 700 ft/min) → TOC dentro da 1ª perna (A→B).
        Assert.NotNull(r.Toc);
        Assert.Equal("A", r.Toc!.EntreDe);
        Assert.Equal("B", r.Toc.EntrePara);
        Assert.InRange(r.Toc.DistanciaAcumuladaNm, 3, 9);
        Assert.Equal(5500, r.Toc.AltitudeFt, 1);

        // Descida começa perto do fim (razão 500 ft/min) → TOD dentro da última perna (D→E).
        Assert.NotNull(r.Tod);
        Assert.Equal("D", r.Tod!.EntreDe);
        Assert.Equal("E", r.Tod.EntrePara);
        Assert.InRange(r.Tod.DistanciaAcumuladaNm, 72, 80);
        Assert.Equal(5500, r.Tod.AltitudeFt, 1);
    }

    [Fact]
    public void Referencia_GeraExatamenteUmaSubidaEUmaDescida()
    {
        var r = CalculadoraNavegacao.Calcular(PlanoReferencia.Criar());

        Assert.Single(r.Pernas, p => p.Fase == Fase.Subida);
        Assert.Single(r.Pernas, p => p.Fase == Fase.Descida);
        Assert.Contains(r.Pernas, p => p.Fase == Fase.Cruzeiro);

        // A subida vai de A até o waypoint sintético "TOC".
        var subida = r.Pernas.Single(p => p.Fase == Fase.Subida);
        Assert.Equal("A", subida.De);
        Assert.Equal("TOC", subida.Para);

        // A descida vai do "TOD" até E.
        var descida = r.Pernas.Single(p => p.Fase == Fase.Descida);
        Assert.Equal("TOD", descida.De);
        Assert.Equal("E", descida.Para);
    }

    [Fact]
    public void Referencia_DistanciaTotalPreservada()
    {
        var r = CalculadoraNavegacao.Calcular(PlanoReferencia.Criar());
        Assert.Equal(89.0, r.Totais.DistanciaNm, 0.1); // 25+30+14+20, segmentos somados
    }

    [Fact]
    public void SubidaSpanMultiplasPernas_QuandoRazaoEBaixa()
    {
        // Razão de subida bem baixa: a subida não termina na 1ª perna.
        var plano = PlanoReferencia.Criar();
        plano.PerfilDe(Fase.Subida)!.RazaoTipicaFtMin = 200; // sobe devagar
        plano.AltitudeCruzeiroFt = 9500;                     // e mais alto

        var r = CalculadoraNavegacao.Calcular(plano);

        // Deve haver segmentos de subida em mais de uma perna original (Para != "TOC" em alguma subida).
        int subidas = r.Pernas.Count(p => p.Fase == Fase.Subida);
        Assert.True(subidas >= 2, $"esperava subida em várias pernas, veio {subidas}");
        Assert.NotNull(r.Toc);
    }

    [Fact]
    public void Nivelado_SemSubidaNemDescida_NaoTemTocNemTod()
    {
        var plano = PlanoReferencia.Criar();
        plano.ElevacaoPartidaFt = 5500;
        plano.ElevacaoDestinoFt = 5500; // tudo em cruzeiro

        var r = CalculadoraNavegacao.Calcular(plano);

        Assert.Null(r.Toc);
        Assert.Null(r.Tod);
        Assert.All(r.Pernas, p => Assert.Equal(Fase.Cruzeiro, p.Fase));
    }

    [Fact]
    public void RotaCurta_SobeEJaDesce_PicoSemCruzeiro()
    {
        // Cruzeiro alto e rota curta: não dá tempo de nivelar → pico (TOC=TOD).
        var plano = new PlanoDeVoo
        {
            ElevacaoPartidaFt = 1000,
            AltitudeCruzeiroFt = 15000,
            ElevacaoDestinoFt = 1000,
            Atmosfera = new CondicoesAtmosfera { AltRefFt = 0, OatRefC = 15, GradienteCPor1000Ft = 1.98 },
            Combustivel = new ParametrosCombustivel()
        };
        plano.Perfis.Add(new PerfilFase { Fase = Fase.Subida, IasKt = 80, ConsumoKgH = 30, RazaoTipicaFtMin = 700 });
        plano.Perfis.Add(new PerfilFase { Fase = Fase.Cruzeiro, IasKt = 110, ConsumoKgH = 22 });
        plano.Perfis.Add(new PerfilFase { Fase = Fase.Descida, IasKt = 120, ConsumoKgH = 15, RazaoTipicaFtMin = -500 });
        plano.Pernas.Add(new Perna { Para = "Y", DistanciaNm = 10, CursoMag = 0, VentoVelOverride = 0 });

        var r = CalculadoraNavegacao.Calcular(plano);

        Assert.DoesNotContain(r.Pernas, p => p.Fase == Fase.Cruzeiro); // não nivela
        Assert.Contains(r.Pernas, p => p.Fase == Fase.Subida);
        Assert.Contains(r.Pernas, p => p.Fase == Fase.Descida);
        Assert.NotNull(r.Toc);
    }

    [Fact]
    public void RazaoExigidaDaSubida_RefliteORazaoTipico()
    {
        var r = CalculadoraNavegacao.Calcular(PlanoReferencia.Criar());
        var subida = r.Pernas.Single(p => p.Fase == Fase.Subida);

        // Razão exigida deve ser positiva e próxima da típica (subida projetada pela razão).
        Assert.True(subida.RazaoExigidaFtMin > 0);
        Assert.InRange(subida.RazaoExigidaFtMin!.Value, 650, 750); // ~700 ft/min
    }

    [Fact]
    public void OverrideDeAltitude_SubaEMantemNasPernasSeguintes()
    {
        // Sobrescreve a 2ª perna (B→C) para 7500: o avião sobe a 7500 e mantém.
        var plano = PlanoReferencia.Criar();
        plano.Pernas[1].AltitudeOverrideFt = 7500;

        var r = CalculadoraNavegacao.Calcular(plano);

        // O maior nível atingido passa a ser 7500 (não mais 5500).
        Assert.Equal(7500, r.Pernas.Max(p => p.AltFinalFt), 1);

        // Deve haver uma subida inicial (até 5500) e um step-up (até 7500): 2+ subidas.
        Assert.True(r.Pernas.Count(p => p.Fase == Fase.Subida) >= 2);

        // Há cruzeiro mantido em 7500 depois do step-up.
        Assert.Contains(r.Pernas, p => p.Fase == Fase.Cruzeiro && Math.Abs(p.AltInicialFt - 7500) < 1);

        // O destino continua descendo até a elevação de destino (não fica em 7500).
        var ultima = r.Pernas[^1];
        Assert.Equal(Fase.Descida, ultima.Fase);
        Assert.Equal(2200, ultima.AltFinalFt, 1);
    }

    [Fact]
    public void OverrideDeAltitude_PropagaAdianteSemRepetirEmCadaPerna()
    {
        var plano = PlanoReferencia.Criar();
        plano.Pernas[1].AltitudeOverrideFt = 7500; // só na 2ª perna

        var r = CalculadoraNavegacao.Calcular(plano);

        // Um segmento de cruzeiro numa perna posterior (3ª/4ª) também está em 7500,
        // mesmo sem override próprio — a altitude foi propagada.
        var cruzeirosAltos = r.Pernas.Where(p => p.Fase == Fase.Cruzeiro && Math.Abs(p.AltInicialFt - 7500) < 1).ToList();
        Assert.True(cruzeirosAltos.Count >= 2, "o nível sobrescrito deve valer em mais de uma perna");
    }

    [Fact]
    public void TocContinuaSendoASubidaInicial_MesmoComStepUp()
    {
        var plano = PlanoReferencia.Criar();
        plano.Pernas[1].AltitudeOverrideFt = 7500;

        var r = CalculadoraNavegacao.Calcular(plano);

        // O TOC do resumo é o topo da subida inicial (ao cruzeiro 5500, entre A e B).
        Assert.NotNull(r.Toc);
        Assert.Equal("A", r.Toc!.EntreDe);
        Assert.Equal("B", r.Toc.EntrePara);
        Assert.Equal(5500, r.Toc.AltitudeFt, 1);
    }
}
