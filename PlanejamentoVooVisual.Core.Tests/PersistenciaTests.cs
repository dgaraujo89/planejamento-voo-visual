using PlanejamentoVooVisual.Core;
using Xunit;

namespace PlanejamentoVooVisual.Core.Tests;

public class PersistenciaTests
{
    [Fact]
    public void RoundTrip_PreservaEntradasEReproduzOsCalculos()
    {
        var original = PlanoReferencia.Criar();

        var json = PlanoPersistencia.Serializar(original);
        var recarregado = PlanoPersistencia.Desserializar(json);

        // As entradas essenciais sobrevivem.
        Assert.Equal(original.Pernas.Count, recarregado.Pernas.Count);
        Assert.Equal(original.Atmosfera.VentoDirGrausMag, recarregado.Atmosfera.VentoDirGrausMag);
        Assert.Equal(original.Combustivel.ReservaMin, recarregado.Combustivel.ReservaMin);
        Assert.Equal(3, recarregado.Perfis.Count);

        // E o cálculo roda após recarregar, preservando a distância da rota.
        var r = CalculadoraNavegacao.Calcular(recarregado);
        Assert.Equal(89.0, r.Totais.DistanciaNm, 0.1);
        Assert.True(r.Combustivel.TotalKg > r.Combustivel.PartidaTaxiKg);
        Assert.True(r.Combustivel.RotaKg > 0);
    }

    [Fact]
    public void Json_NaoContemValoresCalculados()
    {
        var json = PlanoPersistencia.Serializar(PlanoReferencia.Criar());

        // Nada de campos derivados (só entradas são persistidas).
        Assert.DoesNotContain("TasKt", json);
        Assert.DoesNotContain("WcaGraus", json);
        Assert.DoesNotContain("ProaMag", json);
        Assert.DoesNotContain("CombustivelAcumulado", json);
    }

    [Fact]
    public void OverrideNulo_NaoPoluiOJsonMasOverridePreenchidoSobrevive()
    {
        var plano = PlanoReferencia.Criar();
        plano.Pernas[1].IasKtOverride = 125;

        var recarregado = PlanoPersistencia.Desserializar(PlanoPersistencia.Serializar(plano));

        Assert.Equal(125, recarregado.Pernas[1].IasKtOverride);
        Assert.Null(recarregado.Pernas[0].IasKtOverride);
    }

    [Fact]
    public void Json_EnumComoTextoEIndentado()
    {
        var json = PlanoPersistencia.Serializar(PlanoReferencia.Criar());

        Assert.Contains("\"Fase\": \"Cruzeiro\"", json); // enum como texto legível
        Assert.Contains("\n", json);                      // indentado
    }

    [Fact]
    public void ExportarCsv_GeraArquivoComCabecalhoETotais()
    {
        var resultado = CalculadoraNavegacao.Calcular(PlanoReferencia.Criar());
        var caminho = Path.Combine(Path.GetTempPath(), $"navlog_teste_{Guid.NewGuid():N}.csv");

        try
        {
            PlanoPersistencia.ExportarCsv(resultado, caminho);
            var conteudo = File.ReadAllText(caminho);

            Assert.Contains("Proa Mag", conteudo);
            Assert.Contains("TOTAL MÍNIMO A BORDO", conteudo);
        }
        finally
        {
            if (File.Exists(caminho)) File.Delete(caminho);
        }
    }
}
