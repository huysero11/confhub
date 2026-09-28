using ConfHub.Infrastructure.Messaging;
using ConfHub.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConfHub.Infrastructure;

// Api gọi AddInfrastructure() một lần: đăng ký CSDL (EF Core) và đường ống sự kiện (MassTransit).
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // CSDL: chuỗi kết nối để ở mục "ConnectionStrings" theo quy ước của .NET
        // (EF Core và lệnh dotnet ef đều đọc chỗ này). Thiếu thì dừng ngay khi khởi động.
        var connectionString = configuration.GetConnectionString("ConfHub");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'ConnectionStrings:ConfHub'.");
        }

        services.AddDbContext<ConfHubDbContext>(options => options.UseSqlServer(connectionString));

        // Options pattern cho RabbitMQ: đọc mục "RabbitMq" vào RabbitMqOptions,
        // kiểm tra [Required] ngay lúc API khởi động thay vì đợi tới lúc kết nối mới lỗi.
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMassTransit(bus =>
        {
            // Outbox phía GỬI: publish không gửi thẳng lên RabbitMQ mà ghi vào bảng OutboxMessage
            // cùng transaction với SaveChanges; tiến trình nền gửi đi sau → không mất sự kiện khi sập giữa chừng.
            bus.AddEntityFrameworkOutbox<ConfHubDbContext>(outbox =>
            {
                outbox.UseSqlServer();
                outbox.UseBusOutbox();
            });

            // Áp dụng cho MỌI consumer:
            // - Thử lại khi lỗi sau 1s, 5s, 15s (lỗi tạm thời như mạng chập chờn tự khỏi).
            // - Inbox phía NHẬN: ghi nhận tin nhắn đã xử lý → cùng một tin nhắn đến hai lần chỉ xử lý một lần.
            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                endpoint.UseMessageRetry(retry => retry.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)));
                endpoint.UseEntityFrameworkOutbox<ConfHubDbContext>(context);
            });

            // Consumer viết trong project này tự được đăng ký (hiện chưa có; consumer đầu tiên ở T1.1).
            bus.AddConsumers(typeof(DependencyInjection).Assembly);

            bus.UsingRabbitMq((context, rabbit) =>
            {
                // Lấy cấu hình đã được kiểm tra hợp lệ từ DI (Options pattern).
                var rabbitMq = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                rabbit.Host(rabbitMq.Host, "/", credentials =>
                {
                    credentials.Username(rabbitMq.Username);
                    credentials.Password(rabbitMq.Password);
                });

                // Tự tạo hàng đợi trên RabbitMQ cho từng consumer đã đăng ký.
                rabbit.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
