using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace ConfHub.Application.Common.Security;

public static class RandomToken
{
    private const int ByteLength = 32; // 256 bits

    // Base64Url: chỉ gồm A-Z a-z 0-9 - _ nên đặt thẳng vào URL được
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(ByteLength);
        return Base64Url.EncodeToString(bytes);
    }

    // SHA-256 đủ an toàn cho token ngẫu nhiên dài (khác mật khẩu: không cần băm chậm)
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes); // hex dễ lưu vào CSDL, dễ debug
    }
}
