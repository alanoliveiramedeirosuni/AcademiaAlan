// Alan Medeiros
using AcademiaDoZe.Domain.Entities;

namespace AcademiaDoZe.Domain.Repositories;

public interface IRepository<T> where T : Entity
{
    Task<T?> ObterPorId(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> ObterTodos(CancellationToken cancellationToken = default);

    Task<T> Adicionar(T entity, CancellationToken cancellationToken = default);

    Task<T> Atualizar(T entity, CancellationToken cancellationToken = default);

    Task<bool> Remover(int id, CancellationToken cancellationToken = default);
}
