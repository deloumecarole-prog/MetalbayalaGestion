using System;
using System.Security.Cryptography;
using System.Text;

namespace MetalBayalaGestion.Services;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100000;

    public static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(KeySize);

        var result = new byte[SaltSize + KeySize];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, result, SaltSize, KeySize);

        return Convert.ToBase64String(result);
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        var hashBytes = Convert.FromBase64String(hashedPassword);
        if (hashBytes.Length != SaltSize + KeySize) return false;

        var salt = new byte[SaltSize];
        var hash = new byte[KeySize];
        Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(hashBytes, SaltSize, hash, 0, KeySize);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var computedHash = pbkdf2.GetBytes(KeySize);

        return CryptographicOperations.FixedTimeEquals(hash, computedHash);
    }
}
