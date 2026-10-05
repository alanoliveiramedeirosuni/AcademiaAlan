// Alan Medeiros
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Services;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Entities;

public sealed class Logradouro : Entity
{
    public Cep Cep { get; }
    public string Nome { get; }
    public string Bairro { get; }
    public string Cidade { get; }
    public string Estado { get; }
    public string Pais { get; }

    private Logradouro(
        int id,
        Cep cep,
        string nome,
        string bairro,
        string cidade,
        string estado,
        string pais) : base(id)
    {
        Cep = cep;
        Nome = nome;
        Bairro = bairro;
        Cidade = cidade;
        Estado = estado;
        Pais = pais;
    }

    public static Result<Logradouro> Criar(
        int id,
        string? cep,
        string? nome,
        string? bairro,
        string? cidade,
        string? estado,
        string? pais)
    {
        var notifications = new List<Notification>();

        var cepResult = Cep.Criar(cep);
        if (cepResult.IsFailure)
            notifications.AddRange(cepResult.Notifications);

        ValidarTexto(nome, "Nome", "NOME_OBRIGATORIO", notifications, out nome);
        ValidarTexto(bairro, "Bairro", "BAIRRO_OBRIGATORIO", notifications, out bairro);
        ValidarTexto(cidade, "Cidade", "CIDADE_OBRIGATORIA", notifications, out cidade);

        if (NormalizadoService.TextoVazioOuNulo(estado))
            notifications.Add(new Notification("Estado", "ESTADO_OBRIGATORIO"));
        else
            estado = NormalizadoService.ParaMaiusculo(
                NormalizadoService.LimparTodosEspacos(estado));

        ValidarTexto(pais, "Pais", "PAIS_OBRIGATORIO", notifications, out pais);

        if (notifications.Count != 0)
            return Result<Logradouro>.Failure(notifications);

        return Result<Logradouro>.Success(
            new Logradouro(id, cepResult.Value!, nome!, bairro!, cidade!, estado!, pais!));
    }

    private static void ValidarTexto(
        string? valor,
        string propriedade,
        string mensagem,
        ICollection<Notification> notifications,
        out string? valorNormalizado)
    {
        if (NormalizadoService.TextoVazioOuNulo(valor))
        {
            notifications.Add(new Notification(propriedade, mensagem));
            valorNormalizado = valor;
            return;
        }

        valorNormalizado = NormalizadoService.LimparEspacos(valor);
    }
}
