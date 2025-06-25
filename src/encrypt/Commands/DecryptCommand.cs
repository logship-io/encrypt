using encrypt.Encryptors.E2E;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;

namespace encrypt.Commands
{
    internal static class DecryptCommand
    {
        public static Command CreateInstance()
        {
            var command = new Command("dec", "Decrypts input, either from a file or from stdin.")
            {
                Description = "Decrypts input using AES256 with HMAC256 for integrity. The input is a binary file that contains the salt, IV, encrypted data, and the HMAC."
            };

            var inputFile = new Option<string>("--input", "-i")
            {
                DefaultValueFactory = DefaultToLine,
                Description = "The output file path. If no value is passed, or if the '-' character is passed, the input will be read from stdin."
            };

            var outputFile = new Option<string>("--output", "-o")
            {
                DefaultValueFactory = DefaultToLine,
                Description = "The output file path. If no value is passed, or if the '-' character is passed, the output will be written to stdout."
            };
            var password = new Option<string>("--password", "-p");
            password.Description = "The password to decrypt with. If not provided here, input will be prompted.";

            command.Options.Add(inputFile);
            command.Options.Add(outputFile);
            command.Options.Add(password);

            command.SetAction(async (result, token) =>
            {
                var inputFileValue = result.GetValue(inputFile);
                var outputFileValue = result.GetValue(outputFile);
                var passwordValue = result.GetValue(password);

                if (string.IsNullOrWhiteSpace(inputFileValue))
                {
                    Console.Error.WriteLine("Invalid argument for input file");
                }

                if (string.IsNullOrWhiteSpace(outputFileValue))
                {
                    Console.Error.WriteLine("Invalid argument for output file");
                }

                var readFromStdin = string.Equals(inputFileValue, "-", StringComparison.OrdinalIgnoreCase);
                var writeToStdout = string.Equals(outputFileValue, "-", StringComparison.OrdinalIgnoreCase);

                if (false == readFromStdin)
                {
                    if (false == File.Exists(inputFileValue!))
                    {
                        Console.Error.WriteLine($"Input file '{inputFileValue}' does not exist.");
                        return -1;
                    }
                }

                if (token.IsCancellationRequested)
                {
                    return -2;
                }

                using var inputFileStream = readFromStdin
                    ? Console.OpenStandardInput()
                    : File.OpenRead(inputFileValue!);

                using var outputFileStream = writeToStdout
                    ? Console.OpenStandardOutput()
                    : File.OpenWrite(outputFileValue!);

                if (string.IsNullOrEmpty(passwordValue))
                {
                    if (readFromStdin)
                    {
                        Console.Error.WriteLine("Cannot read password from stdin when input is stdin. Please specify the source file, or specify the password on the command line.");
                        return -1;
                    }

                    Console.WriteLine("Please enter password:");
                    passwordValue = Console.ReadLine();
                }

                // Read the encryption type.
                var encryptorType = ReadEncryptorTypeFromStream(inputFileStream);

                if (encryptorType != EncryptorTypes.AES256HMAC256)
                {
                    Console.Error.WriteLine($"Unsupported encryptor type: {encryptorType}");
                    return -1;
                }

                try
                {
                    await AESHMACEncryptor.Decrypt(Encoding.UTF8.GetBytes(passwordValue!), inputFileStream, outputFileStream, token);
                }
                catch (Exception)
                {
                    Console.Error.WriteLine($"Invalid password or the file is corrupt.");
                    return -1;
                }

                return 0;
            });

            return command;
        }

        private static string DefaultToLine(ArgumentResult input)
        {
            return "-";
        }

        private static EncryptorTypes ReadEncryptorTypeFromStream(Stream stream)
        {
            unsafe
            {
                ushort value = 0;
                var buffer = new Span<byte>(&value, sizeof(ushort));
                stream.ReadExactly(buffer);

                return (EncryptorTypes)value;
            }
        }
    }
}
