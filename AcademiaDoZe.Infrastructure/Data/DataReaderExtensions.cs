// Alan Medeiros
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data.Common;
using System.Globalization;

namespace AcademiaDoZe.Infrastructure.Data;

public static class DataReaderExtensions
{
    private static object? ObterValor(DbDataReader reader, string columnName)
    {
        int ordinal;

        try
        {
            ordinal = reader.GetOrdinal(columnName);
        }
        catch (IndexOutOfRangeException ex)
        {
            throw new InfrastructureException(
                "COLUNA_NAO_ENCONTRADA",
                $"Coluna '{columnName}' nao encontrada no resultado da consulta.",
                ex);
        }

        return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
    }

    public static int GetInt32Value(this DbDataReader reader, string columnName)
    {
        var valor = ObterValor(reader, columnName);

        if (valor is null)
        {
            throw new InfrastructureException(
                "VALOR_NULO_INESPERADO",
                $"Coluna '{columnName}' retornou nulo onde um inteiro era esperado.");
        }

        return Convert.ToInt32(valor, CultureInfo.InvariantCulture);
    }

    public static int? GetNullableInt32(this DbDataReader reader, string columnName)
    {
        var valor = ObterValor(reader, columnName);

        return valor is null ? null : Convert.ToInt32(valor, CultureInfo.InvariantCulture);
    }

    public static string GetStringValue(this DbDataReader reader, string columnName)
    {
        var valor = ObterValor(reader, columnName);

        if (valor is null)
        {
            throw new InfrastructureException(
                "VALOR_NULO_INESPERADO",
                $"Coluna '{columnName}' retornou nulo onde um texto era esperado.");
        }

        return Convert.ToString(valor, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public static string GetNullableString(this DbDataReader reader, string columnName)
    {
        var valor = ObterValor(reader, columnName);

        return valor is null
            ? string.Empty
            : Convert.ToString(valor, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public static DateOnly GetDateOnlyValue(this DbDataReader reader, string columnName)
    {
        var valor = ObterValor(reader, columnName);

        if (valor is null)
        {
            throw new InfrastructureException(
                "VALOR_NULO_INESPERADO",
                $"Coluna '{columnName}' retornou nulo onde uma data era esperada.");
        }

        return valor switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            string texto => ConverterTextoParaData(texto, columnName),
            _ => throw new InfrastructureException(
                "CONVERSAO_DATA_INVALIDA",
                $"Nao foi possivel converter a coluna '{columnName}' ({valor.GetType().Name}) para data.")
        };
    }

    public static DateTime GetDateTimeValue(this DbDataReader reader, string columnName)
    {
        var valor = ObterValor(reader, columnName);

        if (valor is null)
        {
            throw new InfrastructureException(
                "VALOR_NULO_INESPERADO",
                $"Coluna '{columnName}' retornou nulo onde uma data/hora era esperada.");
        }

        return valor switch
        {
            DateTime dateTime => dateTime,
            string texto => DateTime.Parse(texto, CultureInfo.InvariantCulture),
            _ => Convert.ToDateTime(valor, CultureInfo.InvariantCulture)
        };
    }

    public static byte[]? GetNullableBytes(this DbDataReader reader, string columnName)
    {
        var valor = ObterValor(reader, columnName);

        return valor switch
        {
            null => null,
            byte[] bytes => bytes.Length == 0 ? null : bytes,
            _ => throw new InfrastructureException(
                "CONVERSAO_BINARIO_INVALIDA",
                $"Nao foi possivel converter a coluna '{columnName}' ({valor.GetType().Name}) para binario.")
        };
    }

    private static DateOnly ConverterTextoParaData(string texto, string columnName)
    {
        if (DateOnly.TryParse(texto, CultureInfo.InvariantCulture, out var data))
            return data;

        if (DateTime.TryParse(texto, CultureInfo.InvariantCulture, out var dataHora))
            return DateOnly.FromDateTime(dataHora);

        throw new InfrastructureException(
            "CONVERSAO_DATA_INVALIDA",
            $"Nao foi possivel converter o texto '{texto}' da coluna '{columnName}' para data.");
    }
}
