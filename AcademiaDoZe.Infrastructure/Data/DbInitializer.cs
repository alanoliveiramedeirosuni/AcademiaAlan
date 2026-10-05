// Alan Medeiros
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data.Common;
using System.Reflection;

namespace AcademiaDoZe.Infrastructure.Data;

public static class DbInitializer
{
    private const string SeparadorComandos = "-- @@SPLIT@@";

    public static async Task InitializeAsync(
        string connectionString,
        DatabaseType databaseType,
        CancellationToken cancellationToken = default)
    {
        var script = LerScript(databaseType);

        if (databaseType == DatabaseType.Sqlite)
            GarantirDiretorioDoArquivo(connectionString);

        await using var connection = await DbProvider.CreateOpenConnectionAsync(
            connectionString,
            databaseType,
            cancellationToken);

        var comandos = script
            .Split(SeparadorComandos, StringSplitOptions.RemoveEmptyEntries)
            .Select(comando => comando.Trim())
            .Where(comando => comando.Length > 0);

        foreach (var texto in comandos)
        {
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = texto;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (DbException ex)
            {
                throw new InfrastructureException(
                    "ERRO_INICIALIZAR_BANCO",
                    $"Erro ao executar script de criacao em {databaseType}: {ex.Message}",
                    ex);
            }
        }
    }

    private static string LerScript(DatabaseType databaseType)
    {
        var nomeArquivo = databaseType switch
        {
            DatabaseType.SqlServer => "schema_sqlserver.sql",
            DatabaseType.MySql => "schema_mysql.sql",
            DatabaseType.Sqlite => "schema_sqlite.sql",
            _ => throw new InfrastructureException(
                "SGBD_NAO_SUPORTADO",
                $"SGBD nao suportado: {databaseType}.")
        };

        var diretorio = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppContext.BaseDirectory;

        var caminho = Path.Combine(diretorio, "Scripts", nomeArquivo);

        if (!File.Exists(caminho))
        {
            throw new InfrastructureException(
                "SCRIPT_NAO_ENCONTRADO",
                $"Script de criacao nao encontrado em '{caminho}'.");
        }

        return File.ReadAllText(caminho);
    }

    private static void GarantirDiretorioDoArquivo(string connectionString)
    {
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        var diretorio = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));

        if (!string.IsNullOrWhiteSpace(diretorio) && !Directory.Exists(diretorio))
            Directory.CreateDirectory(diretorio);
    }
}
