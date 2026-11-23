// Alan Medeiros
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;

namespace AcademiaDoZe.Domain.Repositories;

public interface IRegistroAcessoRepository
{
    Task<IReadOnlyList<RegistroAcesso>> ObterPorPessoaAsync(AcessoTipo tipo, int pessoaId, CancellationToken ct = default);
    Task RegistrarEntradaAsync(RegistroAcesso registro, CancellationToken ct = default);
    Task RegistrarSaidaAsync(int id, DateTime saida, CancellationToken ct = default);
}
