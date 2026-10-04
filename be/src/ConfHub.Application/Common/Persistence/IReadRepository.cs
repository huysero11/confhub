using Ardalis.Specification;
using ConfHub.Domain.Common;

namespace ConfHub.Application.Common.Persistence;

// Repository chỉ đọc: dùng khi handler chỉ cần tra cứu (vd tìm vai trò theo mã).
public interface IReadRepository<T> : IReadRepositoryBase<T>
    where T : class, IAggregateRoot
{
}
