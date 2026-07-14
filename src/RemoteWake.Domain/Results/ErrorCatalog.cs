namespace RemoteWake.Domain.Results;

public static class ErrorCatalog
{
    private static readonly Dictionary<ErrorCode, string> Names =
        new Dictionary<ErrorCode, string>
        {
            [ErrorCode.ERR001] = "windows_inventory_failed",
            [ErrorCode.ERR002] = "eligible_ethernet_not_found",
            [ErrorCode.ERR003] = "network_adapter_ambiguous",
            [ErrorCode.ERR004] = "wake_capability_inconclusive",
            [ErrorCode.ERR005] = "persistence_unavailable",
            [ErrorCode.ERR006] = "secret_vault_unavailable",
            [ErrorCode.ERR007] = "privileged_operation_rejected",
            [ErrorCode.ERR008] = "vpn_disconnected",
            [ErrorCode.ERR009] = "bridge_unreachable",
            [ErrorCode.ERR010] = "remote_identity_mismatch",
            [ErrorCode.ERR011] = "request_replayed_or_expired",
            [ErrorCode.ERR012] = "wake_rejected_or_receipt_missing",
            [ErrorCode.ERR013] = "wake_not_confirmed",
            [ErrorCode.ERR014] = "windows_not_ready",
            [ErrorCode.ERR015] = "remote_service_unavailable",
            [ErrorCode.ERR016] = "remote_client_launch_failed",
            [ErrorCode.ERR017] = "integration_not_supported",
            [ErrorCode.ERR018] = "android_bootstrap_failed",
            [ErrorCode.ERR019] = "notification_unavailable",
            [ErrorCode.ERR020] = "unknown_failure",
            [ErrorCode.ERR021] = "readiness_protocol_unavailable_or_incompatible",
        };

    public static string GetName(ErrorCode code) =>
        Names.TryGetValue(code, out var name)
            ? name
            : throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown error code.");
}
