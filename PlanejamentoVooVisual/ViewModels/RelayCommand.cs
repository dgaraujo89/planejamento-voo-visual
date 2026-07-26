using System.Windows.Input;

namespace PlanejamentoVooVisual.ViewModels;

/// <summary>ICommand simples, sem dependências externas.</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _executar;
    private readonly Predicate<object?>? _podeExecutar;

    public RelayCommand(Action<object?> executar, Predicate<object?>? podeExecutar = null)
    {
        _executar = executar ?? throw new ArgumentNullException(nameof(executar));
        _podeExecutar = podeExecutar;
    }

    public RelayCommand(Action executar, Func<bool>? podeExecutar = null)
        : this(_ => executar(), podeExecutar is null ? null : _ => podeExecutar())
    {
    }

    public bool CanExecute(object? parameter) => _podeExecutar?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _executar(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
