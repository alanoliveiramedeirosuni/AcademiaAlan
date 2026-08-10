// Alan Medeiros
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Services;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Entities;

public class Matricula : Entity
{
    public Aluno AlunoMatricula { get; private set; }
    public MatriculaPlano Plano { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly DataFim { get; private set; }
    public string Objetivo { get; private set; }
    public MatriculaRestricoes RestricoesMedicas { get; private set; }
    public string ObservacoesRestricoes { get; private set; }
    public Arquivo? LaudoMedico { get; private set; }

    private Matricula(
        int id,
        Aluno alunoMatricula,
        MatriculaPlano plano,
        DateOnly dataInicio,
        DateOnly dataFim,
        string objetivo,
        MatriculaRestricoes restricoesMedicas,
        Arquivo? laudoMedico,
        string observacoesRestricoes) : base(id)
    {
        AlunoMatricula = alunoMatricula;
        Plano = plano;
        DataInicio = dataInicio;
        DataFim = dataFim;
        Objetivo = objetivo;
        RestricoesMedicas = restricoesMedicas;
        LaudoMedico = laudoMedico;
        ObservacoesRestricoes = observacoesRestricoes;
    }

    public static Result<Matricula> Criar(
        int id,
        Aluno? alunoMatricula,
        MatriculaPlano plano,
        DateOnly dataInicio,
        DateOnly dataFim,
        string? objetivo,
        MatriculaRestricoes restricoesMedicas,
        Arquivo? laudoMedico,
        string? observacoesRestricoes = "")
    {
        var notifications = new List<Notification>();

        if (alunoMatricula is null)
            notifications.Add(new Notification("AlunoMatricula", "ALUNO_OBRIGATORIO"));

        if (!Enum.IsDefined(plano))
            notifications.Add(new Notification("Plano", "PLANO_INVALIDO"));

        if (dataInicio == default)
            notifications.Add(new Notification("DataInicio", "DATA_INICIO_OBRIGATORIA"));

        if (dataFim == default)
            notifications.Add(new Notification("DataFim", "DATA_FIM_OBRIGATORIA"));
        else if (dataInicio != default && dataFim <= dataInicio)
            notifications.Add(new Notification("DataFim", "DATA_FIM_INVALIDA"));

        if (NormalizadoService.TextoVazioOuNulo(objetivo))
            notifications.Add(new Notification("Objetivo", "OBJETIVO_OBRIGATORIO"));
        else
            objetivo = NormalizadoService.LimparEspacos(objetivo);

        const MatriculaRestricoes restricoesValidas =
            MatriculaRestricoes.Diabetes |
            MatriculaRestricoes.PressaoAlta |
            MatriculaRestricoes.Labirintite |
            MatriculaRestricoes.Alergias |
            MatriculaRestricoes.ProblemasRespiratorios |
            MatriculaRestricoes.RemedioContinuo;

        if ((restricoesMedicas & ~restricoesValidas) != 0)
            notifications.Add(new Notification("RestricoesMedicas", "RESTRICOES_INVALIDAS"));

        observacoesRestricoes = NormalizadoService.LimparEspacos(observacoesRestricoes);

        var exigeLaudoPorIdade = alunoMatricula is not null &&
            dataInicio != default &&
            CalcularIdade(alunoMatricula.DataNascimento, dataInicio) is >= 12 and <= 16;

        var possuiRestricao = restricoesMedicas != MatriculaRestricoes.Nenhuma;
        if ((exigeLaudoPorIdade || possuiRestricao) && laudoMedico is null)
            notifications.Add(new Notification("LaudoMedico", "LAUDO_MEDICO_OBRIGATORIO"));

        if (notifications.Count != 0)
            return Result<Matricula>.Failure(notifications);

        return Result<Matricula>.Success(new Matricula(
            id,
            alunoMatricula!,
            plano,
            dataInicio,
            dataFim,
            objetivo!,
            restricoesMedicas,
            laudoMedico,
            observacoesRestricoes));
    }

    private static int CalcularIdade(DateOnly nascimento, DateOnly referencia)
    {
        var idade = referencia.Year - nascimento.Year;
        if (nascimento > referencia.AddYears(-idade))
            idade--;

        return idade;
    }
}
