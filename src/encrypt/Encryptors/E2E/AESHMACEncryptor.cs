using encrypt.Utilities;
using System.Security.Cryptography;

namespace encrypt.Encryptors.E2E
{
    internal static class AESHMACEncryptor
    {
        public static async Task Encrypt(byte[] password, Stream input, Stream output, CancellationToken token)
        {
            var salt = new byte[64];
            RandomNumberGenerator.Fill(salt);

            // Write the password salt to the output stream
            output.Write(salt);

            var aesKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA3_512, 64);
            var passKey = aesKey.AsMemory().Slice(0, 32);
            var hmacKey = aesKey.AsMemory().Slice(32, 32);

            // First create the IV
            var initializationVector = new byte[16];
            RandomNumberGenerator.Fill(initializationVector);

            using var hmac = new HMACSHA256(hmacKey.ToArray());

            using (var hmacStream = new CryptoStream(output, hmac, CryptoStreamMode.Write, true))
            {
                // Write the IV to the hamc stream
                hmacStream.Write(initializationVector);

                using (var aes = Aes.Create())
                {
                    aes.Key = passKey.ToArray();
                    aes.IV = initializationVector;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.ISO10126;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        using (var cryptorStream = new CryptoStream(hmacStream, encryptor, CryptoStreamMode.Write))
                        {
                            await input.CopyToAsync(cryptorStream, token);
                            await cryptorStream.FlushFinalBlockAsync(token);
                        }
                    }

                    aes.Clear();
                }
            }

            // At the end of it all, write the hmac
            output.Write(hmac.Hash!, 0, hmac.Hash!.Length);
        }

        public static async Task Decrypt(byte[] password, Stream input, Stream output, CancellationToken token)
        {
            var salt = new byte[64];

            // First, read the salt from the input stream
            input.ReadExactly(salt);

            var aesKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA3_512, 64);
            var passKey = aesKey.AsMemory().Slice(0, 32);
            var hmacKey = aesKey.AsMemory().Slice(32, 32);

            // Now lets calculate the hmac for the whole stream
            using var hmac = new HMACSHA256(hmacKey.ToArray());
            using (var readExceptOffsetStream = new ReadExceptOffsetStream(input, 32))
            {
                using (var hmacStream = new CryptoStream(readExceptOffsetStream, hmac, CryptoStreamMode.Read))
                {
                    // Read the IV from the hmac stream
                    var initializationVector = new byte[16];
                    hmacStream.ReadExactly(initializationVector);
                    using (var aes = Aes.Create())
                    {
                        aes.Key = passKey.ToArray();
                        aes.IV = initializationVector;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.ISO10126;
                        using (var decryptor = aes.CreateDecryptor())
                        {
                            using (var cryptorStream = new CryptoStream(hmacStream, decryptor, CryptoStreamMode.Read))
                            {
                                // Copy the decrypted data to the output stream
                                await cryptorStream.CopyToAsync(output, token);
                            }
                        }
                        aes.Clear();
                    }
                }

                // Now we need to verify the HMAC
                var calculatedHash = hmac.Hash!;
                var fileHash = readExceptOffsetStream.OffsetBuffer;
                if (calculatedHash.Length != fileHash.Length || !calculatedHash.SequenceEqual(fileHash))
                {
                    throw new CryptographicException("HMAC verification failed. The data may have been tampered with or the password is incorrect.");
                }
            }
        }
    }
}
