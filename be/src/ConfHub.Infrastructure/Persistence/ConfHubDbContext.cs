using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ConfHub.Infrastructure.Persistence;

// DbContext duy nhất của hệ thống: cầu nối giữa code C# và CSDL SQL Server.
// Hiện chỉ có 3 bảng kỹ thuật của MassTransit; bảng nghiệp vụ (User, Role...) thêm từ T1.1.
public sealed class ConfHubDbContext(DbContextOptions<ConfHubDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Bảng cho Transactional Outbox/Inbox của MassTransit:
        // - OutboxMessage: sự kiện chờ gửi, ghi cùng transaction với dữ liệu nghiệp vụ.
        // - OutboxState: tiến độ gửi của từng lần SaveChanges.
        // - InboxState: tin nhắn đã nhận, để không xử lý trùng một tin nhắn hai lần.
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.AddInboxStateEntity();
    }
}
