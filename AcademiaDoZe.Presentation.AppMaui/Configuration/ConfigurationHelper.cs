// Alan Medeiros
using AcademiaDoZe.Application.DependencyInjection;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Presentation.AppMaui.Message;
using CommunityToolkit.Mvvm.Messaging;

namespace AcademiaDoZe.Presentation.AppMaui.Configuration;

public static class ConfigurationHelper
{
    private const string NomeArquivoBanco = "db_academia_do_ze.db";

    // Bancos que acompanham o pacote do aplicativo e ficam disponíveis para escolha na tela de configurações
    private static readonly string[] BancosDoPacote = [NomeArquivoBanco, "db_academia_filial.db"];

    public static void ConfigureServices(IServiceCollection services)
    {
        var (connectionString, databaseType) = ObterConfiguracaoAtual();

        var repoConfig = new RepositoryConfig
        {
            ConnectionString = connectionString,
            DatabaseType = databaseType.ToInfrastructure()
        };

        // Configura a fábrica de repositórios com a string de conexão e tipo de banco
        services.AddSingleton(repoConfig);

        // Assina mensagens de alteração de banco de dados para atualizar o RepositoryConfig diretamente
        WeakReferenceMessenger.Default.Register<RepositoryConfig, BancoPreferencesUpdatedMessage>(repoConfig, (r, m) =>
        {
            var (novaConnStr, novoDbType) = ObterConfiguracaoAtual();
            r.ConnectionString = novaConnStr;
            r.DatabaseType = novoDbType.ToInfrastructure();
        });

        // Configura os serviços da camada de aplicação
        services.AddApplicationServices();
    }

    // Obtém a Connection String e o AppDatabaseType ativos a partir das Preferences do usuário,
    // com valores padrão para cada um dos 3 gerenciadores: Sqlite, MySql e SqlServer.
    public static (string ConnectionString, AppDatabaseType DatabaseType) ObterConfiguracaoAtual()
    {
        var databaseTypeStr = Preferences.Get("DatabaseType", AppDatabaseType.Sqlite.ToString());
        if (!Enum.TryParse<AppDatabaseType>(databaseTypeStr, out var databaseType))
        {
            databaseType = AppDatabaseType.Sqlite;
        }

        string connectionString;

        if (databaseType == AppDatabaseType.Sqlite)
        {
            var caminhoPadrao = ObterCaminhoBancoSqlite();

            var caminho = Preferences.Get("Sqlite_Caminho", caminhoPadrao);
            if (string.IsNullOrWhiteSpace(caminho)) caminho = caminhoPadrao;

            // quando apenas o nome do arquivo é informado, ele é procurado na pasta de dados do aplicativo
            if (!caminho.Contains(Path.DirectorySeparatorChar) && !caminho.Contains('/'))
            {
                caminho = Path.Combine(Path.GetDirectoryName(caminhoPadrao)!, caminho);
            }

            var complemento = Preferences.Get("Sqlite_Complemento", "Default Timeout=5;");
            connectionString = $"Data Source={caminho};{complemento}";
        }
        else
        {
            var prefixo = databaseType == AppDatabaseType.SqlServer ? "SqlServer" : "MySql";
            var usuarioPadrao = databaseType == AppDatabaseType.SqlServer ? "sa" : "root";
            var complementoPadrao = databaseType == AppDatabaseType.SqlServer
                ? "TrustServerCertificate=True;Encrypt=True;Connect Timeout=5;Connection Timeout=5;"
                : "Connection Timeout=5;Default Command Timeout=30;";

            // 10.0.2.2 é o endereço do computador host visto de dentro do emulador Android
            var dbServer = Preferences.Get($"{prefixo}_Servidor", "10.0.2.2");
            var dbDatabase = Preferences.Get($"{prefixo}_Banco", "db_academia_do_ze");
            var dbUser = Preferences.Get($"{prefixo}_Usuario", usuarioPadrao);
            var dbPassword = Preferences.Get($"{prefixo}_Senha", string.Empty);
            var dbComplemento = Preferences.Get($"{prefixo}_Complemento", complementoPadrao);

            connectionString = $"Server={dbServer};Database={dbDatabase};User Id={dbUser};Password={dbPassword};{dbComplemento}";
        }

        return (connectionString, databaseType);
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

        foreach (var nome in BancosDoPacote)
        {
            var arquivoDestino = Path.Combine(FileSystem.AppDataDirectory, nome);
            if (File.Exists(arquivoDestino)) continue;

            // Task.Run evita bloquear a thread principal durante a inicializacao do aplicativo
            Task.Run(async () =>
            {
                using var origem = await FileSystem.OpenAppPackageFileAsync(nome);
                using var arquivo = File.Create(arquivoDestino);
                await origem.CopyToAsync(arquivo);
            }).GetAwaiter().GetResult();
        }

        return Path.Combine(FileSystem.AppDataDirectory, NomeArquivoBanco);
    }
}
