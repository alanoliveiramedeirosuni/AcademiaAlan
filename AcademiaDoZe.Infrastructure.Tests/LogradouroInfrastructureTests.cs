// Alan Medeiros
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Exceptions;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class LogradouroInfrastructureTests : TestBase
{
    private readonly LogradouroRepository _logradouroRepo;

    public LogradouroInfrastructureTests()
    {
        _logradouroRepo = new LogradouroRepository(ConnectionString, DatabaseType);
    }

    internal static async Task<Logradouro> CriarEInserirLogradouroAsync(
        LogradouroRepository logradouroRepo)
    {
        var logradouroResult = Logradouro.Criar(
            id: 0,
            cep: GerarCep(),
            nome: "Rua Teste " + Guid.NewGuid().ToString("N")[..5],
            bairro: "Centro",
            cidade: "Curitiba",
            estado: "PR",
            pais: "Brasil");

        if (logradouroResult.IsFailure)
        {
            throw new Exception(
                $"Falha ao criar Logradouro: " +
                $"{string.Join(", ", logradouroResult.Notifications.Select(n => n.Mensagem))}");
        }

        return await logradouroRepo.Adicionar(logradouroResult.Value!);
    }

    [Fact]
    public async Task Logradouro_Adicionar_E_ObterPorId_Sucesso()
    {
        var logradouro = await CriarEInserirLogradouroAsync(_logradouroRepo);

        Assert.NotNull(logradouro);
        Assert.True(logradouro.Id > 0);

        var obtido = await _logradouroRepo.ObterPorId(logradouro.Id);

        Assert.NotNull(obtido);
        Assert.Equal(logradouro.Id, obtido.Id);
        Assert.Equal(logradouro.Cep.Valor, obtido.Cep.Valor);
        Assert.Equal(logradouro.Nome, obtido.Nome);
        Assert.Equal(logradouro.Cidade, obtido.Cidade);
        Assert.Equal(logradouro.Estado, obtido.Estado);
    }

    [Fact]
    public async Task Logradouro_ObterPorId_RetornaNuloQuandoInexistente()
    {
        var obtido = await _logradouroRepo.ObterPorId(999999);

        Assert.Null(obtido);
    }

    [Fact]
    public async Task Logradouro_ObterTodos_Sucesso()
    {
        await CriarEInserirLogradouroAsync(_logradouroRepo);

        var todos = await _logradouroRepo.ObterTodos();

        Assert.NotNull(todos);
        Assert.NotEmpty(todos);
    }

    [Fact]
    public async Task Logradouro_Atualizar_Sucesso()
    {
        var logradouro = await CriarEInserirLogradouroAsync(_logradouroRepo);

        var novoNome = "Rua Editada " + Guid.NewGuid().ToString("N")[..5];

        var logradouroAtualizado = Logradouro.Criar(
            id: logradouro.Id,
            cep: logradouro.Cep.Valor,
            nome: novoNome,
            bairro: "Batel",
            cidade: logradouro.Cidade,
            estado: logradouro.Estado,
            pais: logradouro.Pais).Value!;

        var resultado = await _logradouroRepo.Atualizar(logradouroAtualizado);

        Assert.NotNull(resultado);
        Assert.Equal(novoNome, resultado.Nome);

        var noBanco = await _logradouroRepo.ObterPorId(logradouro.Id);

        Assert.NotNull(noBanco);
        Assert.Equal(novoNome, noBanco.Nome);
        Assert.Equal("Batel", noBanco.Bairro);
    }

    [Fact]
    public async Task Logradouro_Atualizar_LancaExcecaoQuandoInexistente()
    {
        var logradouroInexistente = Logradouro.Criar(
            id: 999999,
            cep: GerarCep(),
            nome: "Inexistente",
            bairro: "Centro",
            cidade: "Curitiba",
            estado: "PR",
            pais: "Brasil").Value!;

        var ex = await Assert.ThrowsAsync<InfrastructureException>(
            () => _logradouroRepo.Atualizar(logradouroInexistente));

        Assert.Equal("REGISTRO_NAO_ENCONTRADO", ex.ErrorCode);
    }

    [Fact]
    public async Task Logradouro_Remover_Sucesso()
    {
        var logradouro = await CriarEInserirLogradouroAsync(_logradouroRepo);

        var removido = await _logradouroRepo.Remover(logradouro.Id);
        Assert.True(removido);

        var noBanco = await _logradouroRepo.ObterPorId(logradouro.Id);
        Assert.Null(noBanco);
    }

    [Fact]
    public async Task Logradouro_Remover_RetornaFalseQuandoInexistente()
    {
        var removido = await _logradouroRepo.Remover(999999);

        Assert.False(removido);
    }

    [Fact]
    public async Task Logradouro_ObterPorCep_SucessoENulo()
    {
        var logradouro = await CriarEInserirLogradouroAsync(_logradouroRepo);

        var obtido = await _logradouroRepo.ObterPorCep(logradouro.Cep);
        Assert.NotNull(obtido);
        Assert.Equal(logradouro.Id, obtido.Id);

        var cepInexistente = Cep.Criar(GerarCep()).Value!;
        var naoObtido = await _logradouroRepo.ObterPorCep(cepInexistente);
        Assert.Null(naoObtido);
    }

    [Fact]
    public async Task Logradouro_ObterPorCidade_FiltragemCorreta()
    {
        var logradouro = await CriarEInserirLogradouroAsync(_logradouroRepo);

        var resultados = await _logradouroRepo.ObterPorCidade(logradouro.Cidade);

        Assert.NotNull(resultados);
        Assert.Contains(resultados, l => l.Id == logradouro.Id);
    }

    [Fact]
    public async Task Logradouro_ObterPorBairro_FiltragemCorreta()
    {
        var logradouro = await CriarEInserirLogradouroAsync(_logradouroRepo);

        var resultados = await _logradouroRepo.ObterPorBairro(logradouro.Cidade, logradouro.Bairro);

        Assert.NotNull(resultados);
        Assert.Contains(resultados, l => l.Id == logradouro.Id);
        Assert.All(resultados, l => Assert.Equal(logradouro.Bairro, l.Bairro));
    }

    [Fact]
    public async Task Logradouro_CepJaExiste_ValidacaoCorreta()
    {
        var logradouro = await CriarEInserirLogradouroAsync(_logradouroRepo);

        var existe = await _logradouroRepo.CepJaExiste(logradouro.Cep);
        Assert.True(existe);

        var existeIgnorandoId = await _logradouroRepo.CepJaExiste(logradouro.Cep, logradouro.Id);
        Assert.False(existeIgnorandoId);

        var cepInedito = Cep.Criar(GerarCep()).Value!;
        var existeInedito = await _logradouroRepo.CepJaExiste(cepInedito);
        Assert.False(existeInedito);
    }
}
