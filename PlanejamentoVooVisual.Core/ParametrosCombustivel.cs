namespace PlanejamentoVooVisual.Core;

/// <summary>Parâmetros do bloco de combustível adicional.</summary>
public sealed class ParametrosCombustivel : ObservableObject
{
    private double _partidaTaxiKg;
    private double _reservaMin;
    private double _contingenciaPercentual;
    private double _alternativaMin;

    /// <summary>Combustível de partida e táxi, em kg.</summary>
    public double PartidaTaxiKg
    {
        get => _partidaTaxiKg;
        set => SetProperty(ref _partidaTaxiKg, value);
    }

    /// <summary>Reserva final, em minutos.</summary>
    public double ReservaMin
    {
        get => _reservaMin;
        set => SetProperty(ref _reservaMin, value);
    }

    /// <summary>Contingência como fração do combustível de rota (0,05 = 5%).</summary>
    public double ContingenciaPercentual
    {
        get => _contingenciaPercentual;
        set => SetProperty(ref _contingenciaPercentual, value);
    }

    /// <summary>Combustível para a alternativa, em minutos.</summary>
    public double AlternativaMin
    {
        get => _alternativaMin;
        set => SetProperty(ref _alternativaMin, value);
    }
}
