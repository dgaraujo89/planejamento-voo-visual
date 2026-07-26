using System.ComponentModel;
using PlanejamentoVooVisual.Core;

namespace PlanejamentoVooVisual.ViewModels;

/// <summary>
/// Linha do navlog calculado. Os dados de cálculo vêm do <see cref="ResultadoPerna"/>
/// (somente leitura); a única coisa editável é <see cref="Cumprido"/>, marcada pelo
/// piloto para indicar por onde já passou.
/// </summary>
public sealed class SegmentoLinhaViewModel : INotifyPropertyChanged
{
    public SegmentoLinhaViewModel(ResultadoPerna r) => R = r;

    public ResultadoPerna R { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _cumprido;

    /// <summary>Marcado pelo piloto ao passar por este trecho (destaca a linha).</summary>
    public bool Cumprido
    {
        get => _cumprido;
        set
        {
            if (_cumprido == value) return;
            _cumprido = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Cumprido)));
        }
    }

    public int Ordem => R.Ordem;
    public string De => R.De;
    public string Para => R.Para;
    public Fase Fase => R.Fase;
    public double DistanciaNm => R.DistanciaNm;
    public double CursoMag => R.CursoMag;

    public double? AltInicialFt => R.AltInicialFt;
    public double? AltMediaFt => R.AltMediaFt;
    public double? AltFinalFt => R.AltFinalFt;
    public double? OatC => R.OatC;
    public double? IasKt => R.IasKt;
    public double? VentoDirGrausMag => R.VentoDirGrausMag;
    public double? VentoVelKt => R.VentoVelKt;
    public double? TasKt => R.TasKt;
    public double? WcaGraus => R.WcaGraus;
    public double? ProaMag => R.ProaMag;
    public double? GsKt => R.GsKt;
    public double? TempoMin => R.TempoMin;
    public double? RazaoExigidaFtMin => R.RazaoExigidaFtMin;
    public double? ConsumoKgH => R.ConsumoKgH;
    public double? CombustivelKg => R.CombustivelKg;
    public double? TempoAcumuladoMin => R.TempoAcumuladoMin;
    public double? CombustivelAcumuladoKg => R.CombustivelAcumuladoKg;

    public bool RazaoAcimaDoTipico => R.RazaoAcimaDoTipico;
    public bool Resolvida => R.Resolvida;
    public string? Motivo => R.Motivo;

    /// <summary>Verdadeiro quando esta linha começa ou termina num ponto TOC/TOD (para destaque).</summary>
    public bool EhPontoNotavel =>
        Ehmarcador(De) || Ehmarcador(Para);

    private static bool Ehmarcador(string wp)
        => wp.Contains("TOC", StringComparison.Ordinal) || wp.Contains("TOD", StringComparison.Ordinal);
}
