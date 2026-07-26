namespace PlanejamentoVooVisual.Core;

/// <summary>
/// Funções aerodinâmicas puras (densidade ISA, TAS e triângulo do vento),
/// compartilhadas pelo solver de perfil vertical e pelo cálculo por segmento.
/// Trigonometria em radianos; ângulos de entrada em graus.
/// </summary>
internal static class Aerodinamica
{
    private const double DensidadeIsaNivelMar = 1.225;   // kg/m³
    private const double ConstanteGasArSeco = 287.05;    // J/(kg·K)
    private const double ZeroAbsolutoC = 273.15;

    public static double OatNaAltitude(double oatRef, double gradiente, double altRef, double altMedia)
        => oatRef - gradiente * (altMedia - altRef) / 1000.0;

    public static double Densidade(double altMedia, double oatC)
    {
        double pressaoHpa = 1013.25 * Math.Pow(1 - 0.00000687535 * altMedia, 5.2559);
        return pressaoHpa * 100 / (ConstanteGasArSeco * (oatC + ZeroAbsolutoC));
    }

    public static double Tas(double ias, double altMedia, double oatC)
    {
        double densidade = Densidade(altMedia, oatC);
        if (densidade <= 0) return double.NaN;
        return ias * Math.Sqrt(DensidadeIsaNivelMar / densidade);
    }

    /// <summary>
    /// Resolve o triângulo do vento. Retorna <c>ok=false</c> quando a perna é
    /// insolúvel (vento &gt; TAS no ângulo, ou GS não-positiva).
    /// </summary>
    public static (bool ok, double wca, double gs) TrianguloVento(
        double tas, double ventoVel, double ventoDir, double cursoMag)
    {
        if (tas <= 0 || double.IsNaN(tas) || double.IsInfinity(tas))
            return (false, 0, 0);

        double delta = ventoDir - cursoMag;
        double seno = ventoVel * SenGraus(delta) / tas;
        if (Math.Abs(seno) > 1)
            return (false, 0, 0);

        double wca = GrausDe(Math.Asin(seno));
        double gs = tas * CosGraus(wca) - ventoVel * CosGraus(delta);
        if (gs <= 0 || double.IsNaN(gs) || double.IsInfinity(gs))
            return (false, wca, gs);

        return (true, wca, gs);
    }

    public static double SenGraus(double graus) => Math.Sin(graus * Math.PI / 180.0);
    public static double CosGraus(double graus) => Math.Cos(graus * Math.PI / 180.0);
    public static double GrausDe(double radianos) => radianos * 180.0 / Math.PI;

    /// <summary>Normaliza um ângulo para 0–359 (aceita 0–360; 360 vira 0).</summary>
    public static double Normalizar360(double graus)
    {
        double m = graus % 360.0;
        if (m < 0) m += 360.0;
        return m;
    }
}
