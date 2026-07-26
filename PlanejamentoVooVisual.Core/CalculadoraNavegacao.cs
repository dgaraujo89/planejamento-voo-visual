namespace PlanejamentoVooVisual.Core;

/// <summary>
/// Motor de cálculo de navegação VFR. Classe pura, determinística e sem qualquer
/// dependência de UI. Primeiro monta o perfil vertical (dividindo as pernas nos
/// pontos de TOC/TOD via <see cref="PerfilVertical"/>) e então calcula cada
/// segmento: TAS, WCA, proa, GS, tempo, razão e combustível.
/// </summary>
public static class CalculadoraNavegacao
{
    /// <summary>Calcula o plano inteiro: segmentos, totais, quebra por fase, combustível e TOC/TOD.</summary>
    public static ResultadoPlano Calcular(PlanoDeVoo plano)
    {
        ArgumentNullException.ThrowIfNull(plano);

        var perfil = PerfilVertical.Construir(plano);

        var pernas = new List<ResultadoPerna>(perfil.Segmentos.Count);
        double tempoAcum = 0;
        double combAcum = 0;
        int ordem = 1;

        foreach (var seg in perfil.Segmentos)
        {
            var r = CalcularSegmento(seg, plano, ordem++);

            if (r.Resolvida)
            {
                tempoAcum += r.TempoMin!.Value;
                combAcum += r.CombustivelKg!.Value;
                r = r with { TempoAcumuladoMin = tempoAcum, CombustivelAcumuladoKg = combAcum };
            }

            pernas.Add(r);
        }

        var totais = CalcularTotais(pernas);
        var porFase = CalcularPorFase(pernas);
        var combustivel = CalcularCombustivel(plano, totais.CombustivelKg);

        double totalGeometrico = perfil.Segmentos.Sum(s => s.DistanciaNm);
        var toc = MontarPontoNotavel(perfil.Toc, PerfilVertical.RotuloToc, pernas, totalGeometrico, totais.TempoMin);
        var tod = MontarPontoNotavel(perfil.Tod, PerfilVertical.RotuloTod, pernas, totalGeometrico, totais.TempoMin);

        return new ResultadoPlano
        {
            Pernas = pernas,
            Totais = totais,
            PorFase = porFase,
            Combustivel = combustivel,
            Toc = toc,
            Tod = tod
        };
    }

    private static ResultadoPerna CalcularSegmento(Segmento seg, PlanoDeVoo plano, int ordem)
    {
        var origem = seg.Origem;
        var perfil = plano.PerfilDe(seg.Fase);
        var atm = plano.Atmosfera;

        double ventoDir = origem.VentoDirOverride ?? atm.VentoDirGrausMag;
        double ventoVel = origem.VentoVelOverride ?? atm.VentoVelKt;
        double ias = origem.IasKtOverride ?? perfil?.IasKt ?? 0;
        double consumoKgH = origem.ConsumoKgHOverride ?? perfil?.ConsumoKgH ?? 0;
        double razaoTipica = perfil?.RazaoTipicaFtMin ?? 0;

        double altMedia = (seg.AltInicialFt + seg.AltFinalFt) / 2.0;
        double oat = origem.OatCOverride
                     ?? Aerodinamica.OatNaAltitude(atm.OatRefC, atm.GradienteCPor1000Ft, atm.AltRefFt, altMedia);

        ResultadoPerna Base(bool resolvida, string? motivo) => new()
        {
            Ordem = ordem,
            De = seg.De,
            Para = seg.Para,
            Fase = seg.Fase,
            DistanciaNm = seg.DistanciaNm,
            CursoMag = seg.CursoMag,
            AltInicialFt = seg.AltInicialFt,
            AltFinalFt = seg.AltFinalFt,
            VentoDirGrausMag = ventoDir,
            VentoVelKt = ventoVel,
            AltMediaFt = altMedia,
            OatC = oat,
            IasKt = ias,
            ConsumoKgH = consumoKgH,
            Resolvida = resolvida,
            Motivo = motivo
        };

        double densidade = Aerodinamica.Densidade(altMedia, oat);
        if (seg.DistanciaNm <= 0 || ias <= 0 || densidade <= 0)
            return Base(false, "Segmento incompleto (distância, IAS ou densidade inválida).");

        double tas = Aerodinamica.Tas(ias, altMedia, oat);
        if (tas <= 0 || double.IsNaN(tas) || double.IsInfinity(tas))
            return Base(false, "TAS inválida.");

        var (ok, wca, gs) = Aerodinamica.TrianguloVento(tas, ventoVel, ventoDir, seg.CursoMag);
        if (!ok)
        {
            string motivo = gs <= 0
                ? "Vento de proa mais forte que a TAS — velocidade no solo não-positiva."
                : "Vento mais forte que a TAS neste ângulo — segmento insolúvel.";
            return Base(false, motivo);
        }

        double proaMag = Aerodinamica.Normalizar360(seg.CursoMag + wca);
        double tempoMin = seg.DistanciaNm / gs * 60.0;
        double combustivelKg = tempoMin / 60.0 * consumoKgH;
        double razaoExig = (seg.AltFinalFt - seg.AltInicialFt) / tempoMin; // ft/min; 0 se nivelado
        // Os segmentos são projetados NA razão típica; só sinaliza excesso real (margem de 10%).
        bool razaoAcima = razaoTipica != 0 && Math.Abs(razaoExig) > Math.Abs(razaoTipica) * 1.10;

        if (double.IsNaN(tempoMin) || double.IsInfinity(tempoMin))
            return Base(false, "Tempo inválido.");

        return Base(true, null) with
        {
            TasKt = tas,
            WcaGraus = wca,
            ProaMag = proaMag,
            GsKt = gs,
            TempoMin = tempoMin,
            RazaoExigidaFtMin = razaoExig,
            CombustivelKg = combustivelKg,
            RazaoAcimaDoTipico = razaoAcima
        };
    }

    private static ResultadoTotais CalcularTotais(IReadOnlyList<ResultadoPerna> pernas)
    {
        double dist = 0, tempo = 0, comb = 0;
        foreach (var p in pernas)
        {
            if (!p.Resolvida) continue;
            dist += p.DistanciaNm;
            tempo += p.TempoMin!.Value;
            comb += p.CombustivelKg!.Value;
        }

        double gsMedia = tempo > 0 ? dist / (tempo / 60.0) : 0;
        return new ResultadoTotais { DistanciaNm = dist, TempoMin = tempo, CombustivelKg = comb, GsMediaKt = gsMedia };
    }

    private static IReadOnlyList<ResultadoFase> CalcularPorFase(IReadOnlyList<ResultadoPerna> pernas)
    {
        var fases = new[] { Fase.Subida, Fase.Cruzeiro, Fase.Descida };
        var lista = new List<ResultadoFase>(fases.Length);

        foreach (var fase in fases)
        {
            double dist = 0, tempo = 0, comb = 0;
            foreach (var p in pernas)
            {
                if (!p.Resolvida || p.Fase != fase) continue;
                dist += p.DistanciaNm;
                tempo += p.TempoMin!.Value;
                comb += p.CombustivelKg!.Value;
            }
            lista.Add(new ResultadoFase { Fase = fase, DistanciaNm = dist, TempoMin = tempo, CombustivelKg = comb });
        }

        return lista;
    }

    private static ResultadoCombustivel CalcularCombustivel(PlanoDeVoo plano, double combustivelRotaKg)
    {
        var param = plano.Combustivel;
        double consumoCruzeiro = plano.PerfilDe(Fase.Cruzeiro)?.ConsumoKgH ?? 0;

        double contingencia = combustivelRotaKg * param.ContingenciaPercentual;
        double alternativa = param.AlternativaMin / 60.0 * consumoCruzeiro;
        double reserva = param.ReservaMin / 60.0 * consumoCruzeiro;
        double total = param.PartidaTaxiKg + combustivelRotaKg + contingencia + alternativa + reserva;
        double autonomia = consumoCruzeiro > 0 ? (total - param.PartidaTaxiKg) / consumoCruzeiro : 0;

        return new ResultadoCombustivel
        {
            PartidaTaxiKg = param.PartidaTaxiKg,
            RotaKg = combustivelRotaKg,
            ContingenciaKg = contingencia,
            AlternativaKg = alternativa,
            ReservaKg = reserva,
            TotalKg = total,
            AutonomiaHoras = autonomia
        };
    }

    /// <summary>Converte o ponto bruto do solver em <see cref="PontoNotavel"/>, somando o tempo até ele.</summary>
    private static PontoNotavel? MontarPontoNotavel(
        PontoBruto? bruto, string rotulo, IReadOnlyList<ResultadoPerna> pernas, double totalDist, double totalTempo)
    {
        if (bruto is not PontoBruto p) return null;

        double cumDist = 0, cumTempo = 0;
        foreach (var seg in pernas)
        {
            double proxima = cumDist + seg.DistanciaNm;
            if (proxima > p.DistanciaNm + 1e-6) break;
            cumDist = proxima;
            if (seg.Resolvida) cumTempo += seg.TempoMin!.Value;
        }

        return new PontoNotavel
        {
            Waypoint = rotulo,
            EntreDe = p.EntreDe,
            EntrePara = p.EntrePara,
            DistanciaAcumuladaNm = p.DistanciaNm,
            TempoAcumuladoMin = cumTempo,
            AltitudeFt = p.AltitudeFt,
            DistanciaRestanteNm = totalDist - p.DistanciaNm,
            TempoRestanteMin = totalTempo - cumTempo
        };
    }
}
