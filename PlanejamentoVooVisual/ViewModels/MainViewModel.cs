using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PlanejamentoVooVisual.Core;

namespace PlanejamentoVooVisual.ViewModels;

/// <summary>
/// View-model principal. Mantém o <see cref="PlanoDeVoo"/>, assina as mudanças de
/// todos os blocos de entrada e recalcula imediatamente. As pernas editáveis
/// ficam em <see cref="Pernas"/>; o navlog calculado (segmentos com TOC/TOD) em
/// <see cref="Segmentos"/>.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly List<PerfilFase> _perfisAssinados = new();

    public MainViewModel()
    {
        NovoCommand = new RelayCommand(() => SolicitarNovo?.Invoke(this, EventArgs.Empty));
        AbrirCommand = new RelayCommand(() => SolicitarAbrir?.Invoke(this, EventArgs.Empty));
        SalvarCommand = new RelayCommand(() => SolicitarSalvar?.Invoke(this, EventArgs.Empty));
        SalvarComoCommand = new RelayCommand(() => SolicitarSalvarComo?.Invoke(this, EventArgs.Empty));
        ExportarCsvCommand = new RelayCommand(() => SolicitarExportarCsv?.Invoke(this, EventArgs.Empty));
        ExportarIvaoCommand = new RelayCommand(ExportarIvao, () => Pernas.Count > 0);

        AdicionarPernaCommand = new RelayCommand(AdicionarPerna);
        RemoverPernaCommand = new RelayCommand(RemoverPerna, () => Selecionada is not null);
        DuplicarPernaCommand = new RelayCommand(DuplicarPerna, () => Selecionada is not null);
        SubirPernaCommand = new RelayCommand(SubirPerna, () => Selecionada is not null && Pernas.IndexOf(Selecionada) > 0);
        DescerPernaCommand = new RelayCommand(DescerPerna, () => Selecionada is not null && Pernas.IndexOf(Selecionada) < Pernas.Count - 1);
        ReverterOverridesCommand = new RelayCommand(() => Selecionada?.ReverterOverrides(), () => Selecionada is not null);

        CarregarPlano(PlanoExemplo());
    }

    // ---- Comandos de arquivo (a View trata os diálogos) ----
    public RelayCommand NovoCommand { get; }
    public RelayCommand AbrirCommand { get; }
    public RelayCommand SalvarCommand { get; }
    public RelayCommand SalvarComoCommand { get; }
    public RelayCommand ExportarCsvCommand { get; }
    public RelayCommand ExportarIvaoCommand { get; }

    public event EventHandler? SolicitarNovo;
    public event EventHandler? SolicitarAbrir;
    public event EventHandler? SolicitarSalvar;
    public event EventHandler? SolicitarSalvarComo;
    public event EventHandler? SolicitarExportarCsv;
    public event EventHandler<string>? SolicitarAbrirIvao;

    private void ExportarIvao()
    {
        if (_resultado is null) Recalcular();
        var url = ExportacaoIvao.GerarUrl(Plano, _resultado!);
        SolicitarAbrirIvao?.Invoke(this, url);
    }

    // ---- Comandos de pernas ----
    public RelayCommand AdicionarPernaCommand { get; }
    public RelayCommand RemoverPernaCommand { get; }
    public RelayCommand DuplicarPernaCommand { get; }
    public RelayCommand SubirPernaCommand { get; }
    public RelayCommand DescerPernaCommand { get; }
    public RelayCommand ReverterOverridesCommand { get; }

    /// <summary>Pernas editáveis (a rota que o usuário lança).</summary>
    public ObservableCollection<PernaEdicaoViewModel> Pernas { get; } = new();

    /// <summary>Navlog calculado: um segmento por linha, com TOC/TOD já inseridos.</summary>
    public ObservableCollection<SegmentoLinhaViewModel> Segmentos { get; } = new();

    private PlanoDeVoo _plano = null!;
    public PlanoDeVoo Plano
    {
        get => _plano;
        private set { _plano = value; OnPropertyChanged(); }
    }

    private PernaEdicaoViewModel? _selecionada;
    public PernaEdicaoViewModel? Selecionada
    {
        get => _selecionada;
        set { _selecionada = value; OnPropertyChanged(); }
    }

    private bool _estaSujo;
    public bool EstaSujo
    {
        get => _estaSujo;
        private set { _estaSujo = value; OnPropertyChanged(); OnPropertyChanged(nameof(Titulo)); }
    }

    private string? _caminhoArquivo;
    public string? CaminhoArquivo
    {
        get => _caminhoArquivo;
        set { _caminhoArquivo = value; OnPropertyChanged(); OnPropertyChanged(nameof(Titulo)); }
    }

    public string Titulo
    {
        get
        {
            string nome = CaminhoArquivo is null ? "Sem título" : System.IO.Path.GetFileName(CaminhoArquivo);
            return $"{(EstaSujo ? "*" : "")}{nome} — Planejamento de Voo VFR";
        }
    }

    // ---- Resumo ----
    private ResultadoPlano? _resultado;
    public double TotalDistanciaNm => _resultado?.Totais.DistanciaNm ?? 0;
    public double TotalTempoMin => _resultado?.Totais.TempoMin ?? 0;
    public double TotalCombustivelKg => _resultado?.Totais.CombustivelKg ?? 0;
    public double GsMediaKt => _resultado?.Totais.GsMediaKt ?? 0;

    public ResultadoFase? FaseSubida => Fase(Core.Fase.Subida);
    public ResultadoFase? FaseCruzeiro => Fase(Core.Fase.Cruzeiro);
    public ResultadoFase? FaseDescida => Fase(Core.Fase.Descida);
    private ResultadoFase? Fase(Fase f) => _resultado?.PorFase.FirstOrDefault(x => x.Fase == f);

    public PontoNotavel? Toc => _resultado?.Toc;
    public PontoNotavel? Tod => _resultado?.Tod;
    public bool TemToc => _resultado?.Toc is not null;
    public bool TemTod => _resultado?.Tod is not null;

    public double CombPartidaTaxiKg => _resultado?.Combustivel.PartidaTaxiKg ?? 0;
    public double CombRotaKg => _resultado?.Combustivel.RotaKg ?? 0;
    public double CombContingenciaKg => _resultado?.Combustivel.ContingenciaKg ?? 0;
    public double CombAlternativaKg => _resultado?.Combustivel.AlternativaKg ?? 0;
    public double CombReservaKg => _resultado?.Combustivel.ReservaKg ?? 0;
    public double CombTotalKg => _resultado?.Combustivel.TotalKg ?? 0;
    public double AutonomiaHoras => _resultado?.Combustivel.AutonomiaHoras ?? 0;

    /// <summary>Há um alternado calculado (distância informada) para exibir?</summary>
    public bool TemAlternado => _resultado?.Alternado is not null;

    /// <summary>Resumo da perna do alternado: "SBAX: 22 NM · 12 min · 7,3 kg".</summary>
    public string AlternadoResumo
    {
        get
        {
            var a = _resultado?.Alternado;
            if (a is null) return string.Empty;

            var pt = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
            string nome = string.IsNullOrWhiteSpace(a.Para) ? "Alternado" : a.Para;
            if (!a.Resolvida)
                return $"{nome}: trecho insolúvel";
            return string.Format(pt, "{0}: {1:0} NM · {2:0} min · {3:0.0} kg",
                nome, a.DistanciaNm, a.TempoMin, a.CombustivelKg);
        }
    }

    /// <summary>Substitui o plano atual (Novo/Abrir) e recalcula.</summary>
    public void CarregarPlano(PlanoDeVoo plano)
    {
        DesassinarTudo();
        Plano = plano;

        Pernas.Clear();
        foreach (var perna in plano.Pernas)
            Pernas.Add(CriarPernaVm(perna));

        AssinarBlocos();
        Recalcular();
        EstaSujo = false;
    }

    private void AssinarBlocos()
    {
        Plano.PropertyChanged += OnEntradaAlterada;
        Plano.Atmosfera.PropertyChanged += OnEntradaAlterada;
        Plano.Combustivel.PropertyChanged += OnEntradaAlterada;
        Plano.Perfis.CollectionChanged += OnColecaoAlterada;
        Plano.Pernas.CollectionChanged += OnColecaoAlterada;
        foreach (var perfil in Plano.Perfis)
        {
            perfil.PropertyChanged += OnEntradaAlterada;
            _perfisAssinados.Add(perfil);
        }
    }

    private void DesassinarTudo()
    {
        if (_plano is null) return;
        Plano.PropertyChanged -= OnEntradaAlterada;
        Plano.Atmosfera.PropertyChanged -= OnEntradaAlterada;
        Plano.Combustivel.PropertyChanged -= OnEntradaAlterada;
        Plano.Perfis.CollectionChanged -= OnColecaoAlterada;
        Plano.Pernas.CollectionChanged -= OnColecaoAlterada;
        foreach (var perfil in _perfisAssinados)
            perfil.PropertyChanged -= OnEntradaAlterada;
        _perfisAssinados.Clear();
        foreach (var linha in Pernas)
            linha.EntradaAlterada -= OnEntradaAlterada;
    }

    private PernaEdicaoViewModel CriarPernaVm(Perna perna)
    {
        var vm = new PernaEdicaoViewModel(perna);
        vm.EntradaAlterada += OnEntradaAlterada;
        return vm;
    }

    private void OnColecaoAlterada(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (ReferenceEquals(sender, Plano.Perfis))
        {
            foreach (var p in _perfisAssinados) p.PropertyChanged -= OnEntradaAlterada;
            _perfisAssinados.Clear();
            foreach (PerfilFase p in Plano.Perfis)
            {
                p.PropertyChanged += OnEntradaAlterada;
                _perfisAssinados.Add(p);
            }
        }
        OnEntradaAlterada(sender, EventArgs.Empty);
    }

    private void OnEntradaAlterada(object? sender, EventArgs e)
    {
        Recalcular();
        EstaSujo = true;
    }

    /// <summary>Recalcula o plano e reconstrói os segmentos do navlog.</summary>
    public void Recalcular()
    {
        Plano.RenumerarPernas();

        // Preserva as marcações "cumprido" (por trecho De→Para) ao reconstruir.
        var marcados = new HashSet<string>(
            Segmentos.Where(s => s.Cumprido).Select(s => $"{s.De}→{s.Para}"));

        _resultado = CalculadoraNavegacao.Calcular(Plano);

        Segmentos.Clear();
        foreach (var seg in _resultado.Pernas)
        {
            var vm = new SegmentoLinhaViewModel(seg);
            if (marcados.Contains($"{vm.De}→{vm.Para}"))
                vm.Cumprido = true;
            Segmentos.Add(vm);
        }

        foreach (var linha in Pernas)
            linha.AtualizarOrdem();

        RaiseResumo();
    }

    private void AdicionarPerna()
    {
        var anterior = Plano.Pernas.LastOrDefault();
        var nova = new Perna { CursoMag = anterior?.CursoMag ?? 0 };
        Plano.Pernas.Add(nova);
        Pernas.Add(CriarPernaVm(nova));
        Recalcular();
        EstaSujo = true;
        Selecionada = Pernas[^1];
    }

    private void RemoverPerna()
    {
        if (Selecionada is null) return;
        int i = Pernas.IndexOf(Selecionada);
        Selecionada.EntradaAlterada -= OnEntradaAlterada;
        Plano.Pernas.RemoveAt(i);
        Pernas.RemoveAt(i);
        Recalcular();
        EstaSujo = true;
        Selecionada = Pernas.Count > 0 ? Pernas[Math.Min(i, Pernas.Count - 1)] : null;
    }

    private void DuplicarPerna()
    {
        if (Selecionada is null) return;
        int i = Pernas.IndexOf(Selecionada);
        var copia = Clonar(Selecionada.Modelo);
        Plano.Pernas.Insert(i + 1, copia);
        Pernas.Insert(i + 1, CriarPernaVm(copia));
        Recalcular();
        EstaSujo = true;
        Selecionada = Pernas[i + 1];
    }

    private void SubirPerna() => Mover(-1);
    private void DescerPerna() => Mover(+1);

    private void Mover(int desloc)
    {
        if (Selecionada is null) return;
        int i = Pernas.IndexOf(Selecionada);
        int j = i + desloc;
        if (j < 0 || j >= Pernas.Count) return;

        Plano.Pernas.Move(i, j);
        Pernas.Move(i, j);
        Recalcular();
        EstaSujo = true;
        Selecionada = Pernas[j];
    }

    private static Perna Clonar(Perna p) => new()
    {
        Para = p.Para,
        DistanciaNm = p.DistanciaNm,
        CursoMag = p.CursoMag,
        AltitudeOverrideFt = p.AltitudeOverrideFt,
        OatCOverride = p.OatCOverride,
        IasKtOverride = p.IasKtOverride,
        VentoDirOverride = p.VentoDirOverride,
        VentoVelOverride = p.VentoVelOverride,
        ConsumoKgHOverride = p.ConsumoKgHOverride
    };

    public void MarcarSalvo() => EstaSujo = false;

    private void RaiseResumo()
    {
        foreach (var nome in new[]
        {
            nameof(TotalDistanciaNm), nameof(TotalTempoMin), nameof(TotalCombustivelKg), nameof(GsMediaKt),
            nameof(FaseSubida), nameof(FaseCruzeiro), nameof(FaseDescida),
            nameof(Toc), nameof(Tod), nameof(TemToc), nameof(TemTod),
            nameof(CombPartidaTaxiKg), nameof(CombRotaKg), nameof(CombContingenciaKg),
            nameof(CombAlternativaKg), nameof(CombReservaKg), nameof(CombTotalKg), nameof(AutonomiaHoras),
            nameof(TemAlternado), nameof(AlternadoResumo)
        })
            OnPropertyChanged(nome);
    }

    /// <summary>Plano de exemplo para o app abrir com algo útil.</summary>
    public static PlanoDeVoo PlanoExemplo()
    {
        var plano = new PlanoDeVoo
        {
            Aeronave = "PT-ABC",
            Piloto = "",
            AeronaveIcaoTipo = "C172",
            Regras = Core.RegrasVoo.VFR,
            EobtUtc = "13:00",
            PessoasABordo = 1,
            OrigemNome = "SBSP",
            DestinoNome = "SDCO",
            ElevacaoPartidaFt = 2400,
            AltitudeCruzeiroFt = 5500,
            ElevacaoDestinoFt = 2200,
            AlternadoNome = "SBAX",
            AlternadoDistanciaNm = 22,
            AlternadoCursoMag = 300,
            Atmosfera = new CondicoesAtmosfera
            {
                VentoDirGrausMag = 270, VentoVelKt = 15, AltRefFt = 2400, OatRefC = 22, GradienteCPor1000Ft = 1.98
            },
            Combustivel = new ParametrosCombustivel
            {
                PartidaTaxiKg = 3, ReservaMin = 45, ContingenciaPercentual = 0.05
            }
        };
        plano.Perfis.Add(new PerfilFase { Fase = Core.Fase.Subida, IasKt = 80, ConsumoKgH = 30, RazaoTipicaFtMin = 700 });
        plano.Perfis.Add(new PerfilFase { Fase = Core.Fase.Cruzeiro, IasKt = 110, ConsumoKgH = 22, RazaoTipicaFtMin = 0 });
        plano.Perfis.Add(new PerfilFase { Fase = Core.Fase.Descida, IasKt = 120, ConsumoKgH = 15, RazaoTipicaFtMin = -500 });

        plano.Pernas.Add(new Perna { Para = "AGUAS", DistanciaNm = 25, CursoMag = 292 });
        plano.Pernas.Add(new Perna { Para = "SERRA", DistanciaNm = 30, CursoMag = 274 });
        plano.Pernas.Add(new Perna { Para = "VALE", DistanciaNm = 14, CursoMag = 251 });
        plano.Pernas.Add(new Perna { Para = "SDCO", DistanciaNm = 20, CursoMag = 240 });
        plano.RenumerarPernas();
        return plano;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? nome = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
}
