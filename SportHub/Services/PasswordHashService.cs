using System.Buffers.Binary;
using System.Security.Cryptography;

namespace SportHub.Services;

// ======================================================
// ACCOUNT MODULE - PASSWORD HASHING SERVICE
// Uses PBKDF2 without ASP.NET Core Identity Framework.
// ======================================================
public sealed class PasswordHashService
{
    private const string Prefix = "PBKDF2-SHA256";
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return string.Join(
            '$',
            Prefix,
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool VerifyPassword(
        string storedHash,
        string suppliedPassword,
        out bool needsRehash)
    {
        needsRehash = false;

        if (string.IsNullOrWhiteSpace(storedHash) ||
            string.IsNullOrEmpty(suppliedPassword))
        {
            return false;
        }

        if (storedHash.StartsWith($"{Prefix}$", StringComparison.Ordinal))
        {
            return VerifyCurrentHash(storedHash, suppliedPassword, out needsRehash);
        }

        // Existing accounts may contain hashes produced before the project
        // removed its direct Identity PasswordHasher dependency.
        var verifiedLegacyHash = VerifyLegacyIdentityHash(
            storedHash,
            suppliedPassword);

        needsRehash = verifiedLegacyHash;
        return verifiedLegacyHash;
    }

    private static bool VerifyCurrentHash(
        string storedHash,
        string password,
        out bool needsRehash)
    {
        needsRehash = false;
        var parts = storedHash.Split('$');

        if (parts.Length != 4 ||
            !int.TryParse(parts[1], out var iterations) ||
            iterations < 10_000)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            var verified = CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);

            needsRehash = verified && iterations < Iterations;
            return verified;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool VerifyLegacyIdentityHash(
        string storedHash,
        string password)
    {
        byte[] decoded;

        try
        {
            decoded = Convert.FromBase64String(storedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        if (decoded.Length < 13)
        {
            return false;
        }

        return decoded[0] switch
        {
            0x00 => VerifyIdentityV2(decoded, password),
            0x01 => VerifyIdentityV3(decoded, password),
            _ => false
        };
    }

    private static bool VerifyIdentityV2(byte[] decoded, string password)
    {
        const int saltLength = 16;
        const int subkeyLength = 32;

        if (decoded.Length != 1 + saltLength + subkeyLength)
        {
            return false;
        }

        var salt = decoded.AsSpan(1, saltLength).ToArray();
        var expected = decoded.AsSpan(1 + saltLength, subkeyLength);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            1_000,
            HashAlgorithmName.SHA1,
            subkeyLength);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool VerifyIdentityV3(byte[] decoded, string password)
    {
        var prf = BinaryPrimitives.ReadUInt32BigEndian(decoded.AsSpan(1, 4));
        var iterations = BinaryPrimitives.ReadUInt32BigEndian(decoded.AsSpan(5, 4));
        var saltLength = BinaryPrimitives.ReadUInt32BigEndian(decoded.AsSpan(9, 4));

        if (iterations == 0 ||
            iterations > int.MaxValue ||
            saltLength < 16 ||
            saltLength > int.MaxValue ||
            decoded.Length <= 13 + (int)saltLength)
        {
            return false;
        }

        var hashAlgorithm = prf switch
        {
            0 => HashAlgorithmName.SHA1,
            1 => HashAlgorithmName.SHA256,
            2 => HashAlgorithmName.SHA512,
            _ => default
        };

        if (hashAlgorithm == default)
        {
            return false;
        }

        var salt = decoded.AsSpan(13, (int)saltLength).ToArray();
        var expected = decoded.AsSpan(13 + (int)saltLength);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            (int)iterations,
            hashAlgorithm,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
