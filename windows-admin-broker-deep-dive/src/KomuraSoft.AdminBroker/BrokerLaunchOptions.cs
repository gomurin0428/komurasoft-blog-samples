namespace KomuraSoft.AdminBroker;

public sealed class BrokerLaunchOptions
{
    public required string PipeName { get; init; }
    public required int ExpectedClientProcessId { get; init; }
    public required string ClientUserSid { get; init; }

    public static BrokerLaunchOptions Parse(string[] args)
    {
        string? pipeName = null;
        int? clientPid = null;
        string? clientSid = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--pipe":
                    pipeName = ReadNextValue(args, ref i, "--pipe");
                    break;
                case "--client-pid":
                    string pidText = ReadNextValue(args, ref i, "--client-pid");
                    if (!int.TryParse(pidText, out int pid) || pid <= 0)
                    {
                        throw new ArgumentException($"Invalid client PID: {pidText}");
                    }

                    clientPid = pid;
                    break;
                case "--client-sid":
                    clientSid = ReadNextValue(args, ref i, "--client-sid");
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        if (string.IsNullOrWhiteSpace(pipeName))
        {
            throw new ArgumentException("--pipe is required.");
        }

        if (clientPid is null)
        {
            throw new ArgumentException("--client-pid is required.");
        }

        if (string.IsNullOrWhiteSpace(clientSid))
        {
            throw new ArgumentException("--client-sid is required.");
        }

        return new BrokerLaunchOptions
        {
            PipeName = pipeName,
            ExpectedClientProcessId = clientPid.Value,
            ClientUserSid = clientSid
        };
    }

    private static string ReadNextValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"A value is required after {optionName}.");
        }

        index++;
        return args[index];
    }
}
