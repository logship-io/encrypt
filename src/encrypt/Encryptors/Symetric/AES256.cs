using System.Security.Cryptography;

namespace encrypt.Encryptors.Symetric
{
    internal sealed class AES256
    {
        public async Task Encrypt(byte[] password, Stream input, Stream output, CancellationToken token)
        {
            // First create the salt
            var salt = new byte[32];
            var initializationVector = new byte[16];
            RandomNumberGenerator.Fill(salt);
            RandomNumberGenerator.Fill(initializationVector);

            // Lets start by writing our details to the output stream...
            output.Write("S1"u8);
            output.Write(salt);
            output.Write(initializationVector);

            var aesKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA512, 32);

            using (var aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = initializationVector;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.ISO10126;

                using (var encryptor = aes.CreateEncryptor())
                {
                    using (var cryptorStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
                    {
                        await input.CopyToAsync(cryptorStream, token);
                        await cryptorStream.FlushFinalBlockAsync(token);
                    }
                }

                aes.Clear();
            }
        }

        public async Task Decrypt(byte[] password, Stream input, Stream output, CancellationToken token)
        {
            // Assume we've read the encyrption header.
            var salt = new byte[32];
            var initializationVector = new byte[16];
            input.ReadExactly(salt);
            input.ReadExactly(initializationVector);

            var aesKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA512, 32);

            using (var aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = initializationVector;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.ISO10126;

                using (var encryptor = aes.CreateDecryptor())
                {
                    using (var cryptorStream = new CryptoStream(input, encryptor, CryptoStreamMode.Read))
                    {
                        await cryptorStream.CopyToAsync(output, token);
                    }
                }

                aes.Clear();
            }
        }
    }
}
