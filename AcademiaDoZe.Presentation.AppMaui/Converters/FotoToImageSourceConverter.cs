// Alan Medeiros
using AcademiaDoZe.Application.DTOs;
using System.Globalization;

namespace AcademiaDoZe.Presentation.AppMaui.Converters;

// A foto é armazenada no banco em array de bytes; para exibir é preciso converter para ImageSource
public class FotoToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ArquivoDto foto && foto.Conteudo != null && foto.Conteudo.Length > 0)
        {
            var bytes = foto.Conteudo;
            return ImageSource.FromStream(() => new MemoryStream(bytes));
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
