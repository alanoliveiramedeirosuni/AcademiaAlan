// Alan Medeiros
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Repositories;

namespace AcademiaDoZe.Application.Services;

public sealed class RegistroAcessoService(
    Func<IRegistroAcessoRepository> acessoFactory,
    Func<IAlunoRepository> alunoFactory,
    Func<IColaboradorRepository> colaboradorFactory,
    Func<IMatriculaRepository> matriculaFactory,
    TimeProvider relogio) : IRegistroAcessoService
{
    private DateTime Agora
    {
        get
        {
            var agora = relogio.GetLocalNow().DateTime;
            return new DateTime(agora.Year, agora.Month, agora.Day, agora.Hour, agora.Minute, agora.Second);
        }
    }
    private static AcessoTipo Tipo(bool colaborador) => colaborador ? AcessoTipo.Colaborador : AcessoTipo.Aluno;

    public async Task<IReadOnlyList<AcessoDto>> ObterHistoricoAsync(bool colaborador, int pessoaId, CancellationToken ct = default)
    {
        var registros = await acessoFactory().ObterPorPessoaAsync(Tipo(colaborador), pessoaId, ct);
        return registros.Select(r => new AcessoDto { Id = r.Id, Entrada = r.Entrada, Saida = r.Saida }).ToList();
    }

    public async Task<ResultadoAcessoDto> RegistrarEntradaAsync(bool colaborador, int pessoaId, CancellationToken ct = default)
    {
        var agora = Agora;
        var repo = acessoFactory();
        var registros = await repo.ObterPorPessoaAsync(Tipo(colaborador), pessoaId, ct);
        if (registros.Any(r => r.Saida is null))
            throw new InvalidOperationException("Já existe uma entrada em aberto. Registre a saída antes de entrar novamente.");
        if (registros.Any(r => r.Saida > agora))
            throw new InvalidOperationException("O relógio está anterior à última saída registrada. Confira o horário do dispositivo.");

        string nome, mensagem;
        if (colaborador)
        {
            var pessoa = await colaboradorFactory().ObterPorId(pessoaId, ct)
                ?? throw new InvalidOperationException("Colaborador não encontrado.");
            nome = pessoa.Nome;
            var limite = Limite(pessoa.Vinculo);
            var total = SomarDia(registros, agora);
            if (total >= limite)
                throw new InvalidOperationException($"Limite diário de {limite.TotalHours:0} horas atingido. Total de hoje: {AcessoDto.FormatarTempo(total)}.");
            mensagem = $"Entrada registrada. Jornada de {(pessoa.Vinculo == ColaboradorVinculo.CLT ? "CLT" : "estágio")}: {limite.TotalHours:0} horas. Tempo disponível hoje: {AcessoDto.FormatarTempo(limite - total)}.";
        }
        else
        {
            var pessoa = await alunoFactory().ObterPorId(pessoaId, ct)
                ?? throw new InvalidOperationException("Aluno não encontrado.");
            nome = pessoa.Nome;
            var hoje = DateOnly.FromDateTime(agora);
            var matriculas = await matriculaFactory().ObterPorAluno(pessoaId, ct);
            var ativa = matriculas.Where(m => m.DataInicio <= hoje && m.DataFim >= hoje)
                .OrderByDescending(m => m.DataFim).FirstOrDefault()
                ?? throw new InvalidOperationException("Entrada não permitida: este aluno não possui matrícula ativa. Cadastre ou renove a matrícula.");
            var dias = ativa.DataFim.DayNumber - hoje.DayNumber;
            mensagem = dias == 0
                ? $"Entrada registrada. Seu plano vence hoje, {ativa.DataFim:dd/MM/yyyy}."
                : $"Entrada registrada. Restam {dias} dias até o vencimento do plano em {ativa.DataFim:dd/MM/yyyy}.";
        }
        var registro = RegistroAcesso.Criar(0, Tipo(colaborador), pessoaId, agora);
        if (registro.IsFailure) throw new InvalidOperationException("Os dados da entrada são inválidos.");
        await repo.RegistrarEntradaAsync(registro.Value!, ct);
        return new(nome, mensagem);
    }

    public async Task<ResultadoAcessoDto> RegistrarSaidaAsync(bool colaborador, int pessoaId, CancellationToken ct = default)
    {
        var agora = Agora;
        var repo = acessoFactory();
        var registros = await repo.ObterPorPessoaAsync(Tipo(colaborador), pessoaId, ct);
        var aberto = registros.SingleOrDefault(r => r.Saida is null)
            ?? throw new InvalidOperationException("Não há entrada em aberto para esta pessoa. Registre a entrada primeiro.");
        if (agora < aberto.Entrada) throw new InvalidOperationException("A saída não pode ser anterior à entrada. Confira o relógio do dispositivo.");

        string nome;
        TimeSpan? limite = null;
        if (colaborador)
        {
            var pessoa = await colaboradorFactory().ObterPorId(pessoaId, ct)
                ?? throw new InvalidOperationException("Colaborador não encontrado.");
            nome = pessoa.Nome;
            limite = Limite(pessoa.Vinculo);
        }
        else nome = (await alunoFactory().ObterPorId(pessoaId, ct))?.Nome
            ?? throw new InvalidOperationException("Aluno não encontrado.");

        await repo.RegistrarSaidaAsync(aberto.Id, agora, ct);
        var mensagem = $"Saída registrada. Permanência nesta visita: {AcessoDto.FormatarTempo(agora - aberto.Entrada)}.";
        if (limite is { } jornada)
        {
            var total = SomarDia(registros, agora);
            mensagem += $" Total de hoje: {AcessoDto.FormatarTempo(total)} de {jornada.TotalHours:0} horas.";
            if (total > jornada) mensagem += $" Atenção: limite diário excedido em {AcessoDto.FormatarTempo(total - jornada)}.";
            else if (total == jornada) mensagem += " Limite diário atingido.";
        }
        return new(nome, mensagem);
    }

    private static TimeSpan Limite(ColaboradorVinculo vinculo) => TimeSpan.FromHours(vinculo == ColaboradorVinculo.CLT ? 8 : 6);
    private static TimeSpan SomarDia(IEnumerable<RegistroAcesso> registros, DateTime agora) =>
        TimeSpan.FromTicks(registros.Sum(r => r.TempoNoDia(agora, agora).Ticks));
}
