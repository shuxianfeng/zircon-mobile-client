namespace Zircon.Mobile.Core.Network
{
    public enum ZirconConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        VersionChecking,
        ReadyForLogin,
        LoggingIn,
        SelectingCharacter,
        LoadingMap,
        InGame,
        Reconnecting
    }
}
