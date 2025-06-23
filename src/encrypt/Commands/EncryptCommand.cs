using encrypt.Encryptors.Symetric;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Security.Cryptography;
using System.Text;

namespace encrypt.Commands
{
    internal static class EncryptCommand
    {
        public static Command CreateInstance()
        {
            var command = new Command("enc", "Encrypts input");

            var inputFile = new Argument<string>("input")
            {
                DefaultValueFactory = DefaultToLine,
                Description = "The output file path. If no value is passed, or if the '-' character is passed, the input will be read from stdin."
            };

            var outputFile = new Argument<string>("output")
            {
                DefaultValueFactory = DefaultToLine,
                Description = "The output file path. If no value is passed, or if the '-' character is passed, the output will be written to stdout."
            };
            var password = new Option<string>("-p,--password");
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

                if (string.IsNullOrEmpty(passwordValue))
                {
                    if (string.Equals(inputFileValue, "-", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.Error.WriteLine("Cannot read password from stdin when input is stdin. Please specify the source file, or specify the password on the command line.");
                        return -1;
                    }

                    Console.WriteLine("Please enter password:");
                    passwordValue = Console.ReadLine();
                }

                if (token.IsCancellationRequested)
                {
                    return -2;
                }

                using var inputFileStream = string.Equals(inputFileValue, "-", StringComparison.OrdinalIgnoreCase) 
                    ? Console.OpenStandardInput()
                    : File.OpenRead(inputFileValue!);

                using var outputFileStream = string.Equals(outputFileValue, "-", StringComparison.OrdinalIgnoreCase)
                    ? Console.OpenStandardOutput()
                    : File.OpenWrite(outputFileValue!);


                var enc = new AES256();
                await enc.Encrypt(Encoding.UTF8.GetBytes(passwordValue), inputFileStream, outputFileStream, token);

                return 0;
            });

            return command;
        }

        private static string DefaultToLine(ArgumentResult input)
        {
            return "-";
        }
    }
}
