using System.Collections.ObjectModel;

namespace PlanejamentoVooVisual.Core;

/// <summary>
/// Plano de voo completo — apenas entradas. Os valores calculados nunca são
/// guardados aqui; são sempre derivados pelo motor de cálculo.
/// </summary>
public sealed class PlanoDeVoo : ObservableObject
{
    private string _aeronave = string.Empty;
    private DateTime _data = DateTime.Today;
    private string _piloto = string.Empty;
    private string _aeronaveIcaoTipo = string.Empty;
    private RegrasVoo _regras = RegrasVoo.VFR;
    private string _eobtUtc = string.Empty;
    private int _pessoasABordo = 1;
    private string _origemNome = string.Empty;
    private string _destinoNome = string.Empty;
    private double _elevacaoPartidaFt;
    private double _altitudeCruzeiroFt;
    private double _elevacaoDestinoFt;

    /// <summary>Identificação da aeronave (callsign / matrícula).</summary>
    public string Aeronave
    {
        get => _aeronave;
        set => SetProperty(ref _aeronave, value);
    }

    /// <summary>Tipo ICAO da aeronave para o plano IVAO (ex.: C172, PA28, B738).</summary>
    public string AeronaveIcaoTipo
    {
        get => _aeronaveIcaoTipo;
        set => SetProperty(ref _aeronaveIcaoTipo, value);
    }

    /// <summary>Regras de voo (campo 8 do plano ICAO). Padrão VFR.</summary>
    public RegrasVoo Regras
    {
        get => _regras;
        set => SetProperty(ref _regras, value);
    }

    /// <summary>Hora prevista de partida (EOBT), em UTC, formato "HH:mm".</summary>
    public string EobtUtc
    {
        get => _eobtUtc;
        set => SetProperty(ref _eobtUtc, value);
    }

    /// <summary>Pessoas a bordo (POB).</summary>
    public int PessoasABordo
    {
        get => _pessoasABordo;
        set => SetProperty(ref _pessoasABordo, value);
    }

    /// <summary>Nome do ponto de origem (partida). É o "de" da primeira perna.</summary>
    public string OrigemNome
    {
        get => _origemNome;
        set => SetProperty(ref _origemNome, value);
    }

    /// <summary>Nome do ponto de destino (o "para" da última perna).</summary>
    public string DestinoNome
    {
        get => _destinoNome;
        set => SetProperty(ref _destinoNome, value);
    }

    /// <summary>Elevação/altitude da origem, em ft (início do perfil vertical).</summary>
    public double ElevacaoPartidaFt
    {
        get => _elevacaoPartidaFt;
        set => SetProperty(ref _elevacaoPartidaFt, value);
    }

    /// <summary>Altitude de cruzeiro alvo, em ft.</summary>
    public double AltitudeCruzeiroFt
    {
        get => _altitudeCruzeiroFt;
        set => SetProperty(ref _altitudeCruzeiroFt, value);
    }

    /// <summary>Elevação/altitude do destino, em ft (fim do perfil vertical).</summary>
    public double ElevacaoDestinoFt
    {
        get => _elevacaoDestinoFt;
        set => SetProperty(ref _elevacaoDestinoFt, value);
    }

    /// <summary>Data do voo.</summary>
    public DateTime Data
    {
        get => _data;
        set => SetProperty(ref _data, value);
    }

    /// <summary>Nome do piloto.</summary>
    public string Piloto
    {
        get => _piloto;
        set => SetProperty(ref _piloto, value);
    }

    /// <summary>Os três perfis de desempenho (Subida, Cruzeiro, Descida).</summary>
    public ObservableCollection<PerfilFase> Perfis { get; init; } = new();

    /// <summary>Vento e temperatura de referência.</summary>
    public CondicoesAtmosfera Atmosfera { get; init; } = new();

    /// <summary>Parâmetros do bloco de combustível adicional.</summary>
    public ParametrosCombustivel Combustivel { get; init; } = new();

    /// <summary>Pernas da rota, na ordem de voo.</summary>
    public ObservableCollection<Perna> Pernas { get; init; } = new();

    /// <summary>Perfil correspondente à fase informada, ou <c>null</c> se ausente.</summary>
    public PerfilFase? PerfilDe(Fase fase)
    {
        foreach (var perfil in Perfis)
            if (perfil.Fase == fase)
                return perfil;
        return null;
    }

    /// <summary>Renumera a ordem das pernas conforme a posição atual na lista.</summary>
    public void RenumerarPernas()
    {
        for (int i = 0; i < Pernas.Count; i++)
            Pernas[i].Ordem = i + 1;
    }

    /// <summary>Cria um plano com os três perfis padrão e valores de referência ISA.</summary>
    public static PlanoDeVoo Novo()
    {
        var plano = new PlanoDeVoo
        {
            Atmosfera = new CondicoesAtmosfera { GradienteCPor1000Ft = 1.98 },
            Combustivel = new ParametrosCombustivel()
        };
        plano.Perfis.Add(new PerfilFase { Fase = Fase.Subida });
        plano.Perfis.Add(new PerfilFase { Fase = Fase.Cruzeiro });
        plano.Perfis.Add(new PerfilFase { Fase = Fase.Descida });
        return plano;
    }
}
