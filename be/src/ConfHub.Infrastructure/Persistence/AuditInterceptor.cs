using ConfHub.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ConfHub.Infrastructure.Persistence;

// Interceptor của EF Core: chạy ngay trước khi SaveChanges ghi xuống CSDL.
// Gán CreatedAt cho mọi bản ghi mới → không handler nào phải tự gán.
// Được gắn vào EF Core bằng AddInterceptors trong PersistenceServiceRegistration.cs (và OutboxFixture.cs của test).
public sealed class AuditInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetCreatedAt(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetCreatedAt(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void SetCreatedAt(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entry in dbContext.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedAt).CurrentValue = now;
            }
        }
    }
}
