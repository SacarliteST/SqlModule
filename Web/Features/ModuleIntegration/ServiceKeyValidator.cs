using System.Security.Cryptography;
using System.Text;

namespace SQLModule.Web.Features.ModuleIntegration;

internal static class ServiceKeyValidator
{
    internal static bool IsValid(string? actual, string expected)
    {
        if (String.IsNullOrEmpty(actual) || String.IsNullOrEmpty(expected))
        {
            return false;
        }

        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return actualBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
