namespace PlanejamentoVooVisual.Core;

/// <summary>Regras de voo do plano ICAO (campo 8 do formulário IVAO/ICAO).</summary>
public enum RegrasVoo
{
    /// <summary>Visual (V).</summary>
    VFR,
    /// <summary>Por instrumentos (I).</summary>
    IFR,
    /// <summary>IFR primeiro, depois VFR (Y).</summary>
    Y,
    /// <summary>VFR primeiro, depois IFR (Z).</summary>
    Z
}
