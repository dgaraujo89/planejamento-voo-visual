using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlanejamentoVooVisual.Core;

/// <summary>
/// Base leve com <see cref="INotifyPropertyChanged"/> para as entradas do plano.
/// Vive no Core porque <see cref="INotifyPropertyChanged"/> pertence a
/// <c>System.ComponentModel</c> (biblioteca base), não à UI — o motor de cálculo
/// continua puro, apenas lendo propriedades. Permite que a WPF recalcule a cada edição.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T campo, T valor, [CallerMemberName] string? nome = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
            return false;
        campo = valor;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
        return true;
    }

    protected void RaisePropertyChanged([CallerMemberName] string? nome = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
}
