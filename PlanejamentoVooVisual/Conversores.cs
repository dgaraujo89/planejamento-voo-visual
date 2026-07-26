using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PlanejamentoVooVisual;

/// <summary>
/// Converte <c>double?</c> ↔ texto em pt-BR. Texto vazio ou inválido vira
/// <c>null</c> — é assim que o piloto limpa uma célula e volta ao valor herdado.
/// </summary>
public sealed class NullableDoubleConverter : IValueConverter
{
    public string Formato { get; set; } = "0.##";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return string.Empty;
        double d = System.Convert.ToDouble(value, culture);
        return d.ToString(Formato, culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
            return null;
        return double.TryParse(s, NumberStyles.Any, culture, out var d) ? d : null;
    }
}

/// <summary>Minutos (double) → texto "h:mm". Nulo/NaN → travessão.</summary>
public sealed class MinutosParaHoraConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return "—";
        double min = System.Convert.ToDouble(value, culture);
        if (double.IsNaN(min) || double.IsInfinity(min)) return "—";
        int total = (int)Math.Round(min);
        return $"{total / 60}:{total % 60:00}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Fração (0,05) ↔ percentual exibido (5). Para o campo de contingência.</summary>
public sealed class FracaoParaPercentualConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double f = System.Convert.ToDouble(value ?? 0.0, culture);
        return (f * 100).ToString("0.##", culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && double.TryParse(s, NumberStyles.Any, culture, out var p))
            return p / 100.0;
        return 0.0;
    }
}

/// <summary>Verdadeiro → Visible; falso → Collapsed.</summary>
public sealed class BoolParaVisibilidadeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible;
}
