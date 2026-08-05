using System.Security.Cryptography;

namespace GardenSystem.Application.Auth;

internal static class OpaqueTokenGenerator
{
    public static string Generate() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
