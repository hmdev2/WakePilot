using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Security;

namespace RemoteWake.Domain.Tests;

[TestClass]
public sealed class IdentifiersAndSecurityTests
{
    [TestMethod]
    public void StrongIdentifiersAreTypedAndRejectEmptyValues()
    {
        var computer = ComputerId.New();
        var bridge = BridgeId.New();
        var request = RequestId.New();
        var target = TargetId.New();
        var correlation = CorrelationId.New();

        Assert.AreNotEqual(Guid.Empty, computer.Value);
        Assert.AreEqual(computer.Value, ComputerId.From(computer.Value).Value);
        Assert.AreEqual(bridge.Value, BridgeId.From(bridge.Value).Value);
        Assert.AreEqual(request.Value, RequestId.From(request.Value).Value);
        Assert.AreEqual(target.Value, TargetId.From(target.Value).Value);
        Assert.AreEqual(correlation.Value, CorrelationId.From(correlation.Value).Value);
        Assert.AreEqual(computer.Value.ToString("D"), computer.ToString());

        Assert.ThrowsExactly<ArgumentException>(() => ComputerId.From(Guid.Empty));
        Assert.ThrowsExactly<ArgumentException>(() => BridgeId.From(Guid.Empty));
        Assert.ThrowsExactly<ArgumentException>(() => RequestId.From(Guid.Empty));
        Assert.ThrowsExactly<ArgumentException>(() => TargetId.From(Guid.Empty));
        Assert.ThrowsExactly<ArgumentException>(() => CorrelationId.From(Guid.Empty));
    }

    [TestMethod]
    public void CryptographicNonceIsBase64UrlAndRedactedByDefault()
    {
        var generator = new CryptographicNonceGenerator();

        var first = generator.Create();
        var second = generator.Create();
        var encoded = first.RevealForTransport();

        Assert.AreEqual(43, encoded.Length);
        Assert.IsTrue(encoded.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));
        Assert.AreNotEqual(encoded, second.RevealForTransport());
        Assert.AreEqual("[REDACTED]", first.ToString());
        Assert.AreEqual(first, Nonce.Parse(encoded));
    }

    [TestMethod]
    public void NonceParserRejectsMalformedInput()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Nonce.Parse(""));
        Assert.ThrowsExactly<FormatException>(() => Nonce.Parse(new string('a', 42)));
        Assert.ThrowsExactly<FormatException>(() => Nonce.Parse(new string('=', 43)));
    }
}
