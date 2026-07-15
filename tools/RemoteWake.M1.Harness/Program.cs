using System.Security.Cryptography;
using System.Text.Json;
using RemoteWake.Application.Models;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Security;
using RemoteWake.Domain.Time;
using RemoteWake.Infrastructure.Processes;
using RemoteWake.Infrastructure.SshBridge;
using RemoteWake.Infrastructure.Windows.Security;

namespace RemoteWake.M1.Harness;

internal static class Program
{
    private const int UsageError = 2;
    private const int OperationError = 1;

    public static async Task<int> Main(string[] arguments)
    {
        try
        {
            var commandLine = CommandLine.Parse(arguments);
            return commandLine.Command switch
            {
                "configure" => await ConfigureAsync(commandLine).ConfigureAwait(false),
                "health" => await ExecuteAsync(wake: false).ConfigureAwait(false),
                "wake" => await ExecuteAsync(wake: true).ConfigureAwait(false),
                _ => UsageError,
            };
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            WriteUsage();
            return UsageError;
        }
        catch (Exception exception) when (
            exception is IOException or InvalidDataException or UnauthorizedAccessException or
            CryptographicException or InvalidOperationException or JsonException)
        {
            Console.Error.WriteLine(exception.Message);
            return OperationError;
        }
    }

    private static async Task<int> ConfigureAsync(CommandLine arguments)
    {
        var paths = LabPaths.Create();
        if (File.Exists(paths.ProfilePath))
        {
            throw new InvalidOperationException(
                "Já existe uma configuração. Remova-a pelo procedimento de recuperação antes de parear novamente.");
        }

        var host = arguments.Required("--host");
        var userName = arguments.Required("--user");
        var port = ParsePort(arguments.Required("--port"));
        var targetId = ParseTargetId(arguments.Required("--target-id"));
        _ = new SshBridgeEndpoint(host, port, userName, targetId);
        var identityPath = CanonicalExistingFile(arguments.Required("--identity-file"));
        var hostKeyPath = CanonicalExistingFile(arguments.Required("--host-key-file"));
        var sshKeygenPath = GetOpenSshPath("ssh-keygen.exe");
        var sshPath = GetOpenSshPath("ssh.exe");

        await ValidateLauncherIdentityAsync(sshKeygenPath, identityPath).ConfigureAwait(false);
        var hostPublicKey = await File.ReadAllTextAsync(hostKeyPath).ConfigureAwait(false);
        var pin = HostKeyPin.Parse(
            hostPublicKey,
            arguments.Required("--confirm-host-fingerprint"));

        var privateKey = await ReadPrivateKeyAsync(identityPath).ConfigureAwait(false);

        Directory.CreateDirectory(paths.RootDirectory);
        var vault = new DpapiSecretVault(paths.VaultDirectory);
        SecretReference? storedReference = null;
        try
        {
            var stored = await vault.StoreAsync("m1-lab-ssh-identity", privateKey, CancellationToken.None)
                .ConfigureAwait(false);
            if (stored.IsFailure)
            {
                Console.Error.WriteLine($"{stored.Error!.Code}: {stored.Error.Reason}");
                return OperationError;
            }

            storedReference = stored.Value;
            WriteAtomically(paths.KnownHostsPath, pin.CreateKnownHostsEntry(host, port));
            var profile = LabProfile.Create(
                BridgeId.New(),
                targetId,
                host,
                port,
                userName,
                storedReference);
            profile.Save(paths.ProfilePath);
        }
        catch
        {
            if (storedReference is not null)
            {
                _ = await vault.RemoveAsync(storedReference, CancellationToken.None).ConfigureAwait(false);
            }

            File.Delete(paths.KnownHostsPath);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }

        var sourceRemoved = !arguments.HasSwitch("--remove-identity-file") || TryDeleteIdentity(identityPath);

        _ = sshPath;
        Console.WriteLine("Configuração protegida concluída.");
        Console.WriteLine("Execute 'RemoteWake.M1.Harness health' e depois 'RemoteWake.M1.Harness wake'.");
        if (sourceRemoved)
        {
            return 0;
        }

        Console.Error.WriteLine(
            "A configuração foi concluída, mas o arquivo privado de origem não pôde ser removido. Remova-o manualmente.");
        return OperationError;
    }

    private static async Task<int> ExecuteAsync(bool wake)
    {
        var paths = LabPaths.Create();
        var profile = LabProfile.Load(paths.ProfilePath);
        var bridgeId = profile.GetBridgeId();
        var targetId = profile.GetTargetId();
        var endpoint = new SshBridgeEndpoint(profile.Host, profile.Port, profile.UserName, targetId);
        var references = new Dictionary<BridgeId, SecretReference> { [bridgeId] = profile.GetSecretReference() };
        var vault = new DpapiSecretVault(paths.VaultDirectory);
        var leases = new DpapiPrivateKeyLeaseProvider(vault, references, paths.LeaseDirectory);
        var options = new SshBridgeOptions(
            GetOpenSshPath("ssh.exe"),
            Path.GetFullPath(paths.KnownHostsPath),
            new Dictionary<BridgeId, SshBridgeEndpoint> { [bridgeId] = endpoint });
        var processRunner = new SystemProcessRunner();
        var client = new SshBridgeClient(
            processRunner,
            leases,
            new SshPinnedHostKeyVerifier(processRunner, options),
            options,
            SystemClock.Instance,
            new CryptographicNonceGenerator());

        if (!wake)
        {
            var health = await client.GetHealthAsync(bridgeId, CancellationToken.None).ConfigureAwait(false);
            if (health.IsFailure)
            {
                Console.Error.WriteLine($"{health.Error!.Code}: {health.Error.Reason}");
                return OperationError;
            }

            Console.WriteLine("Celular de ativação acessível e autenticado.");
            return 0;
        }

        var receipt = await client.SendWakeAsync(
            bridgeId,
            new WakeCommand(
                RequestId.New(),
                targetId,
                SystemClock.Instance.UtcNow,
                new CryptographicNonceGenerator().Create()),
            CancellationToken.None).ConfigureAwait(false);
        if (receipt.IsFailure)
        {
            Console.Error.WriteLine($"{receipt.Error!.Code}: {receipt.Error.Reason}");
            return OperationError;
        }

        Console.WriteLine($"Solicitação aceita; {receipt.Value.PacketCount} pacotes de ativação foram enviados.");
        Console.WriteLine("O recibo confirma o envio, não que o computador já iniciou.");
        return 0;
    }

    private static async Task ValidateLauncherIdentityAsync(string executablePath, string identityPath)
    {
        var invocation = new ProcessInvocation(
            executablePath,
            ["-y", "-f", identityPath],
            TimeSpan.FromSeconds(5),
            4096);
        var result = await new SystemProcessRunner().RunAsync(invocation, CancellationToken.None).ConfigureAwait(false);
        var parts = result.StandardOutput.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (result.ExitCode != 0 || parts.Length < 2 ||
            !string.Equals(parts[0], "ssh-ed25519", StringComparison.Ordinal))
        {
            throw new InvalidDataException("A identidade do launcher deve ser uma chave OpenSSH Ed25519 sem senha.");
        }
    }

    private static ushort ParsePort(string value)
    {
        return ushort.TryParse(value, out var port) && port != 0
            ? port
            : throw new ArgumentException("A porta SSH deve estar entre 1 e 65535.");
    }

    private static TargetId ParseTargetId(string value)
    {
        return Guid.TryParseExact(value, "D", out var parsed) &&
            parsed != Guid.Empty &&
            string.Equals(parsed.ToString("D"), value, StringComparison.Ordinal)
            ? TargetId.From(parsed)
            : throw new ArgumentException("O target ID deve ser um UUID canônico e não vazio.");
    }

    private static async Task<byte[]> ReadPrivateKeyAsync(string path)
    {
        var content = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        if (content.Length is > 0 and <= 65_536)
        {
            return content;
        }

        CryptographicOperations.ZeroMemory(content);
        throw new InvalidDataException("A identidade SSH possui tamanho inválido.");
    }

    private static bool TryDeleteIdentity(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string GetOpenSshPath(string fileName)
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var path = Path.GetFullPath(Path.Combine(windows, "System32", "OpenSSH", fileName));
        return File.Exists(path)
            ? path
            : throw new FileNotFoundException("O cliente OpenSSH do Windows não foi encontrado.", path);
    }

    private static string CanonicalExistingFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return File.Exists(fullPath)
            ? fullPath
            : throw new FileNotFoundException("Um arquivo obrigatório não foi encontrado.", fullPath);
    }

    private static void WriteAtomically(string path, string content)
    {
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, content, System.Text.Encoding.ASCII);
            File.Move(temporary, path);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private static void WriteUsage()
    {
        Console.Error.WriteLine(
            "Uso: RemoteWake.M1.Harness configure --host HOST --port PORTA --user USUARIO " +
            "--target-id UUID --identity-file ARQUIVO --host-key-file ARQUIVO " +
            "--confirm-host-fingerprint SHA256:... [--remove-identity-file]");
        Console.Error.WriteLine("     RemoteWake.M1.Harness health");
        Console.Error.WriteLine("     RemoteWake.M1.Harness wake");
    }

    private sealed record LabPaths(
        string RootDirectory,
        string ProfilePath,
        string KnownHostsPath,
        string VaultDirectory,
        string LeaseDirectory)
    {
        public static LabPaths Create()
        {
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var root = Path.GetFullPath(Path.Combine(localData, "WakePilot", "M1Lab"));
            return new LabPaths(
                root,
                Path.Combine(root, "profile.json"),
                Path.Combine(root, "known_hosts"),
                Path.Combine(root, "vault"),
                Path.Combine(root, "leases"));
        }
    }
}
