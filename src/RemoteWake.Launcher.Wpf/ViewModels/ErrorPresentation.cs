using RemoteWake.Domain.Results;
using RemoteWake.Launcher.Wpf.Services;

namespace RemoteWake.Launcher.Wpf.ViewModels;

public sealed record ErrorPresentation(string Title, string Consequence, string Action);

public sealed class ErrorPresentationMapper
{
    private readonly ILauncherTextProvider texts;

    public ErrorPresentationMapper(ILauncherTextProvider texts)
    {
        this.texts = texts ?? throw new ArgumentNullException(nameof(texts));
    }

    public ErrorPresentation Map(ErrorCode code)
    {
        var titleKey = code switch
        {
            ErrorCode.ERR008 => "ErrorVpnTitle",
            ErrorCode.ERR009 => "ErrorBridgeTitle",
            ErrorCode.ERR010 => "ErrorIdentityTitle",
            ErrorCode.ERR011 => "ErrorRateTitle",
            ErrorCode.ERR012 => "ErrorWakeRequestTitle",
            ErrorCode.ERR013 => "ErrorWakeTitle",
            ErrorCode.ERR014 => "ErrorWindowsTitle",
            ErrorCode.ERR015 => "ErrorServiceTitle",
            ErrorCode.ERR016 => "ErrorClientTitle",
            ErrorCode.ERR021 => "ErrorReadinessTitle",
            _ => "ErrorUnknownTitle",
        };
        var actionKey = code switch
        {
            ErrorCode.ERR008 => "ErrorVpnAction",
            ErrorCode.ERR009 => "ErrorBridgeAction",
            ErrorCode.ERR010 => "ErrorIdentityAction",
            ErrorCode.ERR011 => "ErrorRateAction",
            ErrorCode.ERR012 => "ErrorWakeRequestAction",
            ErrorCode.ERR013 => "ErrorWakeAction",
            ErrorCode.ERR014 => "ErrorWindowsAction",
            ErrorCode.ERR015 => "ErrorServiceAction",
            ErrorCode.ERR016 => "ErrorClientAction",
            ErrorCode.ERR021 => "ErrorReadinessAction",
            _ => "ErrorUnknownAction",
        };

        return new ErrorPresentation(
            texts.GetText(titleKey),
            texts.GetText("ErrorConsequence"),
            texts.GetText(actionKey));
    }
}
