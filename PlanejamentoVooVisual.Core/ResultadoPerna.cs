namespace PlanejamentoVooVisual.Core;

/// <summary>
/// Resultado calculado de uma perna. Sempre derivado, nunca persistido.
/// Campos dependentes do triângulo do vento ficam <c>null</c> quando a perna
/// é insolúvel ou incompleta (<see cref="Resolvida"/> = false).
/// </summary>
public sealed record ResultadoPerna
{
    public required int Ordem { get; init; }
    public required string De { get; init; }
    public required string Para { get; init; }
    public required Fase Fase { get; init; }

    // Entradas efetivas usadas (após herança/override), para exibição na grade.
    public double DistanciaNm { get; init; }
    public double CursoMag { get; init; }
    public double AltInicialFt { get; init; }
    public double AltFinalFt { get; init; }
    public double VentoDirGrausMag { get; init; }
    public double VentoVelKt { get; init; }

    // Derivados.
    public double? AltMediaFt { get; init; }
    public double? OatC { get; init; }
    public double? IasKt { get; init; }
    public double? TasKt { get; init; }
    public double? WcaGraus { get; init; }
    public double? ProaMag { get; init; }
    public double? GsKt { get; init; }
    public double? TempoMin { get; init; }
    public double? RazaoExigidaFtMin { get; init; }
    public double? ConsumoKgH { get; init; }
    public double? CombustivelKg { get; init; }

    // Acumulados ao longo da rota (apenas de pernas resolvidas).
    public double? TempoAcumuladoMin { get; init; }
    public double? CombustivelAcumuladoKg { get; init; }

    /// <summary>Verdadeiro quando |razão exigida| excede |razão típica da fase|.</summary>
    public bool RazaoAcimaDoTipico { get; init; }

    /// <summary>Falso quando a perna é insolúvel ou incompleta.</summary>
    public bool Resolvida { get; init; }

    /// <summary>Motivo legível quando <see cref="Resolvida"/> é falso; caso contrário null.</summary>
    public string? Motivo { get; init; }
}
