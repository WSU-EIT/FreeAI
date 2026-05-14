using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace FreeA11yChecker;

public partial interface IDataAccess
{
    string EncryptString(string plainText, string key);
    string DecryptString(string cipherText, string key);
}

public partial class DataAccess
{
    // Static salt for PBKDF2 key derivation (not secret, prevents rainbow tables)
    private static readonly byte[] _scanEncryptionSalt = new byte[] {
        0xA4, 0x3B, 0x7C, 0x1E, 0x9D, 0x5F, 0x2A, 0x8B,
        0xC6, 0xE1, 0x4D, 0x73, 0x0F, 0x96, 0xB8, 0x52
    };

    private const int _scanEncryptionIterations = 100000;
    private const int _scanEncryptionNonceSize = 12;  // AES-GCM standard nonce size
    private const int _scanEncryptionTagSize = 16;     // 128-bit authentication tag

    public string EncryptString(string plainText, string key)
    {
        if (String.IsNullOrEmpty(plainText)) {
            return String.Empty;
        }

        try {
            string resolvedKey = ResolveEncryptionKey(key);
            byte[] keyBytes = DeriveKeyBytes(resolvedKey);

            byte[] nonce = new byte[_scanEncryptionNonceSize];
            RandomNumberGenerator.Fill(nonce);

            byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[_scanEncryptionTagSize];

            using (AesGcm aesGcm = new AesGcm(keyBytes, _scanEncryptionTagSize)) {
                aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            // Concatenate: nonce (12) + tag (16) + ciphertext
            byte[] result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        } catch (Exception ex) {
            Console.WriteLine("Error in EncryptString: " + RecurseExceptionAsString(ex));
            return String.Empty;
        }
    }

    public string DecryptString(string cipherText, string key)
    {
        if (String.IsNullOrEmpty(cipherText)) {
            return String.Empty;
        }

        try {
            byte[] fullCipher = Convert.FromBase64String(cipherText);

            if (fullCipher.Length < _scanEncryptionNonceSize + _scanEncryptionTagSize) {
                return String.Empty;
            }

            // Extract nonce (first 12 bytes)
            byte[] nonce = new byte[_scanEncryptionNonceSize];
            Buffer.BlockCopy(fullCipher, 0, nonce, 0, _scanEncryptionNonceSize);

            // Extract tag (next 16 bytes)
            byte[] tag = new byte[_scanEncryptionTagSize];
            Buffer.BlockCopy(fullCipher, _scanEncryptionNonceSize, tag, 0, _scanEncryptionTagSize);

            // Extract ciphertext (remaining bytes)
            int cipherLength = fullCipher.Length - _scanEncryptionNonceSize - _scanEncryptionTagSize;
            byte[] cipherBytes = new byte[cipherLength];
            Buffer.BlockCopy(fullCipher, _scanEncryptionNonceSize + _scanEncryptionTagSize, cipherBytes, 0, cipherLength);

            string resolvedKey = ResolveEncryptionKey(key);
            byte[] keyBytes = DeriveKeyBytes(resolvedKey);

            byte[] plainBytes = new byte[cipherLength];

            using (AesGcm aesGcm = new AesGcm(keyBytes, _scanEncryptionTagSize)) {
                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            return System.Text.Encoding.UTF8.GetString(plainBytes);
        } catch (CryptographicException) {
            // Tag verification failed — data may be tampered
            throw;
        } catch (Exception ex) {
            Console.WriteLine("Error in DecryptString: " + RecurseExceptionAsString(ex));
            return String.Empty;
        }
    }

    private string ResolveEncryptionKey(string key)
    {
        if (!String.IsNullOrWhiteSpace(key)) {
            return key;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            // DPAPI fallback for Windows development environments
            byte[] entropy = System.Text.Encoding.UTF8.GetBytes("FreeA11yChecker_ScanCredentials");
            byte[] dpapiData = System.Text.Encoding.UTF8.GetBytes("FreeA11yChecker_DefaultKey");
            byte[] protectedData = System.Security.Cryptography.ProtectedData.Protect(
                dpapiData, entropy, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(protectedData);
        }

        throw new InvalidOperationException(
            "ScanEncryptionKey must be configured in appsettings.json on non-Windows platforms.");
    }

    private byte[] DeriveKeyBytes(string resolvedKey)
    {
        using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(
            resolvedKey, _scanEncryptionSalt, _scanEncryptionIterations, HashAlgorithmName.SHA256)) {
            return kdf.GetBytes(32); // 256 bits
        }
    }
}
