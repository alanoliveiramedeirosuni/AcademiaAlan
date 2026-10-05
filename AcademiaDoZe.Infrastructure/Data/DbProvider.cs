// Alan Medeiros
using AcademiaDoZe.Infrastructure.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using System.Data;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Data;

public static class DbProvider
{
    public static DbConnection CreateConnection(string connectionString, DatabaseType databaseType)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InfrastructureException(
                "CONNECTION_STRING_INVALIDA",
                "A connection string nao pode ser nula ou vazia.");
        }

        return databaseType switch
        {
            DatabaseType.SqlServer => new SqlConnection(connectionString),
            DatabaseType.MySql => new MySqlConnection(connectionString),
            DatabaseType.Sqlite => new SqliteConnection(connectionString),
            _ => throw new InfrastructureException(
                "SGBD_NAO_SUPORTADO",
                $"SGBD nao suportado: {databaseType}.")
        };
    }

    public static async Task<DbConnection> CreateOpenConnectionAsync(
        string connectionString,
        DatabaseType databaseType,
        CancellationToken cancellationToken = default)
    {
        var connection = CreateConnection(connectionString, databaseType);

        try
        {
            await connection.OpenAsync(cancellationToken);
        }
        catch (DbException ex)
        {
            await connection.DisposeAsync();

            throw new InfrastructureException(
                "ERRO_ABRIR_CONEXAO",
                $"Erro ao abrir conexao com {databaseType}: {ex.Message}",
                ex);
        }

        if (databaseType == DatabaseType.Sqlite)
        {
            await using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            await pragma.ExecuteNonQueryAsync(cancellationToken);
        }

        return connection;
    }

    public static string GetLastInsertIdQuery(DatabaseType databaseType) => databaseType switch
    {
        DatabaseType.SqlServer => "SELECT CAST(SCOPE_IDENTITY() AS INT);",
        DatabaseType.MySql => "SELECT LAST_INSERT_ID();",
        DatabaseType.Sqlite => "SELECT last_insert_rowid();",
        _ => throw new InfrastructureException(
            "SGBD_NAO_SUPORTADO",
            $"SGBD nao suportado: {databaseType}.")
    };

    public static string GetCurrentDateFunction(DatabaseType databaseType) => databaseType switch
    {
        DatabaseType.SqlServer => "CAST(GETDATE() AS DATE)",
        DatabaseType.MySql => "CURDATE()",
        DatabaseType.Sqlite => "DATE('now', 'localtime')",
        _ => throw new InfrastructureException(
            "SGBD_NAO_SUPORTADO",
            $"SGBD nao suportado: {databaseType}.")
    };

    public static string GetDateAddDaysExpression(
        DatabaseType databaseType,
        string dateExpression,
        string daysExpression) => databaseType switch
    {
        DatabaseType.SqlServer => $"DATEADD(DAY, {daysExpression}, {dateExpression})",
        DatabaseType.MySql => $"DATE_ADD({dateExpression}, INTERVAL {daysExpression} DAY)",
        DatabaseType.Sqlite => $"DATE({dateExpression}, '+' || {daysExpression} || ' days')",
        _ => throw new InfrastructureException(
            "SGBD_NAO_SUPORTADO",
            $"SGBD nao suportado: {databaseType}.")
    };

    public static (string Abertura, string Fechamento) GetDelimitadores(DatabaseType databaseType) =>
        databaseType switch
        {
            DatabaseType.SqlServer => ("[", "]"),
            DatabaseType.MySql => ("`", "`"),
            DatabaseType.Sqlite => ("\"", "\""),
            _ => throw new InfrastructureException(
                "SGBD_NAO_SUPORTADO",
                $"SGBD nao suportado: {databaseType}.")
        };

    public static void AddParameter(this DbCommand command, string name, object? value, DbType dbType)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value ?? DBNull.Value;

        command.Parameters.Add(parameter);
    }

    public static async Task<int> ExecuteScalarIdAsync(
        this DbCommand command,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var resultado = await command.ExecuteScalarAsync(cancellationToken);

        if (resultado is null || resultado == DBNull.Value)
            throw new InfrastructureException(errorCode, errorMessage);

        return Convert.ToInt32(resultado);
    }
}
