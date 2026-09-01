using System.Security.Cryptography;
using System.Text;

namespace Agent.Api.Infrastructure.Hashing;

public static class FileHashHelper
{
    public static async Task<string> ComputeSha256Async(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();

        var hash = await sha256.ComputeHashAsync(
            stream,
            cancellationToken);

        return Convert.ToHexString(hash);
    }

    public static string ComputeTextHash(string text)
    {
        var bytes =
            System.Text.Encoding.UTF8.GetBytes(
                text.Trim());

        var hash =
            SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    public static string ComputeSha256(string content)
    {
        var bytes =
            Encoding.UTF8.GetBytes(
                content.Trim());

        var hash =
            SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

}