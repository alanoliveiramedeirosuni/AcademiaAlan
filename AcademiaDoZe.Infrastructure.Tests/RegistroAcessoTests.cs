// Alan Medeiros
using AcademiaDoZe.Application.Services;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public sealed class RegistroAcessoTests : IDisposable
{
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "academia-acessos-" + Guid.NewGuid().ToString("N"));
    private readonly RelogioTeste _relogio = new();
    private readonly RegistroAcessoService _servico;
    private readonly RegistroAcessoRepository _acessos;
    private readonly AlunoRepository _alunos;
    private readonly ColaboradorRepository _colaboradores;
    private readonly MatriculaRepository _matriculas;
    private readonly LogradouroRepository _logradouros;

    public RegistroAcessoTests()
    {
        var conexao = $"Data Source={Path.Combine(_pasta, "teste.db")};Pooling=False";
        _acessos = new(conexao, DatabaseType.Sqlite);
        _alunos = new(conexao, DatabaseType.Sqlite);
        _colaboradores = new(conexao, DatabaseType.Sqlite);
        _matriculas = new(conexao, DatabaseType.Sqlite);
        _logradouros = new(conexao, DatabaseType.Sqlite);
        DbInitializer.InitializeAsync(conexao, DatabaseType.Sqlite).GetAwaiter().GetResult();
        _servico = new(() => _acessos, () => _alunos, () => _colaboradores, () => _matriculas, _relogio);
    }

    private async Task<Aluno> AlunoAsync() => await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunos, _logradouros);
    private async Task MatricularAsync(Aluno aluno, DateOnly inicio, DateOnly fim)
    {
        var matricula = Matricula.Criar(0, aluno, MatriculaPlano.Mensal, inicio, fim, "Alan Medeiros", MatriculaRestricoes.Nenhuma, null);
        await _matriculas.Adicionar(matricula.Value!);
    }

    [Fact]
    public async Task AlunoSemMatricula_NaoRegistraEntrada()
    {
        var aluno = await AlunoAsync();
        var erro = await Assert.ThrowsAsync<InvalidOperationException>(() => _servico.RegistrarEntradaAsync(false, aluno.Id));
        Assert.Contains("matrícula ativa", erro.Message);
        Assert.Empty(await _servico.ObterHistoricoAsync(false, aluno.Id));
    }

    [Theory]
    [InlineData(-60, -30)]
    [InlineData(1, 31)]
    public async Task MatriculaVencidaOuFutura_NaoAutorizaEntrada(int inicio, int fim)
    {
        var aluno = await AlunoAsync();
        var hoje = DateOnly.FromDateTime(_relogio.Agora);
        await MatricularAsync(aluno, hoje.AddDays(inicio), hoje.AddDays(fim));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _servico.RegistrarEntradaAsync(false, aluno.Id));
    }

    [Fact]
    public async Task AlunoMatriculado_RegistraVisitaCompletaECalculaPermanencia()
    {
        var aluno = await AlunoAsync();
        var hoje = DateOnly.FromDateTime(_relogio.Agora);
        await MatricularAsync(aluno, hoje, hoje.AddDays(30));
        var entrada = await _servico.RegistrarEntradaAsync(false, aluno.Id);
        Assert.Contains("30 dias", entrada.Mensagem);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _servico.RegistrarEntradaAsync(false, aluno.Id));
        _relogio.Agora = _relogio.Agora.AddMinutes(90);
        var saida = await _servico.RegistrarSaidaAsync(false, aluno.Id);
        Assert.Contains("01h 30min", saida.Mensagem);
        var visita = Assert.Single(await _servico.ObterHistoricoAsync(false, aluno.Id));
        Assert.NotNull(visita.Saida);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _servico.RegistrarSaidaAsync(false, aluno.Id));
    }

    [Fact]
    public async Task MatriculaNoUltimoDia_AutorizaEntradaESaidaAposVencimento()
    {
        var aluno = await AlunoAsync();
        var hoje = DateOnly.FromDateTime(_relogio.Agora);
        await MatricularAsync(aluno, hoje.AddDays(-30), hoje);
        var entrada = await _servico.RegistrarEntradaAsync(false, aluno.Id);
        Assert.Contains("vence hoje", entrada.Mensagem);
        _relogio.Agora = _relogio.Agora.AddDays(1);
        await _servico.RegistrarSaidaAsync(false, aluno.Id);
        Assert.NotNull(Assert.Single(await _servico.ObterHistoricoAsync(false, aluno.Id)).Saida);
    }

    [Theory]
    [InlineData(ColaboradorVinculo.CLT, 8)]
    [InlineData(ColaboradorVinculo.Estagio, 6)]
    public async Task Colaborador_SomaVisitasDoDiaEBloqueiaNovaEntradaNoLimite(ColaboradorVinculo vinculo, int horas)
    {
        var pessoa = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradores, _logradouros);
        if (vinculo == ColaboradorVinculo.Estagio)
        {
            pessoa = Colaborador.Criar(pessoa.Id, pessoa.Nome, pessoa.Cpf.Valor, pessoa.DataNascimento,
                pessoa.Telefone.Valor, pessoa.Email.Valor, pessoa.Endereco.Logradouro,
                pessoa.Endereco.Numero, pessoa.Endereco.Complemento, "SenhaValida123", pessoa.Foto,
                pessoa.DataAdmissao, ColaboradorTipo.Instrutor, vinculo).Value!;
            await _colaboradores.Atualizar(pessoa);
        }
        await _servico.RegistrarEntradaAsync(true, pessoa.Id);
        _relogio.Agora = _relogio.Agora.AddHours(2);
        await _servico.RegistrarSaidaAsync(true, pessoa.Id);
        _relogio.Agora = _relogio.Agora.AddHours(1);
        await _servico.RegistrarEntradaAsync(true, pessoa.Id);
        _relogio.Agora = _relogio.Agora.AddHours(horas - 2);
        var saida = await _servico.RegistrarSaidaAsync(true, pessoa.Id);
        Assert.Contains($"Total de hoje: {horas:00}h 00min", saida.Mensagem);
        var erro = await Assert.ThrowsAsync<InvalidOperationException>(() => _servico.RegistrarEntradaAsync(true, pessoa.Id));
        Assert.Contains("Limite diário", erro.Message);
        Assert.Equal(2, (await _servico.ObterHistoricoAsync(true, pessoa.Id)).Count);
        _relogio.Agora = _relogio.Agora.Date.AddDays(1).AddHours(8);
        await _servico.RegistrarEntradaAsync(true, pessoa.Id);
    }

    [Fact]
    public async Task ExcessoDeJornada_NaoImpedeSaidaEMostraAviso()
    {
        var pessoa = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradores, _logradouros);
        await _servico.RegistrarEntradaAsync(true, pessoa.Id);
        _relogio.Agora = _relogio.Agora.AddHours(9);
        var saida = await _servico.RegistrarSaidaAsync(true, pessoa.Id);
        Assert.Contains("excedido em 01h", saida.Mensagem);
    }

    [Fact]
    public async Task BancoRecusaDuasEntradasAbertasDaMesmaPessoa()
    {
        var aluno = await AlunoAsync();
        var registro = RegistroAcesso.Criar(0, AcessoTipo.Aluno, aluno.Id, _relogio.Agora).Value!;
        await _acessos.RegistrarEntradaAsync(registro);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _acessos.RegistrarEntradaAsync(registro));
    }

    [Fact]
    public async Task SaidaSemEntrada_NaoCriaRegistro()
    {
        var aluno = await AlunoAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => _servico.RegistrarSaidaAsync(false, aluno.Id));
        Assert.Empty(await _servico.ObterHistoricoAsync(false, aluno.Id));
    }

    [Fact]
    public void VisitaAtravessandoMeiaNoite_ContaSomenteOTrechoDoDia()
    {
        var hoje = DateTime.Today;
        var visita = RegistroAcesso.Criar(1, AcessoTipo.Colaborador, 1, hoje.AddHours(-2), hoje.AddHours(3)).Value!;
        Assert.Equal(TimeSpan.FromHours(3), visita.TempoNoDia(hoje, hoje.AddHours(4)));
    }

    [Fact]
    public void HorariosExibidosEPermanencia_UsamAMesmaPrecisao()
    {
        var inicio = DateTime.Today.AddHours(8).AddMilliseconds(900);
        var fim = DateTime.Today.AddHours(8).AddSeconds(2).AddMilliseconds(100);
        var visita = RegistroAcesso.Criar(1, AcessoTipo.Colaborador, 1, inicio, fim).Value!;
        Assert.Equal(0, visita.Entrada.Millisecond);
        Assert.Equal(0, visita.Saida!.Value.Millisecond);
        Assert.Equal(TimeSpan.FromSeconds(2), visita.TempoNoDia(DateTime.Today, fim));
    }

    public void Dispose() { if (Directory.Exists(_pasta)) Directory.Delete(_pasta, true); }
    private sealed class RelogioTeste : TimeProvider
    {
        public DateTime Agora { get; set; } = DateTime.Today.AddHours(8);
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(Agora).ToUniversalTime();
    }
}
