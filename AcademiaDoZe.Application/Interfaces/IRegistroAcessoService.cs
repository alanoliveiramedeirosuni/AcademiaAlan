// Alan Medeiros
using AcademiaDoZe.Application.DTOs;

namespace AcademiaDoZe.Application.Interfaces;

public interface IRegistroAcessoService
{
    Task<IReadOnlyList<AcessoDto>> ObterHistoricoAsync(bool colaborador, int pessoaId, CancellationToken ct = default);
    Task<ResultadoAcessoDto> RegistrarEntradaAsync(bool colaborador, int pessoaId, CancellationToken ct = default);
    Task<ResultadoAcessoDto> RegistrarSaidaAsync(bool colaborador, int pessoaId, CancellationToken ct = default);
}
