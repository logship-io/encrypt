
using encrypt.Commands;
using System.CommandLine;

var ctx = new CancellationTokenSource();

Console.CancelKeyPress += Console_CancelKeyPress;

void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    ctx.Cancel();
}

var command = new RootCommand("Encryption command line tool.");
command.Subcommands.Add(EncryptCommand.CreateInstance());
command.Subcommands.Add(DecryptCommand.CreateInstance());

var conf = new CommandLineConfiguration(command);
conf.EnableDefaultExceptionHandler = true;

var parseResult = conf.Parse(args);

await parseResult.InvokeAsync(ctx.Token);

return 0;