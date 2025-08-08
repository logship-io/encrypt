using encrypt.Encryptors.E2E;
using encrypt.Encryptors.Metadata;
using encrypt.Utilities;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;
using System.Text.Json;

namespace encrypt.Commands
{
    internal static class DecryptCommand
    {
        private const string DEFAULTINPUT = "<inputpath>";

        public static Command CreateInstance()
        {
            var command = new Command("dec", "Decrypts input, either from a file or from stdin.")
            {
                Description = "Decrypts input using AES256 with HMAC256 for integrity. The input is a binary file that contains the salt, IV, encrypted data, and the HMAC."
            };

            var inputFile = new Argument<string>("input")
            {
                Description = "The output file path. If the '-' character is passed, the input will be read from stdin."
            };

            var outputFile = new Argument<string>("output")
            {
                DefaultValueFactory = DefaultToEmpty,
                Description = "The output file path. Not required. If not supplied, will default to the original filename. If the '-' character is passed, the output will be written to stdout. "
            };
            var password = new Option<string>("--password", "-p");
            password.Description = "The password to decrypt with. If not provided here, input will be prompted.";

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

                if (token.IsCancellationRequested)
                {
                    return -2;
                }

                using var inputFileStream = readFromStdin
                    ? Console.OpenStandardInput()
                    : File.OpenRead(inputFileValue!);

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
                var metadata = ReadEncryptorTypeFromStream(inputFileStream);

                if (metadata.EncryptorType != EncryptorTypes.AES256HMAC256)
                {
                    Console.Error.WriteLine($"Unsupported encryptor type: {metadata.EncryptorType}");
                    return -1;
                }

                try
                {
                    var outputStreamBuildre = CreateOutputStreamFactory(outputFileValue);
                    await AESHMACEncryptor.Decrypt(Encoding.UTF8.GetBytes(passwordValue!), inputFileStream, outputStreamBuildre, token);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    return -1;
                }

                return 0;
            });

            return command;
        }

        private static string DefaultToEmpty(ArgumentResult input)
        {
            return DEFAULTINPUT;
        }

        private static OutputStreamFactory CreateOutputStreamFactory(string filepath)
        {
            if (string.IsNullOrWhiteSpace(filepath) || filepath.Equals("-", StringComparison.OrdinalIgnoreCase))
            {
                return new OutputStreamFactory(Console.OpenStandardOutput());
            }

            if (string.Equals(filepath, DEFAULTINPUT, StringComparison.OrdinalIgnoreCase))
            {
                return new OutputStreamFactory(null);
            }

            if (File.Exists(filepath))
            {
                File.Delete(filepath);
                // throw new Exception($"File already exists at path: {filepath}. Please choose a different path.");
            }

            return new OutputStreamFactory(new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None));
        }

        private static EncryptedFilePublicMetadata ReadEncryptorTypeFromStream(Stream stream)
        {
            if (false == Utf8JsonObjectReader.ReadJsonObjectAsString(stream, out var jsonValue))
            {
                throw new InvalidOperationException("Make sure the input file was generated with this program. Failed to read file public metadata, which mean's it cannot be decrypted.");
            }

            var value = JsonSerializer.Deserialize(jsonValue.ToString(), EncryptSourceGenerationContext.Default.EncryptedFilePublicMetadata);
            if (value == null)
            {
                throw new InvalidOperationException("Make sure the input file was generated with this program and not altered. Failed to deserialize the file public metadata.");
            }
            return value;
        }
    }
}
