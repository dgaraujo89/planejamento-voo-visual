namespace PlanejamentoVooVisual.Core;

/// <summary>Totais de uma fase de voo (Subida, Cruzeiro ou Descida).</summary>
public sealed class ResultadoFase
{
    public required Fase Fase { get; init; }
    public double DistanciaNm { get; init; }
    public double TempoMin { get; init; }
    public double CombustivelKg { get; init; }
}

/// <summary>Totais da rota inteira.</summary>
public sealed class ResultadoTotais
{
    public double DistanciaNm { get; init; }
    public double TempoMin { get; init; }
    public double CombustivelKg { get; init; }

    /// <summary>GS média = distância total ÷ (tempo total ÷ 60); 0 se sem tempo.</summary>
    public double GsMediaKt { get; init; }
}

/// <summary>Bloco de combustível mínimo a bordo.</summary>
public sealed class ResultadoCombustivel
{
    public double PartidaTaxiKg { get; init; }
    public double RotaKg { get; init; }
    public double ContingenciaKg { get; init; }
    public double AlternativaKg { get; init; }
    public double ReservaKg { get; init; }

    /// <summary>Combustível mínimo total a bordo, em kg.</summary>
    public double TotalKg { get; init; }

    /// <summary>Autonomia = (total − partida/táxi) ÷ consumo de cruzeiro, em horas.</summary>
    public double AutonomiaHoras { get; init; }
}

/// <summary>
/// Ponto notável da rota (TOC — topo de subida, ou TOD — início de descida),
/// localizado por waypoint, acumulados desde a partida e altitude.
/// </summary>
public sealed class PontoNotavel
{
    /// <summary>Rótulo do ponto ("TOC" ou "TOD").</summary>
    public required string Waypoint { get; init; }

    /// <summary>Waypoint real imediatamente antes do ponto (para "entre X e Y").</summary>
    public string? EntreDe { get; init; }

    /// <summary>Waypoint real imediatamente depois do ponto (para "entre X e Y").</summary>
    public string? EntrePara { get; init; }

    /// <summary>Distância acumulada desde a partida, em NM.</summary>
    public double DistanciaAcumuladaNm { get; init; }

    /// <summary>Tempo acumulado desde a partida, em minutos.</summary>
    public double TempoAcumuladoMin { get; init; }

    /// <summary>Altitude no ponto, em ft.</summary>
    public double AltitudeFt { get; init; }

    /// <summary>Distância restante até o destino a partir deste ponto, em NM.</summary>
    public double DistanciaRestanteNm { get; init; }

    /// <summary>Tempo restante até o destino a partir deste ponto, em minutos.</summary>
    public double TempoRestanteMin { get; init; }
}

/// <summary>Resultado completo do plano — pernas, totais, quebra por fase e combustível.</summary>
public sealed class ResultadoPlano
{
    public required IReadOnlyList<ResultadoPerna> Pernas { get; init; }
    public required ResultadoTotais Totais { get; init; }
    public required IReadOnlyList<ResultadoFase> PorFase { get; init; }
    public required ResultadoCombustivel Combustivel { get; init; }

    /// <summary>Topo de subida (fim da última perna de subida); null se não há subida.</summary>
    public PontoNotavel? Toc { get; init; }

    /// <summary>Início de descida (começo da primeira perna de descida); null se não há descida.</summary>
    public PontoNotavel? Tod { get; init; }

    /// <summary>Perna calculada do destino ao alternado (cruzeiro); null se não há alternado.</summary>
    public ResultadoPerna? Alternado { get; init; }
}
