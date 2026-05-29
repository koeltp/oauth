using System.Security.Cryptography;
using System.Text;

namespace OAuth.Infrastructure;

public static class OpenIddictIdentifier
{
    public static string GenerateClientId()
    {
        return Guid.NewGuid().ToString("N");
    }

    public static string GenerateClientSecret()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}