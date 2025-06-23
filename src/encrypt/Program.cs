
using encrypt.Commands;
using System.CommandLine;

var ctx = new CancellationTokenSource();

Console.CancelKeyPress += Console_CancelKeyPress;

void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
{
    ctx.Cancel();
}

var command = new RootCommand("Encryption command line tool.");
command.Add(EncryptCommand.CreateInstance());
command.Add(DecryptCommand.CreateInstance());

var parseResult = command.Parse(args);
if (parseResult.Errors.Count > 0)
{
    foreach (var parseError in parseResult.Errors)
    {
        Console.Error.WriteLine(parseError.Message);
    }

    return 1;
}

await parseResult.InvokeAsync(ctx.Token);

return 0;