namespace RemoteWake.M1.Harness;

internal sealed class CommandLine
{
    private readonly Dictionary<string, string> values;
    private readonly HashSet<string> switches;

    private CommandLine(string command, Dictionary<string, string> values, HashSet<string> switches)
    {
        Command = command;
        this.values = values;
        this.switches = switches;
    }

    public string Command { get; }

    public static CommandLine Parse(string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Length == 0)
        {
            throw new ArgumentException("Informe um comando: configure, health ou wake.");
        }

        var command = arguments[0];
        if (command is not ("configure" or "health" or "wake"))
        {
            throw new ArgumentException("Comando inválido. Use configure, health ou wake.");
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var switches = new HashSet<string>(StringComparer.Ordinal);
        ParseOptions(arguments, values, switches);
        ValidateAllowedOptions(command, values.Keys, switches);
        return new CommandLine(command, values, switches);
    }

    public string Required(string name) => values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new ArgumentException($"A opção {name} é obrigatória.");

    public bool HasSwitch(string name) => switches.Contains(name);

    private static void ParseOptions(
        string[] arguments,
        IDictionary<string, string> values,
        HashSet<string> switches)
    {
        for (var index = 1; index < arguments.Length; index++)
        {
            var option = arguments[index];
            if (!option.StartsWith("--", StringComparison.Ordinal) || option.Length == 2)
            {
                throw new ArgumentException("Todas as opções devem usar o formato --nome valor.");
            }

            if (string.Equals(option, "--remove-identity-file", StringComparison.Ordinal))
            {
                if (!switches.Add(option))
                {
                    throw new ArgumentException("Uma opção não pode ser repetida.");
                }

                continue;
            }

            if (index + 1 >= arguments.Length || arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"A opção {option} exige um valor.");
            }

            if (!values.TryAdd(option, arguments[++index]))
            {
                throw new ArgumentException("Uma opção não pode ser repetida.");
            }
        }
    }

    private static void ValidateAllowedOptions(
        string command,
        IEnumerable<string> valueOptions,
        HashSet<string> switches)
    {
        var allowed = command == "configure"
            ? new HashSet<string>(StringComparer.Ordinal)
            {
                "--host", "--port", "--user", "--target-id", "--identity-file",
                "--host-key-file", "--confirm-host-fingerprint",
            }
            : new HashSet<string>(StringComparer.Ordinal);
        if (valueOptions.Any(key => !allowed.Contains(key)) ||
            (switches.Count != 0 && command != "configure"))
        {
            throw new ArgumentException("O comando recebeu uma opção desconhecida.");
        }
    }
}
