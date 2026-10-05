// Alan Medeiros
using AcademiaDoZe.Infrastructure.Data;

namespace AcademiaDoZe.Infrastructure.Tests;

public abstract class TestBase
{
    // Alterne o SGBD alvo dos testes trocando apenas a constante abaixo:
    private const DatabaseType SelectedDatabaseType = DatabaseType.Sqlite;

    protected const string MeuNome = "Alan";
    protected const string MeuSobrenome = "Medeiros";

    private static readonly Lock Trava = new();
    private static bool _bancoInicializado;

    protected static DatabaseType DatabaseType => SelectedDatabaseType;

    protected static string SiglaSgbd => DatabaseType switch
    {
        DatabaseType.SqlServer => "SQLServer",
        DatabaseType.MySql => "MySQL",
        DatabaseType.Sqlite => "SQLite",
        _ => throw new ArgumentOutOfRangeException(
            nameof(DatabaseType),
            DatabaseType,
            "SGBD nao suportado para testes.")
    };

    protected static string SenhaPadrao => $"SenhaValida123{SiglaSgbd}";

    // Ajuste a ConnectionString com caminhos e credenciais validas
    protected static string ConnectionString => DatabaseType switch
    {
        DatabaseType.SqlServer =>
            "Server=localhost,1433;Database=db_academia_do_ze;User Id=sa;" +
            "Password=abcBolinhas12345;TrustServerCertificate=True;Encrypt=True;",

        DatabaseType.MySql =>
            "Server=localhost;Port=3399;Database=db_academia_do_ze;User Id=root;" +
            "Password=abcBolinhas12345;",

        DatabaseType.Sqlite =>
            $"Data Source={CaminhoBancoSqlite};Cache=Shared;",

        _ => throw new ArgumentOutOfRangeException(
            nameof(DatabaseType),
            DatabaseType,
            "SGBD nao suportado para testes.")
    };

    protected static string CaminhoBancoSqlite => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "DEV",
        "AcademiaDoZe",
        "db_academia_do_ze.db");

    protected TestBase()
    {
        lock (Trava)
        {
            if (_bancoInicializado)
                return;

            DbInitializer
                .InitializeAsync(ConnectionString, DatabaseType)
                .GetAwaiter()
                .GetResult();

            _bancoInicializado = true;
        }
    }

    protected static string GerarCpf()
    {
        var digitos = new int[11];

        for (var i = 0; i < 9; i++)
            digitos[i] = Random.Shared.Next(0, 10);

        digitos[9] = CalcularDigitoCpf(digitos, 9, 10);
        digitos[10] = CalcularDigitoCpf(digitos, 10, 11);

        return string.Concat(digitos);
    }

    private static int CalcularDigitoCpf(int[] digitos, int quantidade, int pesoInicial)
    {
        var soma = 0;
        for (var i = 0; i < quantidade; i++)
            soma += digitos[i] * (pesoInicial - i);

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    protected static string GerarTelefone() =>
        $"5{Random.Shared.Next(1000000000, int.MaxValue).ToString()[..10]}";

    protected static string GerarEmail() =>
        $"user_{Guid.NewGuid().ToString("N")[..8]}@test.com";

    protected static string GerarCep() =>
        Random.Shared.Next(10000000, 99999999).ToString();
}
