namespace Zircon.Mobile.Core.Protocol
{
    public enum ZirconMirAction : byte
    {
        Standing = 0,
        Moving = 1,
        Pushed = 2,
        Attack = 3,
        RangeAttack = 4,
        Spell = 5,
        Harvest = 6,
        Die = 7,
        Dead = 8,
        Show = 9,
        Hide = 10,
        Mount = 11,
        Mining = 12,
    }

    public enum ZirconLoginResult : byte
    {
        Disabled = 0,
        BadEMail = 1,
        BadPassword = 2,
        AccountNotExists = 3,
        AccountNotActivated = 4,
        WrongPassword = 5,
        Banned = 6,
        AlreadyLoggedIn = 7,
        AlreadyLoggedInPassword = 8,
        AlreadyLoggedInAdmin = 9,
        Success = 10
    }

    public enum ZirconStartGameResult : byte
    {
        Disabled = 0,
        Deleted = 1,
        Delayed = 2,
        UnableToSpawn = 3,
        NotFound = 4,
        Success = 5
    }

    public enum ZirconDisconnectReason : byte
    {
        Unknown = 0,
        TimedOut = 1,
        WrongVersion = 2,
        ServerClosing = 3,
        AnotherUser = 4,
        AnotherUserPassword = 5,
        AnotherUserAdmin = 6,
        Banned = 7,
        Crashed = 8
    }
}
