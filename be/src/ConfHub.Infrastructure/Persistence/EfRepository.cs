using Ardalis.Specification.EntityFrameworkCore;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Domain.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ConfHub.Infrastructure.Persistence;

// Repository chung cho mọi aggregate root. RepositoryBase (Ardalis) đã có sẵn Add/Update/Delete,
// GetById, List/FirstOrDefault theo Specification; các hàm ghi đều gọi SaveChangesAsync bên dưới.
public sealed class EfRepository<T> : RepositoryBase<T>, IRepository<T>, IReadRepository<T>
    where T : class, IAggregateRoot
{
    // SQL Server: 2601 = trùng unique index, 2627 = trùng unique constraint.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly ConfHubDbContext _dbContext;
    private readonly DomainEventPublisher _domainEventPublisher;

    public EfRepository(ConfHubDbContext dbContext, DomainEventPublisher domainEventPublisher)
        : base(dbContext)
    {
        _dbContext = dbContext;
        _domainEventPublisher = domainEventPublisher;
    }

    // Publish domain event vào outbox trước, rồi mới lưu → dữ liệu và message cùng 1 transaction.
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _domainEventPublisher.PublishAsync(_dbContext, cancellationToken);

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Hai request cùng lúc vượt qua bước kiểm tra trùng (vd cùng email) → CSDL chặn lại.
            throw new ConflictException("Duplicate", "The record already exists.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && (sqlException.Number == UniqueIndexViolation || sqlException.Number == UniqueConstraintViolation);
    }
}
