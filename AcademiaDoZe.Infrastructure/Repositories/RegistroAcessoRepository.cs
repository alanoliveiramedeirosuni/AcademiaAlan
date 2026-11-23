// Alan Medeiros
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using System.Data;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Repositories;

public sealed class RegistroAcessoRepository(string connectionString, DatabaseType databaseType)
    : BaseRepository(connectionString, databaseType), IRegistroAcessoRepository
{
    public async Task<IReadOnlyList<RegistroAcesso>> ObterPorPessoaAsync(AcessoTipo tipo, int pessoaId, CancellationToken ct = default)
    {
        await DbInitializer.InitializeAsync(ConnectionString, DatabaseType, ct);
        await using var comando = await CreateCommandAsync(
            "SELECT id_acesso, tipo, pessoa_id, entrada, saida FROM tb_registro_acesso WHERE tipo = @Tipo AND pessoa_id = @Pessoa ORDER BY entrada DESC, id_acesso DESC", ct);
        comando.AddParameter("@Tipo", (int)tipo, DbType.Int32);
        comando.AddParameter("@Pessoa", pessoaId, DbType.Int32);
        await using var leitor = await comando.ExecuteReaderAsync(ct);
        var registros = new List<RegistroAcesso>();
        while (await leitor.ReadAsync(ct))
        {
            DateTime? saida = leitor.IsDBNull(leitor.GetOrdinal("saida")) ? null : leitor.GetDateTimeValue("saida");
            var resultado = RegistroAcesso.Criar(leitor.GetInt32Value("id_acesso"),
                (AcessoTipo)leitor.GetInt32Value("tipo"), leitor.GetInt32Value("pessoa_id"),
                leitor.GetDateTimeValue("entrada"), saida);
            if (resultado.IsFailure) throw new InvalidOperationException("Registro de acesso inválido no banco.");
            registros.Add(resultado.Value!);
        }
        return registros;
    }

    public async Task RegistrarEntradaAsync(RegistroAcesso registro, CancellationToken ct = default)
    {
        await DbInitializer.InitializeAsync(ConnectionString, DatabaseType, ct);
        await using var comando = await CreateCommandAsync(
            "INSERT INTO tb_registro_acesso (tipo, pessoa_id, entrada, pessoa_aberta) VALUES (@Tipo, @Pessoa, @Entrada, @Pessoa)", ct);
        comando.AddParameter("@Tipo", (int)registro.Tipo, DbType.Int32);
        comando.AddParameter("@Pessoa", registro.PessoaId, DbType.Int32);
        comando.AddParameter("@Entrada", registro.Entrada, DbType.DateTime);
        try { await comando.ExecuteNonQueryAsync(ct); }
        catch (DbException ex) { throw new InvalidOperationException("Não foi possível registrar a entrada. Confira se já existe uma visita em aberto e a conexão com o banco.", ex); }
    }

    public async Task RegistrarSaidaAsync(int id, DateTime saida, CancellationToken ct = default)
    {
        await DbInitializer.InitializeAsync(ConnectionString, DatabaseType, ct);
        await using var comando = await CreateCommandAsync(
            "UPDATE tb_registro_acesso SET saida = @Saida, pessoa_aberta = NULL WHERE id_acesso = @Id AND saida IS NULL AND entrada <= @Saida", ct);
        comando.AddParameter("@Id", id, DbType.Int32);
        comando.AddParameter("@Saida", saida, DbType.DateTime);
        if (await comando.ExecuteNonQueryAsync(ct) != 1)
            throw new InvalidOperationException("Esta visita já foi encerrada ou o horário da saída é inválido.");
    }
}
