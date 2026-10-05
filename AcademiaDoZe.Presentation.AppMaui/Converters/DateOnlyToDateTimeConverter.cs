// Alan Medeiros
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// Permite binding two-way entre DateOnly, no DTO, e DateTime exigido pelo DatePicker do MAUI
public class DateOnlyToDateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateOnly dateOnly)
            return dateOnly.ToDateTime(TimeOnly.MinValue);

        return DateTime.Today;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
            return DateOnly.FromDateTime(dateTime);

        return DateOnly.FromDateTime(DateTime.Today);
    }
}
