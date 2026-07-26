namespace PlanejamentoVooVisual.Core;

/// <summary>
/// Vento e temperatura de referência do plano. Direção do vento em graus
/// magnéticos (o usuário sempre digita magnético — não há declinação).
/// </summary>
public sealed class CondicoesAtmosfera : ObservableObject
{
    private double _ventoDirGrausMag;
    private double _ventoVelKt;
    private double _altRefFt;
    private double _oatRefC;
    private double _gradienteCPor1000Ft = 1.98;

    /// <summary>Direção do vento, em graus magnéticos (0–359).</summary>
    public double VentoDirGrausMag
    {
        get => _ventoDirGrausMag;
        set => SetProperty(ref _ventoDirGrausMag, value);
    }

    /// <summary>Velocidade do vento, em kt.</summary>
    public double VentoVelKt
    {
        get => _ventoVelKt;
        set => SetProperty(ref _ventoVelKt, value);
    }

    /// <summary>Altitude de referência da OAT informada, em ft.</summary>
    public double AltRefFt
    {
        get => _altRefFt;
        set => SetProperty(ref _altRefFt, value);
    }

    /// <summary>Temperatura do ar de referência, em °C.</summary>
    public double OatRefC
    {
        get => _oatRefC;
        set => SetProperty(ref _oatRefC, value);
    }

    /// <summary>Gradiente térmico, em °C por 1.000 ft (ISA = 1,98).</summary>
    public double GradienteCPor1000Ft
    {
        get => _gradienteCPor1000Ft;
        set => SetProperty(ref _gradienteCPor1000Ft, value);
    }
}
