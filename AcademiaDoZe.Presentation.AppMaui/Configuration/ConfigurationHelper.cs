// Alan Medeiros
using AcademiaDoZe.Application.DependencyInjection;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Mappings;

namespace AcademiaDoZe.Presentation.AppMaui.Configuration;

public static class ConfigurationHelper
{
    private const string NomeArquivoBanco = "db_academia_do_ze.db";

    public static void ConfigureServices(IServiceCollection services)
    {
        // 1. Tipo de banco de dados: SqlServer, MySql ou Sqlite
        var databaseType = AppDatabaseType.Sqlite;

        // 2. Configuração da Connection String de acordo com o banco escolhido
        string connectionString;

        if (databaseType == AppDatabaseType.Sqlite)
        {
            connectionString = $"Data Source={ObterCaminhoBancoSqlite()};Default Timeout=5;";
        }
        else
        {
            const string dbServer = "localhost";
            const string dbDatabase = "db_academia_do_ze";
            const string dbUser = "root";
            const string dbPassword = "abcBolinhas12345";

            // ajuste do complemento de acordo com o tipo de banco de dados escolhido
            string dbComplemento = string.Empty;

            if (databaseType == AppDatabaseType.SqlServer)
            {
                dbComplemento = "TrustServerCertificate=True;Encrypt=True;Connect Timeout=5;Connection Timeout=5;";
            }
            else if (databaseType == AppDatabaseType.MySql)
            {
                dbComplemento = "Connection Timeout=5;Default Command Timeout=30;";
            }

            connectionString = $"Server={dbServer};Database={dbDatabase};User Id={dbUser};Password={dbPassword};{dbComplemento}";
        }

        // 3. Configura a fábrica de repositórios com a string de conexão e tipo de banco
        services.AddSingleton(new RepositoryConfig
        {
            ConnectionString = connectionString,
            DatabaseType = databaseType.ToInfrastructure()
        });

        // 4. Configura os serviços da camada de aplicação
        services.AddApplicationServices();
    }

    // No desktop o banco fica em uma pasta fixa; em celulares e tablets o arquivo
    // é copiado do pacote do aplicativo para a área de dados na primeira execução.
    private static string ObterCaminhoBancoSqlite()
    {
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            var pastaWindows = Path.Combine("C:", "DEV", "AcademiaDoZe");
            Directory.CreateDirectory(pastaWindows);
            return Path.Combine(pastaWindows, NomeArquivoBanco);
        }

        if (DeviceInfo.Platform == DevicePlatform.MacCatalyst)
        {
            var pastaMac = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "DEV",
                "AcademiaDoZe");
            Directory.CreateDirectory(pastaMac);
            return Path.Combine(pastaMac, NomeArquivoBanco);
        }

        var destino = Path.Combine(FileSystem.AppDataDirectory, NomeArquivoBanco);

        if (!File.Exists(destino))
        {
            using var origem = FileSystem.OpenAppPackageFileAsync(NomeArquivoBanco).GetAwaiter().GetResult();
            using var arquivo = File.Create(destino);
            origem.CopyTo(arquivo);
        }

        return destino;
    }
}
