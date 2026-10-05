// Alan Medeiros
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;

namespace AcademiaDoZe.Infrastructure.Repositories;

public class LogradouroRepository : BaseRepository, ILogradouroRepository
{
    public LogradouroRepository(string connectionString, DatabaseType databaseType)
        : base(connectionString, databaseType)
    {
    }

    private static string BaseSelectQuery => @"
        SELECT
            l.id_logradouro, l.cep, l.nome, l.bairro, l.cidade, l.estado, l.pais
        FROM tb_logradouro l";

    public async Task<Logradouro?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} WHERE l.id_logradouro = @Id";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_POR_ID",
                $"Erro ao obter logradouro por ID {id}: {ex.Message}",
                ex);
        }
    }

    public async Task<IEnumerable<Logradouro>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} ORDER BY l.nome";

            await using var command = await CreateCommandAsync(query, cancellationToken);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var logradouros = new List<Logradouro>();

            while (await reader.ReadAsync(cancellationToken))
            {
                logradouros.Add(Map(reader));
            }

            return logradouros;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_TODOS",
                $"Erro ao obter todos os logradouros: {ex.Message}",
                ex);
        }
    }

    public async Task<Logradouro> Adicionar(Logradouro entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = FormatInsertQuery(
                "INSERT INTO tb_logradouro (cep, nome, bairro, cidade, estado, pais) " +
                "VALUES (@Cep, @Nome, @Bairro, @Cidade, @Estado, @Pais)");

            await using var command = await CreateCommandAsync(query, cancellationToken);

            command.AddParameter("@Cep", entity.Cep.Valor, DbType.String);
            command.AddParameter("@Nome", entity.Nome, DbType.String);
            command.AddParameter("@Bairro", entity.Bairro, DbType.String);
            command.AddParameter("@Cidade", entity.Cidade, DbType.String);
            command.AddParameter("@Estado", entity.Estado, DbType.String);
            command.AddParameter("@Pais", entity.Pais, DbType.String);

            int id = await command.ExecuteScalarIdAsync(
                "ERRO_ADICIONAR_LOGRADOURO",
                "Falha ao obter ID inserido para o logradouro.",
                cancellationToken);

            var idProperty = typeof(Entity).GetProperty("Id");
            idProperty?.SetValue(entity, id);

            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_ADICIONAR_LOGRADOURO",
                $"Erro ao adicionar logradouro: {ex.Message}",
                ex);
        }
    }

    public async Task<Logradouro> Atualizar(Logradouro entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query =
                "UPDATE tb_logradouro SET cep = @Cep, nome = @Nome, bairro = @Bairro, " +
                "cidade = @Cidade, estado = @Estado, pais = @Pais WHERE id_logradouro = @Id";

            await using var command = await CreateCommandAsync(query, cancellationToken);

            command.AddParameter("@Id", entity.Id, DbType.Int32);
            command.AddParameter("@Cep", entity.Cep.Valor, DbType.String);
            command.AddParameter("@Nome", entity.Nome, DbType.String);
            command.AddParameter("@Bairro", entity.Bairro, DbType.String);
            command.AddParameter("@Cidade", entity.Cidade, DbType.String);
            command.AddParameter("@Estado", entity.Estado, DbType.String);
            command.AddParameter("@Pais", entity.Pais, DbType.String);

            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                throw new InfrastructureException(
                    "REGISTRO_NAO_ENCONTRADO",
                    $"Nenhum logradouro encontrado com ID {entity.Id} para atualizacao.");
            }

            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_ATUALIZAR_LOGRADOURO",
                $"Erro ao atualizar logradouro ID {entity.Id}: {ex.Message}",
                ex);
        }
    }

    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = "DELETE FROM tb_logradouro WHERE id_logradouro = @Id";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);

            var result = await command.ExecuteNonQueryAsync(cancellationToken);
            return result > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_REMOVER_LOGRADOURO",
                $"Erro ao remover logradouro ID {id}: {ex.Message}",
                ex);
        }
    }

    public async Task<Logradouro?> ObterPorCep(Cep cep, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} WHERE l.cep = @Cep";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Cep", cep.Valor, DbType.String);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_POR_CEP",
                $"Erro ao obter logradouro por CEP {cep.Valor}: {ex.Message}",
                ex);
        }
    }

    public async Task<IEnumerable<Logradouro>> ObterPorCidade(
        string cidade,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} WHERE l.cidade = @Cidade ORDER BY l.nome";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Cidade", cidade, DbType.String);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var logradouros = new List<Logradouro>();

            while (await reader.ReadAsync(cancellationToken))
            {
                logradouros.Add(Map(reader));
            }

            return logradouros;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_POR_CIDADE",
                $"Erro ao obter logradouros por cidade {cidade}: {ex.Message}",
                ex);
        }
    }

    public async Task<IEnumerable<Logradouro>> ObterPorBairro(
        string cidade,
        string bairro,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} WHERE l.cidade = @Cidade AND l.bairro = @Bairro ORDER BY l.nome";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Cidade", cidade, DbType.String);
            command.AddParameter("@Bairro", bairro, DbType.String);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var logradouros = new List<Logradouro>();

            while (await reader.ReadAsync(cancellationToken))
            {
                logradouros.Add(Map(reader));
            }

            return logradouros;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_OBTER_POR_BAIRRO",
                $"Erro ao obter logradouros por bairro {bairro} da cidade {cidade}: {ex.Message}",
                ex);
        }
    }

    public async Task<bool> CepJaExiste(
        Cep cep,
        int? id = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string query =
                "SELECT COUNT(1) FROM tb_logradouro " +
                "WHERE cep = @Cep AND (@Id IS NULL OR id_logradouro <> @Id)";

            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Cep", cep.Valor, DbType.String);
            command.AddParameter("@Id", (object?)id ?? DBNull.Value, DbType.Int32);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return count > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException(
                "ERRO_VERIFICAR_CEP",
                $"Erro ao verificar existencia de CEP: {ex.Message}",
                ex);
        }
    }

    public static Logradouro Map(DbDataReader reader, string nomeColumn = "nome")
    {
        try
        {
            int id = reader.GetInt32Value("id_logradouro");
            string cep = reader.GetStringValue("cep");
            string nome = reader.GetStringValue(nomeColumn);
            string bairro = reader.GetStringValue("bairro");
            string cidade = reader.GetStringValue("cidade");
            string estado = reader.GetStringValue("estado");
            string pais = reader.GetStringValue("pais");

            var result = Logradouro.Criar(
                id: id,
                cep: cep,
                nome: nome,
                bairro: bairro,
                cidade: cidade,
                estado: estado,
                pais: pais);

            if (result.IsFailure)
            {
                throw new InfrastructureException(
                    "ERRO_DOMINIO_MAPEAMENTO",
                    $"Erro de dominio ao mapear logradouro ID {id}: " +
                    $"{string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
            }

            return result.Value!;
        }
        catch (Exception ex) when (ex is not InfrastructureException)
        {
            throw new InfrastructureException(
                "ERRO_MAPEAMENTO_LOGRADOURO",
                $"Erro ao mapear dados do logradouro: {ex.Message}",
                ex);
        }
    }
}
