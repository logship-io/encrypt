using encrypt.Encryptors.E2E;
using encrypt.Encryptors.Metadata;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;
using System.Text.Json;

namespace encrypt.Commands
{
    internal static class EncryptCommand
    {
        public static Command CreateInstance()
        {
            var command = new Command("enc", "Encrypts input. Either from a file or from standard input")
            {
                Description = "Encrypts input using AES256 with HMAC256 for integrity. The output is a binary file that contains the salt, IV, and encrypted data, followed by the HMAC."
            };

            var inputFile = new Argument<string>("input")
            {
                Description = "The output file path. If the '-' character is passed, the input will be read from stdin."
            };

            var outputFile = new Argument<string>("output")
            {
                Description = "The output file path. If the '-' character is passed, the output will be written to stdout."
            };

            var password = new Option<string>("--password", "-p");
            password.Description = "The password to encrypt with. If not provided here, input will be prompted.";

            command.Arguments.Add(inputFile);
            command.Arguments.Add(outputFile);
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

                if (string.IsNullOrEmpty(passwordValue))
                {
                    if (readFromStdin)
                    {
                        Console.Error.WriteLine("Cannot read password from stdin when input is stdin. Please specify the source file, or specify the password on the command line. If you didn't expect this, make sure you're specifying the input from a file.");
                        return -1;
                    }

                    Console.WriteLine("Please enter password:");
                    passwordValue = Console.ReadLine();
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

                WriteEncryptorTypeHeader(EncryptorTypes.AES256HMAC256, outputFileStream);
                await AESHMACEncryptor.Encrypt(new EncryptedFileHiddenMetadata()
                {
                    InputFileName = readFromStdin ? "encrypted-file" : inputFileValue!
                }, Encoding.UTF8.GetBytes(passwordValue!), inputFileStream, outputFileStream, token);

                return 0;
            });

            return command;
        }

        private static void WriteEncryptorTypeHeader(
            EncryptorTypes encryptorType,
            Stream outputFileStream)
        {
            var values = new EncryptedFilePublicMetadata
            {
                EncryptorType = encryptorType
            };

            JsonSerializer.Serialize(outputFileStream, values, EncryptSourceGenerationContext.Default.EncryptedFilePublicMetadata);
        }

        private static string DefaultToLine(ArgumentResult input)
        {
            return "-";
        }
    }
}
