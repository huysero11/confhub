using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ConfHub.Infrastructure.Persistence;

public sealed class ConfHubDbContext(DbContextOptions<ConfHubDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConfHubDbContext).Assembly);

        /*
            OutboxMessage = chứa message thực tế
            OutboxState   = theo dõi việc gửi outbox
            InboxState    = theo dõi message consumer đã nhận/xử lý
        */
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.AddInboxStateEntity();
    }
}
