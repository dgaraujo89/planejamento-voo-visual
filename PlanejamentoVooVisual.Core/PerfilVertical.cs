namespace PlanejamentoVooVisual.Core;

/// <summary>Trecho de cálculo derivado das pernas do usuário, já com fase e altitudes.</summary>
internal sealed class Segmento
{
    public required string De { get; init; }
    public required string Para { get; init; }
    public required Fase Fase { get; init; }
    public required double DistanciaNm { get; init; }
    public required double CursoMag { get; init; }
    public required double AltInicialFt { get; init; }
    public required double AltFinalFt { get; init; }
    public required Perna Origem { get; init; }
}

/// <summary>Ponto notável bruto (posição/altitude) antes de calcular o tempo acumulado.</summary>
internal readonly record struct PontoBruto(double DistanciaNm, double AltitudeFt, string? EntreDe, string? EntrePara);

/// <summary>Saída do solver: segmentos e a localização de TOC/TOD.</summary>
internal sealed class PerfilVerticalResultado
{
    public required List<Segmento> Segmentos { get; init; }
    public PontoBruto? Toc { get; init; }
    public PontoBruto? Tod { get; init; }
}

/// <summary>
/// Constrói o perfil vertical: cada perna tem uma altitude-alvo (a de cruzeiro,
/// ou uma sobrescrita que se propaga adiante). O avião sobe/desce até o alvo na
/// razão típica (podendo passar por mais de um ponto) e o mantém; a descida ao
/// destino é antecipada (define o TOD). As pernas são fatiadas nos pontos de
/// transição, com fase por tendência de altitude.
/// </summary>
internal static class PerfilVertical
{
    public const string RotuloToc = "TOC";
    public const string RotuloTod = "TOD";
    public const string RotuloBod = "BOD"; // bottom of descent (fim de uma descida intermediária)
    private const double Eps = 1e-6;

    private readonly record struct Vertice(double Dist, double Alt, string Nome);

    public static PerfilVerticalResultado Construir(PlanoDeVoo plano)
    {
        var legs = plano.Pernas;
        int n = legs.Count;
        if (n == 0)
            return new PerfilVerticalResultado { Segmentos = new List<Segmento>() };

        double elevDep = plano.ElevacaoPartidaFt;
        double elevDest = plano.ElevacaoDestinoFt;
        double climbRate = Math.Abs(plano.PerfilDe(Fase.Subida)?.RazaoTipicaFtMin ?? 0);
        double descRate = Math.Abs(plano.PerfilDe(Fase.Descida)?.RazaoTipicaFtMin ?? 0);
        double iasSubida = plano.PerfilDe(Fase.Subida)?.IasKt ?? 0;
        double iasDescida = plano.PerfilDe(Fase.Descida)?.IasKt ?? 0;

        var cum = new double[n + 1];
        for (int i = 0; i < n; i++) cum[i + 1] = cum[i] + Math.Max(0, legs[i].DistanciaNm);
        double total = cum[n];

        var wpName = new string[n + 1];
        wpName[0] = string.IsNullOrWhiteSpace(plano.OrigemNome) ? "Origem" : plano.OrigemNome;
        for (int i = 0; i < n; i++) wpName[i + 1] = legs[i].Para;

        // Alvo por perna: sobrescrita da perna, senão o alvo vigente (começa no cruzeiro).
        var alvo = new double[n];
        double corrente = plano.AltitudeCruzeiroFt;
        for (int i = 0; i < n; i++)
        {
            corrente = legs[i].AltitudeOverrideFt ?? corrente;
            alvo[i] = corrente;
        }

        // ---- Marcha para frente: sobe/desce até o alvo de cada perna e mantém ----
        var fwd = new List<Vertice> { new(0, elevDep, wpName[0]) };
        var fwdAlt = new double[n + 1];
        fwdAlt[0] = elevDep;
        double cur = elevDep;

        for (int i = 0; i < n; i++)
        {
            double tgt = alvo[i];
            if (Math.Abs(cur - tgt) > 1)
            {
                bool subindo = tgt > cur;
                double rate = subindo ? climbRate : descRate;
                double ias = legs[i].IasKtOverride ?? (subindo ? iasSubida : iasDescida);

                if (rate > 0)
                {
                    double gs = EstimaGs(legs[i], ias, (cur + tgt) / 2, plano);
                    double tempo = Math.Abs(tgt - cur) / rate;
                    double dist = gs * tempo / 60.0;

                    if (dist < legs[i].DistanciaNm - Eps)
                    {
                        fwd.Add(new Vertice(cum[i] + dist, tgt, subindo ? RotuloToc : RotuloBod));
                        cur = tgt;
                    }
                    else
                    {
                        double variacao = rate * (legs[i].DistanciaNm / Math.Max(gs, 1) * 60.0);
                        cur += subindo ? variacao : -variacao;
                    }
                }
                else cur = tgt;
            }
            fwdAlt[i + 1] = cur;
            fwd.Add(new Vertice(cum[i + 1], cur, wpName[i + 1]));
        }

        // ---- Linha de descida ao destino (antecipada), de trás pra frente ----
        double repAlt = (alvo.Max() + elevDest) / 2.0;
        var descAlt = new double[n + 1];
        descAlt[n] = elevDest;
        for (int i = n - 1; i >= 0; i--)
        {
            if (descRate <= 0) { descAlt[i] = Math.Max(alvo.Max(), elevDest); continue; }
            double ias = legs[i].IasKtOverride ?? iasDescida;
            double gs = EstimaGs(legs[i], ias, repAlt, plano);
            double tempo = gs > 0 ? legs[i].DistanciaNm / gs * 60.0 : 0;
            descAlt[i] = descAlt[i + 1] + descRate * tempo;
        }

        double DescLineAt(double d)
        {
            for (int i = 0; i < n; i++)
                if (d >= cum[i] - Eps && d <= cum[i + 1] + Eps && cum[i + 1] > cum[i])
                    return descAlt[i] + (descAlt[i + 1] - descAlt[i]) * (d - cum[i]) / (cum[i + 1] - cum[i]);
            return descAlt[n];
        }

        // ---- TOD: onde o perfil de frente encostaria acima da linha de descida ----
        double? todDist = null, todAlt = null;
        for (int k = 0; k < fwd.Count - 1; k++)
        {
            double d0 = fwd[k].Dist, d1 = fwd[k + 1].Dist;
            if (d1 - d0 < Eps) continue;
            double diff0 = fwd[k].Alt - DescLineAt(d0);
            double diff1 = fwd[k + 1].Alt - DescLineAt(d1);
            if (diff0 <= Eps && diff1 > Eps)
            {
                double frac = diff1 - diff0 < Eps ? 0 : -diff0 / (diff1 - diff0);
                todDist = d0 + frac * (d1 - d0);
                todAlt = DescLineAt(todDist.Value);
                break;
            }
        }

        // ---- Vértices finais: frente até o TOD, depois desce ao destino ----
        var verts = new List<Vertice>();
        if (todDist is double td)
        {
            foreach (var v in fwd)
                if (v.Dist < td - Eps) verts.Add(v);
            verts.Add(new Vertice(td, todAlt!.Value, RotuloTod));
            for (int j = 1; j <= n; j++)
                if (cum[j] > td + Eps)
                    verts.Add(new Vertice(cum[j], j == n ? elevDest : descAlt[j], wpName[j]));
        }
        else
        {
            verts.AddRange(fwd); // sem descida antecipada (ex.: destino no nível de cruzeiro)
        }

        // Remove vértices duplicados/degenerados por distância.
        var limpos = new List<Vertice>();
        foreach (var v in verts)
            if (limpos.Count == 0 || v.Dist > limpos[^1].Dist + Eps)
                limpos.Add(v);

        var segmentos = MontarSegmentos(limpos, legs, cum, n);
        var (toc, tod) = LocalizarTocTod(segmentos, cum, wpName, n);

        return new PerfilVerticalResultado { Segmentos = segmentos, Toc = toc, Tod = tod };
    }

    private static List<Segmento> MontarSegmentos(List<Vertice> verts, IList<Perna> legs, double[] cum, int n)
    {
        var segmentos = new List<Segmento>();
        for (int k = 0; k < verts.Count - 1; k++)
        {
            var a = verts[k];
            var b = verts[k + 1];
            double dist = b.Dist - a.Dist;
            if (dist <= Eps) continue;

            int li = LegDe((a.Dist + b.Dist) / 2, cum, n);
            var leg = legs[li];

            segmentos.Add(new Segmento
            {
                De = a.Nome,
                Para = b.Nome,
                Fase = FasePorTendencia(a.Alt, b.Alt),
                DistanciaNm = dist,
                CursoMag = leg.CursoMag,
                AltInicialFt = a.Alt,
                AltFinalFt = b.Alt,
                Origem = leg
            });
        }
        return segmentos;
    }

    /// <summary>TOC = fim da subida inicial; TOD = início da descida final ao destino.</summary>
    private static (PontoBruto? toc, PontoBruto? tod) LocalizarTocTod(
        List<Segmento> segs, double[] cum, string[] wpName, int n)
    {
        if (segs.Count == 0) return (null, null);

        PontoBruto? toc = null, tod = null;
        double d = 0;
        var acumInicio = new double[segs.Count];
        for (int i = 0; i < segs.Count; i++) { acumInicio[i] = d; d += segs[i].DistanciaNm; }
        double totalDist = d;

        // TOC: fim do primeiro bloco contínuo de subida a partir do início.
        if (segs[0].Fase == Fase.Subida)
        {
            int i = 0;
            while (i < segs.Count && segs[i].Fase == Fase.Subida) i++;
            double tocDist = i < segs.Count ? acumInicio[i] : totalDist;
            if (tocDist > Eps && tocDist < totalDist - Eps)
                toc = MontarPonto(tocDist, segs[i - 1].AltFinalFt, cum, wpName, n);
        }

        // TOD: início do último bloco contínuo de descida que termina no destino.
        if (segs[^1].Fase == Fase.Descida)
        {
            int i = segs.Count - 1;
            while (i >= 0 && segs[i].Fase == Fase.Descida) i--;
            double todDist = acumInicio[i + 1];
            if (todDist > Eps && todDist < totalDist - Eps)
                tod = MontarPonto(todDist, segs[i + 1].AltInicialFt, cum, wpName, n);
        }

        return (toc, tod);
    }

    private static PontoBruto MontarPonto(double dist, double alt, double[] cum, string[] wpName, int n)
    {
        for (int i = 0; i < n; i++)
            if (dist > cum[i] - Eps && dist < cum[i + 1] + Eps)
                return new PontoBruto(dist, alt, wpName[i], wpName[i + 1]);
        return new PontoBruto(dist, alt, null, null);
    }

    private static int LegDe(double dist, double[] cum, int n)
    {
        for (int i = 0; i < n; i++)
            if (dist >= cum[i] - Eps && dist <= cum[i + 1] + Eps)
                return i;
        return n - 1;
    }

    private static Fase FasePorTendencia(double altIni, double altFim)
    {
        if (altFim > altIni + 1) return Fase.Subida;
        if (altFim < altIni - 1) return Fase.Descida;
        return Fase.Cruzeiro;
    }

    private static double EstimaGs(Perna leg, double ias, double altParaTas, PlanoDeVoo plano)
    {
        var atm = plano.Atmosfera;
        double ventoDir = leg.VentoDirOverride ?? atm.VentoDirGrausMag;
        double ventoVel = leg.VentoVelOverride ?? atm.VentoVelKt;
        double oat = leg.OatCOverride ?? Aerodinamica.OatNaAltitude(atm.OatRefC, atm.GradienteCPor1000Ft, atm.AltRefFt, altParaTas);
        double tas = Aerodinamica.Tas(ias, altParaTas, oat);

        var (ok, _, gs) = Aerodinamica.TrianguloVento(tas, ventoVel, ventoDir, leg.CursoMag);
        if (ok && gs > 0) return gs;
        return !double.IsNaN(tas) && tas > 0 ? tas : 1;
    }
}
