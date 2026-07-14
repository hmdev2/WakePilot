using RemoteWake.Domain.Identifiers;
using RemoteWake.Domain.Security;
using RemoteWake.Infrastructure.SshBridge;

namespace RemoteWake.M1.ContractTests;

[TestClass]
public sealed class BridgeProtocolTests
{
    private static readonly RequestId RequestId = RequestId.From(Guid.Parse("11111111-1111-4111-8111-111111111111"));
    private static readonly TargetId TargetId = TargetId.From(Guid.Parse("22222222-2222-4222-8222-222222222222"));
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Ct017RequestRoundTripsAsClosedBase64UrlEnvelope()
    {
        var request = new BridgeProtocolRequest(
            RequestId,
            TargetId,
            Now,
            Nonce.Parse(new string('A', 43)),
            BridgeAction.Wake);

        var encoded = BridgeProtocolCodec.EncodeRequest(request);
        var decoded = BridgeProtocolCodec.DecodeRequest(encoded);

        Assert.IsTrue(encoded.StartsWith("rwa1:", StringComparison.Ordinal));
        Assert.AreEqual(request, decoded);
        Assert.IsFalse(encoded.Contains('='));
        Assert.IsTrue(encoded.Length < 5500);
    }

    [TestMethod]
    public void Ct018ResponseRejectsUnknownFieldsCorrelationAndOversize()
    {
        var valid = ResponseJson(RequestId, "accepted", "OK", 3);
        var unknown = valid[..^1] + ",\"extra\":true}";
        Assert.ThrowsExactly<InvalidDataException>(() =>
            BridgeProtocolCodec.DecodeResponse(unknown, RequestId));
        Assert.ThrowsExactly<InvalidDataException>(() =>
            BridgeProtocolCodec.DecodeResponse(valid, RequestId.New()));
        Assert.ThrowsExactly<InvalidDataException>(() =>
            BridgeProtocolCodec.DecodeResponse(new string('A', 4097), RequestId));
    }

    [TestMethod]
    public void Ct018ResponseRejectsDuplicateFieldsVersionAndInvalidPacketCount()
    {
        var duplicate =
            $"{{\"v\":1,\"v\":1,\"requestId\":\"{RequestId}\",\"status\":\"accepted\",\"code\":\"OK\",\"packetCount\":3,\"serverTime\":\"2026-07-14T12:00:00.0000000Z\"}}";
        Assert.ThrowsExactly<InvalidDataException>(() =>
            BridgeProtocolCodec.DecodeResponse(duplicate, RequestId));
        Assert.ThrowsExactly<InvalidDataException>(() =>
            BridgeProtocolCodec.DecodeResponse(ResponseJson(RequestId, "accepted", "OK", 4), RequestId));
        Assert.ThrowsExactly<InvalidDataException>(() =>
            BridgeProtocolCodec.DecodeResponse(ResponseJson(RequestId, "accepted", "OK", 3).Replace("\"v\":1", "\"v\":2"), RequestId));
    }

    [TestMethod]
    public void Ct018RequestRejectsNonCanonicalOrOversizedBase64UrlBeforeJsonParsing()
    {
        Assert.ThrowsExactly<InvalidDataException>(() =>
            BridgeProtocolCodec.DecodeRequest("rwa1:AB"));
        Assert.ThrowsExactly<InvalidDataException>(() =>
            BridgeProtocolCodec.DecodeRequest("rwa1:" + new string('A', 5465)));
    }

    [TestMethod]
    public void Ct018ResponseRequiresOkOnlyForAcceptedStatus()
    {
        Assert.ThrowsExactly<InvalidDataException>(() =>
            BridgeProtocolCodec.DecodeResponse(ResponseJson(RequestId, "accepted", "ERR012", 0), RequestId));
        Assert.ThrowsExactly<InvalidDataException>(() =>
            BridgeProtocolCodec.DecodeResponse(ResponseJson(RequestId, "rejected", "OK", 0), RequestId));
    }

    internal static string ResponseJson(
        RequestId requestId,
        string status,
        string code,
        int packetCount) =>
        $"{{\"v\":1,\"requestId\":\"{requestId}\",\"status\":\"{status}\",\"code\":\"{code}\",\"packetCount\":{packetCount},\"serverTime\":\"2026-07-14T12:00:00.0000000Z\"}}";
}
