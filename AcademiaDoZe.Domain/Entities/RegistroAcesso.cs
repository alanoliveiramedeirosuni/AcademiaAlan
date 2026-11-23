// Alan Medeiros
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Enums;

namespace AcademiaDoZe.Domain.Entities;

public sealed class RegistroAcesso : Entity
{
    public AcessoTipo Tipo { get; }
    public int PessoaId { get; }
    public DateTime Entrada { get; }
    public DateTime? Saida { get; }

    private RegistroAcesso(int id, AcessoTipo tipo, int pessoaId, DateTime entrada, DateTime? saida) : base(id)
    {
        Tipo = tipo;
        PessoaId = pessoaId;
        Entrada = SemFracoes(entrada);
        Saida = saida is { } fim ? SemFracoes(fim) : null;
    }

    private static DateTime SemFracoes(DateTime data) =>
        new(data.Year, data.Month, data.Day, data.Hour, data.Minute, data.Second);

    public static Result<RegistroAcesso> Criar(int id, AcessoTipo tipo, int pessoaId, DateTime entrada, DateTime? saida = null)
    {
        if (!Enum.IsDefined(tipo) || pessoaId <= 0)
            return Result<RegistroAcesso>.Failure("Pessoa", "Selecione uma pessoa cadastrada.");
        if (entrada == default || saida < entrada)
            return Result<RegistroAcesso>.Failure("DataHora", "A saída não pode ocorrer antes da entrada.");
        return Result<RegistroAcesso>.Success(new RegistroAcesso(id, tipo, pessoaId, entrada, saida));
    }

    public TimeSpan TempoNoDia(DateTime dia, DateTime agora)
    {
        var inicio = Entrada > dia.Date ? Entrada : dia.Date;
        var fim = Saida ?? agora;
        if (fim > dia.Date.AddDays(1)) fim = dia.Date.AddDays(1);
        return fim > inicio ? fim - inicio : TimeSpan.Zero;
    }
}
