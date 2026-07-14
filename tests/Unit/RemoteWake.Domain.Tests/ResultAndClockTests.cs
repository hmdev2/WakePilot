using RemoteWake.Domain.Results;
using RemoteWake.Domain.Time;

namespace RemoteWake.Domain.Tests;

[TestClass]
public sealed class ResultAndClockTests
{
    [TestMethod]
    public void ErrorCatalogCoversEveryPublicErrorCode()
    {
        var codes = Enum.GetValues<ErrorCode>();
        var names = codes.Select(ErrorCatalog.GetName).ToArray();

        Assert.HasCount(21, codes);
        Assert.HasCount(21, names.Distinct().ToArray());
        Assert.IsTrue(names.All(name => !string.IsNullOrWhiteSpace(name)));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ErrorCatalog.GetName((ErrorCode)999));
    }

    [TestMethod]
    public void ResultsKeepFailureSeparateFromValues()
    {
        var error = DomainError.Create(ErrorCode.ERR020, "test_failure", true);
        var success = Result.Success();
        var failure = Result.Failure(error);
        var valueSuccess = Result.Success("value");
        var valueFailure = Result.Failure<string>(error);

        Assert.IsTrue(success.IsSuccess);
        Assert.IsFalse(success.IsFailure);
        Assert.IsNull(success.Error);
        Assert.IsTrue(failure.IsFailure);
        Assert.AreSame(error, failure.Error);
        Assert.AreEqual("value", valueSuccess.Value);
        Assert.IsTrue(valueFailure.IsFailure);
        Assert.AreSame(error, valueFailure.Error);
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = valueFailure.Value);
    }

    [TestMethod]
    public void ResultsRejectNullOrInvalidConstructionData()
    {
        Assert.ThrowsExactly<ArgumentException>(() => DomainError.Create(ErrorCode.ERR020, ""));
        Assert.ThrowsExactly<ArgumentNullException>(() => Result.Failure(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => Result.Success<string>(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => Result.Failure<string>(null!));
    }

    [TestMethod]
    public async Task SystemClockUsesUtcAndHonorsCancellation()
    {
        var before = DateTimeOffset.UtcNow;
        var observed = SystemClock.Instance.UtcNow;
        var after = DateTimeOffset.UtcNow;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        Assert.IsTrue(observed >= before && observed <= after);
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            async () => await SystemClock.Instance.DelayAsync(TimeSpan.FromSeconds(1), cancellation.Token));
    }
}
