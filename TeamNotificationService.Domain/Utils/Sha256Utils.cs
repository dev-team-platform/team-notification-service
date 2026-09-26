using System.Security.Cryptography;
using System.Text;

namespace TeamNotificationService.Domain.Utils;

public static class Sha256Utils
{
    public static string ComputeHexHash(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}