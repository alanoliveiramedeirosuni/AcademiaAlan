// Alan Medeiros
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Services;

namespace AcademiaDoZe.Domain.ValueObjects;

public sealed record Cpf
{
    public string Valor { get; }

    private Cpf(string valor)
    {
        Valor = valor;
    }

    public static Result<Cpf> Criar(string? valor)
    {
        if (NormalizadoService.TextoVazioOuNulo(valor))
            return Result<Cpf>.Failure("Cpf", "CPF_OBRIGATORIO");

        var textoLimpo = NormalizadoService.LimparEDigitos(valor);
        if (!Validar(textoLimpo))
            return Result<Cpf>.Failure("Cpf", "CPF_INVALIDO");

        return Result<Cpf>.Success(new Cpf(textoLimpo));
    }

    private static bool Validar(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            return false;

        var primeiroDigito = CalcularDigito(cpf[..9], 10);
        var segundoDigito = CalcularDigito(cpf[..10], 11);

        return cpf[9] - '0' == primeiroDigito && cpf[10] - '0' == segundoDigito;
    }

    private static int CalcularDigito(string numeros, int pesoInicial)
    {
        var soma = 0;
        for (var indice = 0; indice < numeros.Length; indice++)
            soma += (numeros[indice] - '0') * (pesoInicial - indice);

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    public override string ToString() => Valor;
}
