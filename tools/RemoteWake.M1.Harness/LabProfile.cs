using System.Text.Json;
using System.Text.Json.Serialization;
using RemoteWake.Application.Models;
using RemoteWake.Domain.Identifiers;
using RemoteWake.Infrastructure.SshBridge;

namespace RemoteWake.M1.Harness;

internal sealed record LabProfile(
    int Version,
    Guid BridgeId,
    Guid TargetId,
    string Host,
    ushort Port,
    string UserName,
    string SecretReference)
{
    private static readonly HashSet<string> ExpectedProperties = new(StringComparer.Ordinal)
    {
        "version",
        "bridgeId",
        "targetId",
        "host",
        "port",
        "userName",
        "secretReference",
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    public static LabProfile Create(
        BridgeId bridgeId,
        TargetId targetId,
        string host,
        ushort port,
        string userName,
        SecretReference secretReference)
    {
        ArgumentNullException.ThrowIfNull(bridgeId);
        ArgumentNullException.ThrowIfNull(targetId);
        ArgumentNullException.ThrowIfNull(secretReference);
        _ = new SshBridgeEndpoint(host, port, userName, targetId);
        ValidateSecretReference(secretReference.Name);
        return new LabProfile(1, bridgeId.Value, targetId.Value, host, port, userName, secretReference.Name);
    }

    public static LabProfile Load(string path)
    {
        var content = File.ReadAllBytes(path);
        if (content.Length is 0 or > 16_384)
        {
            throw new InvalidDataException("O perfil do laboratório possui tamanho inválido.");
        }

        using (var document = JsonDocument.Parse(content, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 4,
        }))
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("O perfil do laboratório deve ser um objeto JSON.");
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!names.Add(property.Name) || !ExpectedProperties.Contains(property.Name))
                {
                    throw new InvalidDataException("O perfil contém campo duplicado ou desconhecido.");
                }
            }

            if (!names.SetEquals(ExpectedProperties))
            {
                throw new InvalidDataException("O perfil não contém todos os campos obrigatórios.");
            }
        }

        var profile = JsonSerializer.Deserialize<LabProfile>(content, SerializerOptions)
            ?? throw new InvalidDataException("O perfil do laboratório não pôde ser lido.");
        profile.Validate();
        return profile;
    }

    public void Save(string path)
    {
        Validate();
        var content = JsonSerializer.SerializeToUtf8Bytes(this, SerializerOptions);
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("O caminho do perfil não possui diretório.");
        Directory.CreateDirectory(directory);
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllBytes(temporary, content);
            File.Move(temporary, path);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    public BridgeId GetBridgeId() => RemoteWake.Domain.Identifiers.BridgeId.From(BridgeId);

    public TargetId GetTargetId() => RemoteWake.Domain.Identifiers.TargetId.From(TargetId);

    public SecretReference GetSecretReference() => new(SecretReference);

    private void Validate()
    {
        if (Version != 1)
        {
            throw new InvalidDataException("A versão do perfil do laboratório é incompatível.");
        }

        var bridgeId = GetBridgeId();
        var targetId = GetTargetId();
        _ = bridgeId;
        _ = new SshBridgeEndpoint(Host, Port, UserName, targetId);
        ValidateSecretReference(SecretReference);
    }

    private static void ValidateSecretReference(string value)
    {
        if (!value.StartsWith("dpapi:", StringComparison.Ordinal) ||
            !Guid.TryParseExact(value[6..], "D", out var parsed) ||
            parsed == Guid.Empty)
        {
            throw new InvalidDataException("A referência protegida da identidade SSH é inválida.");
        }
    }
}
