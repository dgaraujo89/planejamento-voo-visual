namespace PlanejamentoVooVisual.Core;

/// <summary>Perfil de desempenho por fase de voo (Subida, Cruzeiro ou Descida).</summary>
public sealed class PerfilFase : ObservableObject
{
    private Fase _fase;
    private double _iasKt;
    private double _consumoKgH;
    private double _razaoTipicaFtMin;

    /// <summary>Fase à qual este perfil se aplica.</summary>
    public Fase Fase
    {
        get => _fase;
        set => SetProperty(ref _fase, value);
    }

    /// <summary>Velocidade indicada típica da fase, em kt.</summary>
    public double IasKt
    {
        get => _iasKt;
        set => SetProperty(ref _iasKt, value);
    }

    /// <summary>Consumo típico da fase, em kg/h.</summary>
    public double ConsumoKgH
    {
        get => _consumoKgH;
        set => SetProperty(ref _consumoKgH, value);
    }

    /// <summary>Razão de subida/descida típica da fase, em ft/min (0 para cruzeiro).</summary>
    public double RazaoTipicaFtMin
    {
        get => _razaoTipicaFtMin;
        set => SetProperty(ref _razaoTipicaFtMin, value);
    }
}
