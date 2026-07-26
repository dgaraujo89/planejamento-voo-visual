using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Win32;
using PlanejamentoVooVisual.Core;
using PlanejamentoVooVisual.ViewModels;

namespace PlanejamentoVooVisual;

/// <summary>
/// Janela principal. Liga o <see cref="MainViewModel"/> à UI e trata o que é
/// inerente à View: diálogos de arquivo e o aviso de alterações não salvas.
/// </summary>
public partial class MainWindow : Window
{
    private const string FiltroPlano = "Plano de voo VFR (*.vfrplan)|*.vfrplan|Todos os arquivos (*.*)|*.*";
    private const string FiltroCsv = "CSV (*.csv)|*.csv";

    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        // Exibição em pt-BR (vírgula decimal); parsing/serialização continuam invariant.
        Language = XmlLanguage.GetLanguage("pt-BR");

        InitializeComponent();
        DataContext = _vm;

        _vm.SolicitarNovo += (_, _) => Novo();
        _vm.SolicitarAbrir += (_, _) => Abrir();
        _vm.SolicitarSalvar += (_, _) => Salvar();
        _vm.SolicitarSalvarComo += (_, _) => SalvarComo();
        _vm.SolicitarExportarCsv += (_, _) => ExportarCsv();
        _vm.SolicitarAbrirIvao += (_, url) => AbrirIvao(url);
    }

    private void AbrirIvao(string url)
    {
        try
        {
            Clipboard.SetText(url);
        }
        catch
        {
            // Área de transferência indisponível não deve impedir a abertura do navegador.
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Não foi possível abrir o navegador. O link foi copiado para a área de transferência — cole-o no navegador.\n\n{ex.Message}",
                "Exportar para IVAO", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Novo()
    {
        if (!ConfirmarDescarte()) return;
        _vm.CarregarPlano(PlanoDeVoo.Novo());
        _vm.CaminhoArquivo = null;
    }

    private void Abrir()
    {
        if (!ConfirmarDescarte()) return;

        var dialogo = new OpenFileDialog { Filter = FiltroPlano, DefaultExt = PlanoPersistencia.Extensao };
        if (dialogo.ShowDialog(this) != true) return;

        try
        {
            var plano = PlanoPersistencia.Abrir(dialogo.FileName);
            _vm.CarregarPlano(plano);
            _vm.CaminhoArquivo = dialogo.FileName;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível abrir o arquivo:\n{ex.Message}",
                "Abrir", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool Salvar()
    {
        if (_vm.CaminhoArquivo is null)
            return SalvarComo();

        return GravarEm(_vm.CaminhoArquivo);
    }

    private bool SalvarComo()
    {
        var dialogo = new SaveFileDialog
        {
            Filter = FiltroPlano,
            DefaultExt = PlanoPersistencia.Extensao,
            FileName = _vm.CaminhoArquivo ?? "plano" + PlanoPersistencia.Extensao
        };
        if (dialogo.ShowDialog(this) != true) return false;

        if (!GravarEm(dialogo.FileName)) return false;
        _vm.CaminhoArquivo = dialogo.FileName;
        return true;
    }

    private bool GravarEm(string caminho)
    {
        try
        {
            PlanoPersistencia.Salvar(_vm.Plano, caminho);
            _vm.MarcarSalvo();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível salvar:\n{ex.Message}",
                "Salvar", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
    }

    private void ExportarCsv()
    {
        var dialogo = new SaveFileDialog { Filter = FiltroCsv, DefaultExt = ".csv", FileName = "navlog.csv" };
        if (dialogo.ShowDialog(this) != true) return;

        try
        {
            var resultado = CalculadoraNavegacao.Calcular(_vm.Plano);
            PlanoPersistencia.ExportarCsv(resultado, dialogo.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível exportar:\n{ex.Message}",
                "Exportar CSV", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>Se houver alterações não salvas, pergunta ao usuário. Falso = cancelar a ação.</summary>
    private bool ConfirmarDescarte()
    {
        if (!_vm.EstaSujo) return true;

        var r = MessageBox.Show(this,
            "Há alterações não salvas. Deseja salvá-las antes de continuar?",
            "Alterações não salvas", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

        return r switch
        {
            MessageBoxResult.Yes => Salvar(),
            MessageBoxResult.No => true,
            _ => false
        };
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!ConfirmarDescarte())
            e.Cancel = true;
        base.OnClosing(e);
    }
}
