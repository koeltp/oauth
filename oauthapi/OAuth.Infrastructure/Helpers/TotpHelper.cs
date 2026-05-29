using System;
using System.Security.Cryptography;
using System.Text;

namespace OAuth.Infrastructure.Helpers;

public static class TotpHelper
{
    private const int Step = 30;
    private const int CodeLength = 6;

    public static bool Validate(string? secret, string? code)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code))
            return false;

        try
        {
            var secretBytes = Base32Decode(secret);
            
            // TOTP时间步长数 = Unix时间戳 / 30
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var currentStep = currentTime / Step;
            
            // 允许前后各一个时间步长的误差
            for (int offset = -1; offset <= 1; offset++)
            {
                var timestamp = currentStep + offset;
                var expectedCode = GenerateCode(secretBytes, timestamp);
                if (expectedCode == code)
                    return true;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    public static string GenerateCode(byte[] secret, long timestamp)
    {
        var timeBytes = BitConverter.GetBytes(timestamp);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeBytes);

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(timeBytes);

        var offset = hash[hash.Length - 1] & 0xf;
        var binary = (hash[offset] & 0x7f) << 24
                   | (hash[offset + 1] & 0xff) << 16
                   | (hash[offset + 2] & 0xff) << 8
                   | (hash[offset + 3] & 0xff);

        var code = binary % (int)Math.Pow(10, CodeLength);
        return code.ToString($"D{CodeLength}");
    }

    public static string GenerateSecret()
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var random = new Random();
        var bytes = new byte[16];
        random.NextBytes(bytes);
        
        var result = new StringBuilder();
        for (int i = 0; i < bytes.Length; i += 5)
        {
            int remaining = Math.Min(5, bytes.Length - i);
            ulong value = 0;
            for (int j = 0; j < remaining; j++)
            {
                value = (value << 8) | bytes[i + j];
            }
            
            int bits = remaining * 8;
            while (bits > 0)
            {
                bits -= 5;
                result.Append(base32Chars[(int)(value >> bits) & 31]);
            }
        }
        
        return result.ToString();
    }

    private static byte[] Base32Decode(string base32)
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        base32 = base32.ToUpper().TrimEnd('=');
        
        int bits = 0;
        int value = 0;
        int index = 0;
        
        byte[] result = new byte[base32.Length * 5 / 8];
        
        foreach (char c in base32)
        {
            int charIndex = base32Chars.IndexOf(c);
            if (charIndex < 0)
                throw new FormatException("Invalid Base32 character");
            
            value = (value << 5) | charIndex;
            bits += 5;
            
            if (bits >= 8)
            {
                bits -= 8;
                result[index++] = (byte)(value >> bits);
            }
        }
        
        return result;
    }
}
