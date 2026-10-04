using ConfHub.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConfHub.Infrastructure.Messaging;

// Đăng ký DI cho phần sự kiện: MassTransit + RabbitMQ + Outbox/Inbox + consumer.
// Được gọi từ DependencyInjection.AddInfrastructure.
internal static class MessagingServiceRegistration
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind json vào class RabbitMqOptions, và validate các thuộc tính lúc khởi tạo
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMassTransit(bus =>
        {
            /*
                - Outbox ở phía GỬI: khi publish event thì ghi vào bảng OutboxMessage cùng
                transaction với SaveChanges của DbContext.
                - UseBusOutbox() là bật dịch vụ nền đọc bảng để gửi lên RabbitMQ
            */
            bus.AddEntityFrameworkOutbox<ConfHubDbContext>(outbox =>
            {
                outbox.UseSqlServer();
                outbox.UseBusOutbox();
            });

            /*
                Áp dụng cho mọi CONSUMER:
                - Thử lại khi lỗi sau 1s, 5s, 15s (lỗi tạm thời như mạng chập chờn tự khỏi)
                - Inbox phía NHẬN: ghi nhận tin nhắn đã xử lý
            */
            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                endpoint.UseMessageRetry(retry => retry.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)));
                endpoint.UseEntityFrameworkOutbox<ConfHubDbContext>(context);
            });

            // add comsumer trong project để tự tìm các consumer
            bus.AddConsumers(typeof(MessagingServiceRegistration).Assembly);

            // nói với MassTransit là dùng RabbitMQ làm transport, và cấu hình kết nối
            bus.UsingRabbitMq((context, rabbit) =>
            {
                var rabbitMqOptions = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                // tham số: host, virtual host, và credentials
                rabbit.Host(rabbitMqOptions.Host, "/", credentials =>
                {
                    credentials.Username(rabbitMqOptions.Username);
                    credentials.Password(rabbitMqOptions.Password);
                });

                // Tự tạo hàng đợi trên RabbitMQ cho từng consumer đã đăng ký.
                rabbit.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
