using System.ComponentModel;
using System.Runtime.CompilerServices;
using PlanejamentoVooVisual.Core;

namespace PlanejamentoVooVisual.ViewModels;

/// <summary>
/// Linha editável da grade de rota. Expõe apenas as entradas da perna
/// (De/Para/Distância/Curso e overrides). Fase e altitudes não são digitadas —
/// o motor as calcula no perfil vertical. Cada edição dispara recálculo.
/// </summary>
public sealed class PernaEdicaoViewModel : INotifyPropertyChanged
{
    public PernaEdicaoViewModel(Perna modelo) => Modelo = modelo;

    public Perna Modelo { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Disparado quando uma entrada muda (exige recálculo do plano).</summary>
    public event EventHandler? EntradaAlterada;

    public int Ordem => Modelo.Ordem;

    public string Para
    {
        get => Modelo.Para;
        set => Definir(v => Modelo.Para = v, Modelo.Para, value);
    }

    public double DistanciaNm
    {
        get => Modelo.DistanciaNm;
        set => Definir(v => Modelo.DistanciaNm = v, Modelo.DistanciaNm, value);
    }

    public double CursoMag
    {
        get => Modelo.CursoMag;
        set => Definir(v => Modelo.CursoMag = v, Modelo.CursoMag, value);
    }

    /// <summary>Altitude alvo a partir desta perna (nulo = mantém o alvo vigente).</summary>
    public double? AltitudeOverrideFt
    {
        get => Modelo.AltitudeOverrideFt;
        set => Definir(v => Modelo.AltitudeOverrideFt = v, Modelo.AltitudeOverrideFt, value);
    }

    // Overrides (nulo = herda do bloco global).
    public double? OatCOverride
    {
        get => Modelo.OatCOverride;
        set => Definir(v => Modelo.OatCOverride = v, Modelo.OatCOverride, value);
    }

    public double? IasKtOverride
    {
        get => Modelo.IasKtOverride;
        set => Definir(v => Modelo.IasKtOverride = v, Modelo.IasKtOverride, value);
    }

    public double? VentoDirOverride
    {
        get => Modelo.VentoDirOverride;
        set => Definir(v => Modelo.VentoDirOverride = v, Modelo.VentoDirOverride, value);
    }

    public double? VentoVelOverride
    {
        get => Modelo.VentoVelOverride;
        set => Definir(v => Modelo.VentoVelOverride = v, Modelo.VentoVelOverride, value);
    }

    public double? ConsumoKgHOverride
    {
        get => Modelo.ConsumoKgHOverride;
        set => Definir(v => Modelo.ConsumoKgHOverride = v, Modelo.ConsumoKgHOverride, value);
    }

    public void ReverterOverrides()
    {
        Modelo.AltitudeOverrideFt = null;
        Modelo.OatCOverride = null;
        Modelo.IasKtOverride = null;
        Modelo.VentoDirOverride = null;
        Modelo.VentoVelOverride = null;
        Modelo.ConsumoKgHOverride = null;
        OnPropertyChanged(string.Empty);
        EntradaAlterada?.Invoke(this, EventArgs.Empty);
    }

    public void AtualizarOrdem() => OnPropertyChanged(nameof(Ordem));

    private void Definir<T>(Action<T> atribuir, T atual, T novo, [CallerMemberName] string? nome = null)
    {
        if (EqualityComparer<T>.Default.Equals(atual, novo)) return;
        atribuir(novo);
        OnPropertyChanged(nome);
        EntradaAlterada?.Invoke(this, EventArgs.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? nome = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
}
