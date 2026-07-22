namespace Zircon.Mobile.Core.Protocol
{
    public static class ZirconPacketIds
    {
        public static class General
        {
            public const ushort Connected = 1;
            public const ushort Ping = 2;
            public const ushort CheckVersion = 3;
            public const ushort Version = 4;
            public const ushort GoodVersion = 5;
            public const ushort PingResponse = 6;
            public const ushort Disconnect = 7;
        }

        public static class Client
        {
            public const ushort SelectLanguage = 1007;
            public const ushort Login = 1008;
            public const ushort Logout = 1009;
            public const ushort StartGame = 1012;
            public const ushort TownRevive = 1013;
            public const ushort Turn = 1014;
            public const ushort Harvest = 1015;
            public const ushort Move = 1016;
            public const ushort Attack = 1018;
            public const ushort Magic = 1020;
            public const ushort ItemMove = 1021;
            public const ushort ItemDrop = 1023;
            public const ushort ItemUse = 1025;
            public const ushort ItemLock = 1026;
            public const ushort PickUp = 1029;
            public const ushort Chat = 1030;
            public const ushort NpcCall = 1031;
            public const ushort NpcButton = 1032;
            public const ushort NpcBuy = 1033;
            public const ushort NpcSell = 1034;
            public const ushort NpcRepair = 1036;
            public const ushort NpcClose = 1041;
            public const ushort MagicKey = 1045;
            public const ushort CheckClientDb = 1109;
            public const ushort UpgradeClient = 1110;
            public const ushort GroupSwitch = 1047;
            public const ushort GroupInvite = 1048;
            public const ushort GroupRemove = 1049;
            public const ushort GroupResponse = 1050;
            public const ushort MarketHistory = 1056;
            public const ushort MarketConsign = 1057;
            public const ushort MarketSearch = 1058;
            public const ushort MarketSearchIndex = 1059;
            public const ushort MarketCancel = 1060;
            public const ushort MarketBuy = 1061;
            public const ushort MailOpened = 1063;
            public const ushort MailGetItem = 1064;
            public const ushort MailDelete = 1065;
            public const ushort MailSend = 1066;
            public const ushort TradeRequest = 1070;
            public const ushort TradeResponse = 1071;
            public const ushort TradeClose = 1072;
            public const ushort TradeAddGold = 1073;
            public const ushort TradeAddItem = 1074;
            public const ushort TradeConfirm = 1075;
            public const ushort GuildCreate = 1076;
            public const ushort GuildEditNotice = 1077;
            public const ushort GuildInviteMember = 1079;
            public const ushort GuildKickMember = 1080;
            public const ushort GuildResponse = 1084;
            public const ushort QuestAccept = 1087;
            public const ushort QuestComplete = 1088;
            public const ushort QuestTrack = 1089;
            public const ushort SortStorageItem = 1120;
            public const ushort LoginSimple = 1111;
        }

        public static class Server
        {
            public const ushort Login = 2003;
            public const ushort StartGame = 2012;
            public const ushort MapChanged = 2013;
            public const ushort UserLocation = 2014;
            public const ushort ObjectRemove = 2015;
            public const ushort ObjectTurn = 2016;
            public const ushort ObjectMove = 2019;
            public const ushort ObjectAttack = 2022;
            public const ushort ObjectMagic = 2024;
            public const ushort ObjectMonster = 2035;
            public const ushort ObjectNpc = 2036;
            public const ushort ObjectItem = 2037;
            public const ushort ObjectSpell = 2038;
            public const ushort MagicToggle = 2042;
            public const ushort NewMagic = 2048;
            public const ushort MagicLeveled = 2049;
            public const ushort MagicCooldown = 2050;
            public const ushort ItemsGained = 2058;
            public const ushort ItemMove = 2059;
            public const ushort ItemLock = 2061;
            public const ushort ItemChanged = 2063;
            public const ushort ItemDurability = 2066;
            public const ushort StatsUpdate = 2051;
            public const ushort GoldChanged = 2067;
            public const ushort Chat = 2069;
            public const ushort NpcResponse = 2070;
            public const ushort ItemsChanged = 2071;
            public const ushort NpcRepair = 2072;
            public const ushort NpcClose = 2076;
            public const ushort GameLogout = 2009;
            public const ushort GameGoldChanged = 2111;
            public const ushort BuffAdd = 2085;
            public const ushort BuffRemove = 2086;
            public const ushort BuffChanged = 2087;
            public const ushort BuffTime = 2088;
            public const ushort BuffPaused = 2089;
            public const ushort CombatTime = 2091;
            public const ushort WeightUpdate = 2114;
            public const ushort HuntGoldChanged = 2115;
            public const ushort GroupSwitch = 2081;
            public const ushort GroupMember = 2082;
            public const ushort GroupRemove = 2083;
            public const ushort GroupInvite = 2084;
            public const ushort MarketHistory = 2096;
            public const ushort MarketConsign = 2097;
            public const ushort MarketSearch = 2098;
            public const ushort MarketSearchCount = 2099;
            public const ushort MarketSearchIndex = 2100;
            public const ushort MarketBuy = 2101;
            public const ushort MarketConsignChanged = 2103;
            public const ushort MailList = 2104;
            public const ushort MailNew = 2105;
            public const ushort MailDelete = 2106;
            public const ushort MailItemDelete = 2107;
            public const ushort MailSend = 2108;
            public const ushort TradeRequest = 2116;
            public const ushort TradeOpen = 2117;
            public const ushort TradeClose = 2118;
            public const ushort TradeAddItem = 2119;
            public const ushort TradeAddGold = 2120;
            public const ushort TradeItemAdded = 2121;
            public const ushort TradeGoldAdded = 2122;
            public const ushort TradeUnlock = 2123;
            public const ushort GuildInvite = 2135;
            public const ushort QuestChanged = 2151;
            public const ushort DataObjectPlayer = 2166;
            public const ushort DataObjectMonster = 2167;
            public const ushort DataObjectItem = 2168;
            public const ushort DataObjectLocation = 2169;
            public const ushort DataObjectMaxHealthMana = 2171;
            public const ushort StorageSize = 2175;
            public const ushort CheckClientDb = 2179;
            public const ushort CheckClientHash = 2180;
            public const ushort UpgradeClient = 2181;
            public const ushort LoginSimple = 2182;
            public const ushort AccountExpand = 2183;
            public const ushort AutoTimeChanged = 2184;
            public const ushort SortStorageItem = 2187;
            public const ushort SkillConfig = 2189;
        }
    }
}
