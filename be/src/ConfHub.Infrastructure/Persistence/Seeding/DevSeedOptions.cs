namespace ConfHub.Infrastructure.Persistence.Seeding;

// Options pattern: mục "DevSeed", chỉ có trong appsettings.Development.json.
public sealed class DevSeedOptions
{
    public const string SectionName = "DevSeed";

    // Mặc định tắt: môi trường nào không khai báo thì không có tài khoản mẫu.
    public bool Enabled { get; init; }

    // Mật khẩu chung của các tài khoản mẫu.
    public string Password { get; init; } = string.Empty;
}
