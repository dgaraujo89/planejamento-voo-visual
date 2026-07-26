namespace PlanejamentoVooVisual.Core;

/// <summary>
/// Uma perna da rota: o ponto para onde se voa (<see cref="Para"/>) e os dados
/// para chegar até ele (distância, curso, overrides). O "de" é implícito — a
/// Origem na primeira perna, o <see cref="Para"/> anterior nas demais. Fase e
/// altitudes NÃO são digitadas: o motor as calcula pelo perfil vertical.
/// Os campos <c>*Override</c> são nulos quando a perna herda o valor global.
/// </summary>
public sealed class Perna : ObservableObject
{
    private int _ordem;
    private string _para = string.Empty;
    private double _distanciaNm;
    private double _cursoMag;
    private double? _altitudeOverrideFt;
    private double? _oatCOverride;
    private double? _iasKtOverride;
    private double? _ventoDirOverride;
    private double? _ventoVelOverride;
    private double? _consumoKgHOverride;

    /// <summary>Posição da perna na rota (1-based).</summary>
    public int Ordem
    {
        get => _ordem;
        set => SetProperty(ref _ordem, value);
    }

    /// <summary>Ponto para onde se voa nesta perna (o "para").</summary>
    public string Para
    {
        get => _para;
        set => SetProperty(ref _para, value);
    }

    /// <summary>Distância da perna, em NM.</summary>
    public double DistanciaNm
    {
        get => _distanciaNm;
        set => SetProperty(ref _distanciaNm, value);
    }

    /// <summary>Curso magnético, em graus (0–359).</summary>
    public double CursoMag
    {
        get => _cursoMag;
        set => SetProperty(ref _cursoMag, value);
    }

    /// <summary>
    /// Altitude alvo a partir desta perna (null = mantém o alvo vigente). Quando
    /// preenchida, o avião sobe/desce até ela na razão típica e a mantém nas pernas
    /// seguintes, até outra sobrescrita — a descida ao destino no fim é preservada.
    /// </summary>
    public double? AltitudeOverrideFt
    {
        get => _altitudeOverrideFt;
        set => SetProperty(ref _altitudeOverrideFt, value);
    }

    /// <summary>OAT específica da perna (null = calculada pelo gradiente).</summary>
    public double? OatCOverride
    {
        get => _oatCOverride;
        set => SetProperty(ref _oatCOverride, value);
    }

    /// <summary>IAS específica da perna (null = usa o perfil da fase).</summary>
    public double? IasKtOverride
    {
        get => _iasKtOverride;
        set => SetProperty(ref _iasKtOverride, value);
    }

    /// <summary>Direção do vento específica da perna (null = usa a atmosfera global).</summary>
    public double? VentoDirOverride
    {
        get => _ventoDirOverride;
        set => SetProperty(ref _ventoDirOverride, value);
    }

    /// <summary>Velocidade do vento específica da perna (null = usa a atmosfera global).</summary>
    public double? VentoVelOverride
    {
        get => _ventoVelOverride;
        set => SetProperty(ref _ventoVelOverride, value);
    }

    /// <summary>Consumo específico da perna (null = usa o perfil da fase).</summary>
    public double? ConsumoKgHOverride
    {
        get => _consumoKgHOverride;
        set => SetProperty(ref _consumoKgHOverride, value);
    }
}
