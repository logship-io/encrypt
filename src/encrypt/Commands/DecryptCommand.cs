using encrypt.Encryptors.Symetric;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;

namespace encrypt.Commands
{
    internal static class DecryptCommand
    {
        public static Command CreateInstance()
        {
            var command = new Command("dec", "Decrypts input");

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

                // First read, the type from the input stream.
                byte[] type = new byte[2];
                inputFileStream.ReadExactly(type);

                if (type[0] != 'S' || type[1] != '1')
                {
                    Console.Error.WriteLine("Invalid encryption header. Expected 'S1'.");
                    return -1;
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

                var enc = new AES256();

                try
                {
                    await enc.Decrypt(Encoding.UTF8.GetBytes(passwordValue), inputFileStream, outputFileStream, token);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Invalid password.");
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
    }
}
