using Ardalis.Specification;
using ConfHub.Domain.Common;

namespace ConfHub.Application.Common.Persistence;

// Repository pattern (thư viện Ardalis.Specification): đọc + ghi 1 loại aggregate root.
// Quy ước: ghi dữ liệu nghiệp vụ LUÔN qua repository
// → domain event được publish vào outbox trước khi lưu.
public interface IRepository<T> : IRepositoryBase<T>
    where T : class, IAggregateRoot
{
}
