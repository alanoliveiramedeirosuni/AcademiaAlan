// Alan Medeiros
using System.Text.RegularExpressions;

namespace AcademiaDoZe.Domain.Services;

public static partial class NormalizadoService
{
    public static bool TextoVazioOuNulo(string? texto) =>
        string.IsNullOrWhiteSpace(texto);

    public static string LimparEspacos(string? texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? string.Empty
            : EspacosRegex().Replace(texto, " ").Trim();

    public static string LimparTodosEspacos(string? texto) =>
        string.IsNullOrWhiteSpace(texto)
            ? string.Empty
            : new string(texto.Where(caractere => !char.IsWhiteSpace(caractere)).ToArray());

    public static string ParaMaiusculo(string? texto) =>
        string.IsNullOrEmpty(texto) ? string.Empty : texto.ToUpperInvariant();

    public static string ParaMinusculo(string? texto) =>
        string.IsNullOrEmpty(texto) ? string.Empty : texto.ToLowerInvariant();

    public static string LimparEDigitos(string? texto) =>
        string.IsNullOrEmpty(texto)
            ? string.Empty
            : new string(texto.Where(char.IsDigit).ToArray());

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspacosRegex();
}
