using ConfHub.Application.Accounts.Specifications;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;
using Microsoft.Extensions.Options;

namespace ConfHub.Infrastructure.Persistence.Seeding;

// Tạo sẵn mỗi vai trò 1 tài khoản Active trên máy dev để thử API / giao diện mà không phải
// đăng ký + xác thực email. Cần thiết vì Nhân viên vận hành và Quản trị viên không tự đăng ký được.
// Chạy lại nhiều lần không sao: email đã có thì bỏ qua.
public sealed class DevAccountSeeder(
    IRepository<User> userRepository,
    IReadRepository<Role> roleRepository,
    IPasswordHasher passwordHasher,
    IOptions<DevSeedOptions> options)
{
    private static readonly DevAccount[] _devAccounts =
    [
        new("attendee@confhub.local", "Người tham dự mẫu", RoleCodes.Attendee, null),
        new("organizer@confhub.local", "Ban tổ chức mẫu", RoleCodes.Organizer, "ConfHub Demo"),
        new("supplier@confhub.local", "Nhà cung cấp mẫu", RoleCodes.Supplier, "ConfHub Catering"),
        new("staff@confhub.local", "Nhân viên vận hành mẫu", RoleCodes.Staff, null),
        new("admin@confhub.local", "Quản trị viên mẫu", RoleCodes.Admin, null),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seedOptions = options.Value;
        if (!seedOptions.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(seedOptions.Password))
        {
            throw new InvalidOperationException("DevSeed:Password is not configured.");
        }

        foreach (var account in _devAccounts)
        {
            var existingUser = await userRepository.FirstOrDefaultAsync(new UserByEmailSpec(account.Email), cancellationToken);
            if (existingUser is not null)
            {
                continue;
            }

            var role = await roleRepository.FirstOrDefaultAsync(new RoleByCodeSpec(account.RoleCode), cancellationToken)
                ?? throw new InvalidOperationException($"Role '{account.RoleCode}' is missing. Run the migrations first.");

            var user = User.CreateActive(
                account.Email,
                passwordHasher.Hash(seedOptions.Password),
                account.FullName,
                role,
                account.Organization);
            await userRepository.AddAsync(user, cancellationToken);
        }
    }

    private sealed record DevAccount(string Email, string FullName, string RoleCode, string? Organization);
}
