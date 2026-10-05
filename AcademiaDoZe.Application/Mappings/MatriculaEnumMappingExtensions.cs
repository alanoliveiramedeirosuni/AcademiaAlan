// Alan Medeiros
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Domain.Enums;

namespace AcademiaDoZe.Application.Mappings;

public static class MatriculaEnumMappingExtensions
{
    public static MatriculaPlano ToDomain(this AppMatriculaPlano appPlano)
    {
        return (MatriculaPlano)appPlano;
    }

    public static AppMatriculaPlano ToApplication(this MatriculaPlano domainPlano)
    {
        return (AppMatriculaPlano)domainPlano;
    }

    public static MatriculaRestricoes ToDomain(this AppMatriculaRestricoes appRestricoes)
    {
        return (MatriculaRestricoes)appRestricoes;
    }

    public static AppMatriculaRestricoes ToApplication(this MatriculaRestricoes domainRestricoes)
    {
        return (AppMatriculaRestricoes)domainRestricoes;
    }

    public static DateOnly CalcularDataFim(this MatriculaPlano plano, DateOnly dataInicio) => plano switch
    {
        MatriculaPlano.Mensal => dataInicio.AddMonths(1),
        MatriculaPlano.Trimestral => dataInicio.AddMonths(3),
        MatriculaPlano.Semestral => dataInicio.AddMonths(6),
        MatriculaPlano.Anual => dataInicio.AddMonths(12),
        _ => throw new ArgumentOutOfRangeException(nameof(plano), plano, "Plano de matrícula inválido.")
    };
}
