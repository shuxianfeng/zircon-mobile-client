# Generated Packet Schema Summary

Source root: `E:\codex ide\zircon-legend-client`

| Kind | ID | Name | Properties |
| --- | ---: | --- | --- |
| Client | 1001 | `NewAccount` | string EMailAddress<br>string Password<br>DateTime BirthDate<br>string RealName<br>string Referral<br>string CheckSum |
| Client | 1002 | `ChangePassword` | string EMailAddress<br>string CurrentPassword<br>string NewPassword<br>string CheckSum |
| Client | 1003 | `RequestPasswordReset` | string EMailAddress<br>string CheckSum |
| Client | 1004 | `ResetPassword` | string ResetKey<br>string NewPassword<br>string CheckSum |
| Client | 1005 | `Activation` | string ActivationKey<br>string CheckSum |
| Client | 1006 | `RequestActivationKey` | string EMailAddress<br>string CheckSum |
| Client | 1007 | `SelectLanguage` | string Language |
| Client | 1008 | `Login` | string EMailAddress<br>string Password<br>string CheckSum |
| Client | 1009 | `Logout` |  |
| Client | 1010 | `NewCharacter` | string CharacterName<br>MirClass Class<br>MirGender Gender<br>int HairType<br>Color HairColour<br>Color ArmourColour<br>string CheckSum |
| Client | 1011 | `DeleteCharacter` | int CharacterIndex<br>string CheckSum |
| Client | 1012 | `StartGame` | int CharacterIndex |
| Client | 1013 | `TownRevive` |  |
| Client | 1014 | `Turn` | MirDirection Direction |
| Client | 1015 | `Harvest` | MirDirection Direction |
| Client | 1016 | `Move` | MirDirection Direction<br>int Distance |
| Client | 1017 | `Mount` |  |
| Client | 1018 | `Attack` | MirDirection Direction<br>MirAction Action<br>MagicType AttackMagic |
| Client | 1019 | `Mining` | MirDirection Direction |
| Client | 1020 | `Magic` | MirDirection Direction<br>MirAction Action<br>MagicType Type<br>uint Target<br>Point Location |
| Client | 1021 | `ItemMove` | GridType FromGrid<br>GridType ToGrid<br>int FromSlot<br>int ToSlot<br>bool MergeItem |
| Client | 1022 | `ItemSplit` | GridType Grid<br>int Slot<br>long Count |
| Client | 1023 | `ItemDrop` | CellLinkInfo Link |
| Client | 1024 | `GoldDrop` | long Amount |
| Client | 1025 | `ItemUse` | CellLinkInfo Link |
| Client | 1026 | `ItemLock` | GridType GridType<br>int SlotIndex<br>bool Locked |
| Client | 1027 | `BeltLinkChanged` | int Slot<br>int LinkIndex<br>int LinkItemIndex |
| Client | 1028 | `AutoPotionLinkChanged` | int Slot<br>int LinkIndex<br>int Health<br>int Mana<br>bool Enabled |
| Client | 1029 | `PickUp` | byte PickType |
| Client | 1030 | `Chat` | string Text |
| Client | 1031 | `NPCCall` | uint ObjectID |
| Client | 1032 | `NPCButton` | int ButtonID |
| Client | 1033 | `NPCBuy` | int Index<br>long Amount<br>bool GuildFunds |
| Client | 1034 | `NPCSell` | List<CellLinkInfo> Links |
| Client | 1035 | `NPCFragment` | List<CellLinkInfo> Links |
| Client | 1036 | `NPCRepair` | List<CellLinkInfo> Links<br>bool Special<br>bool GuildFunds |
| Client | 1037 | `NPCRefine` | RefineType RefineType<br>RefineQuality RefineQuality<br>List<CellLinkInfo> Ores<br>List<CellLinkInfo> Items<br>List<CellLinkInfo> Specials |
| Client | 1038 | `NPCMasterRefine` | RefineType RefineType<br>List<CellLinkInfo> Fragment1s<br>List<CellLinkInfo> Fragment2s<br>List<CellLinkInfo> Fragment3s<br>List<CellLinkInfo> Stones<br>List<CellLinkInfo> Specials |
| Client | 1039 | `NPCMasterRefineEvaluate` | RefineType RefineType<br>List<CellLinkInfo> Fragment1s<br>List<CellLinkInfo> Fragment2s<br>List<CellLinkInfo> Fragment3s<br>List<CellLinkInfo> Stones<br>List<CellLinkInfo> Specials |
| Client | 1040 | `NPCRefinementStone` | List<CellLinkInfo> IronOres<br>List<CellLinkInfo> SilverOres<br>List<CellLinkInfo> DiamondOres<br>List<CellLinkInfo> GoldOres<br>List<CellLinkInfo> Crystal<br>long Gold |
| Client | 1041 | `NPCClose` |  |
| Client | 1042 | `NPCRefineRetrieve` | int Index |
| Client | 1043 | `NPCAccessoryLevelUp` | CellLinkInfo Target<br>List<CellLinkInfo> Links |
| Client | 1044 | `NPCAccessoryUpgrade` | CellLinkInfo Target<br>RefineType RefineType |
| Client | 1045 | `MagicKey` | MagicType Magic<br>SpellKey Set1Key<br>SpellKey Set2Key<br>SpellKey Set3Key<br>SpellKey Set4Key |
| Client | 1046 | `MagicToggle` | MagicType Magic<br>bool CanUse |
| Client | 1047 | `GroupSwitch` | bool Allow |
| Client | 1048 | `GroupInvite` | string Name |
| Client | 1049 | `GroupRemove` | string Name |
| Client | 1050 | `GroupResponse` | bool Accept |
| Client | 1051 | `Inspect` | int Index |
| Client | 1052 | `RankRequest` | RequiredClass Class<br>bool OnlineOnly<br>int StartIndex |
| Client | 1053 | `ObserverRequest` | string Name |
| Client | 1054 | `ObservableSwitch` | bool Allow |
| Client | 1055 | `Hermit` | Stat Stat |
| Client | 1056 | `MarketPlaceHistory` | int Index<br>int Display<br>int PartIndex |
| Client | 1057 | `MarketPlaceConsign` | CellLinkInfo Link<br>int Price<br>string Message<br>bool GuildFunds |
| Client | 1058 | `MarketPlaceSearch` | string Name<br>bool ItemTypeFilter<br>ItemType ItemType<br>MarketPlaceSort Sort |
| Client | 1059 | `MarketPlaceSearchIndex` | int Index |
| Client | 1060 | `MarketPlaceCancelConsign` | int Index<br>long Count |
| Client | 1061 | `MarketPlaceBuy` | long Index<br>long Count<br>bool GuildFunds |
| Client | 1062 | `MarketPlaceStoreBuy` | int Index<br>long Count<br>bool UseHuntGold |
| Client | 1063 | `MailOpened` | int Index |
| Client | 1064 | `MailGetItem` | int Index<br>int Slot |
| Client | 1065 | `MailDelete` | int Index |
| Client | 1066 | `MailSend` | List<CellLinkInfo> Links<br>string Recipient<br>string Subject<br>string Message<br>long Gold |
| Client | 1067 | `ChangeAttackMode` | AttackMode Mode |
| Client | 1068 | `ChangePetMode` | PetMode Mode |
| Client | 1069 | `GameGoldRecharge` |  |
| Client | 1070 | `TradeRequest` |  |
| Client | 1071 | `TradeRequestResponse` | bool Accept |
| Client | 1072 | `TradeClose` |  |
| Client | 1073 | `TradeAddGold` | long Gold |
| Client | 1074 | `TradeAddItem` | CellLinkInfo Cell |
| Client | 1075 | `TradeConfirm` |  |
| Client | 1076 | `GuildCreate` | string Name<br>bool UseGold<br>int Members<br>int Storage |
| Client | 1077 | `GuildEditNotice` | string Notice |
| Client | 1078 | `GuildEditMember` | int Index<br>string Rank<br>GuildPermission Permission |
| Client | 1079 | `GuildInviteMember` | string Name |
| Client | 1080 | `GuildKickMember` | int Index |
| Client | 1081 | `GuildTax` | long Tax |
| Client | 1082 | `GuildIncreaseMember` |  |
| Client | 1083 | `GuildIncreaseStorage` |  |
| Client | 1084 | `GuildResponse` | bool Accept |
| Client | 1085 | `GuildWar` | string GuildName |
| Client | 1086 | `GuildRequestConquest` | int Index |
| Client | 1087 | `QuestAccept` | int Index |
| Client | 1088 | `QuestComplete` | int Index<br>int ChoiceIndex |
| Client | 1089 | `QuestTrack` | int Index<br>bool Track |
| Client | 1090 | `CompanionUnlock` | int Index |
| Client | 1091 | `CompanionAdopt` | int Index<br>string Name |
| Client | 1092 | `CompanionRetrieve` | int Index |
| Client | 1093 | `CompanionStore` | int Index |
| Client | 1094 | `MarriageResponse` | bool Accept |
| Client | 1095 | `MarriageMakeRing` | int Slot |
| Client | 1096 | `MarriageTeleport` |  |
| Client | 1097 | `BlockAdd` | string Name |
| Client | 1098 | `BlockRemove` | int Index |
| Client | 1099 | `HelmetToggle` | bool HideHelmet |
| Client | 1100 | `GenderChange` | MirGender Gender<br>int HairType<br>Color HairColour |
| Client | 1101 | `HairChange` | int HairType<br>Color HairColour |
| Client | 1102 | `ArmourDye` | Color ArmourColour |
| Client | 1103 | `NameChange` | string Name |
| Client | 1104 | `FortuneCheck` | int ItemIndex |
| Client | 1105 | `TeleportRing` | Point Location<br>int Index |
| Client | 1106 | `JoinStarterGuild` |  |
| Client | 1107 | `NPCAccessoryReset` | CellLinkInfo Cell |
| Client | 1108 | `NPCWeaponCraft` | RequiredClass Class<br>CellLinkInfo Template<br>CellLinkInfo Yellow<br>CellLinkInfo Blue<br>CellLinkInfo Red<br>CellLinkInfo Purple<br>CellLinkInfo Green<br>CellLinkInfo Grey |
| Client | 1109 | `CheckClientDb` | string Hash |
| Client | 1110 | `UpgradeClient` | string FileKey |
| Client | 1111 | `LoginSimple` | string EMailAddress<br>string Password<br>string CheckSum |
| Client | 1112 | `AccountExpand` |  |
| Client | 1113 | `AutoFightConfChanged` | AutoSetConf Slot<br>MagicType MagicIndex<br>int TimeCount<br>bool Enabled |
| Client | 1114 | `SortBagItem` |  |
| Client | 1115 | `PickUpC` | int ItemIdx<br>int Xpos<br>int Ypos |
| Client | 1116 | `PickUpA` | int ItemIdx<br>int Xpos<br>int Ypos |
| Client | 1117 | `PickUpS` | List<PickItemInfo> UserItems<br>List<PickItemInfo> CompanionItems |
| Client | 1118 | `PktFilterItem` | List<string> FilterStr |
| Client | 1119 | `Qiehuanxunzhaoguaiwumoshi` | bool Moshi01<br>bool Moshi02 |
| Client | 1120 | `SortStorageItem` |  |
| General | 1 | `Connected` |  |
| General | 2 | `Ping` |  |
| General | 3 | `CheckVersion` |  |
| General | 4 | `Version` | byte[] ClientHash |
| General | 5 | `GoodVersion` |  |
| General | 6 | `PingResponse` | int Ping |
| General | 7 | `Disconnect` | DisconnectReason Reason |
| Server | 2001 | `NewAccount` | NewAccountResult Result |
| Server | 2002 | `ChangePassword` | ChangePasswordResult Result<br>string Message<br>TimeSpan Duration |
| Server | 2003 | `Login` | LoginResult Result<br>string Message<br>TimeSpan Duration<br>List<SelectInfo> Characters<br>List<ClientUserItem> Items<br>List<ClientBlockInfo> BlockList<br>string Address<br>bool TestServer |
| Server | 2004 | `RequestPasswordReset` | RequestPasswordResetResult Result<br>string Message<br>TimeSpan Duration |
| Server | 2005 | `ResetPassword` | ResetPasswordResult Result |
| Server | 2006 | `Activation` | ActivationResult Result |
| Server | 2007 | `RequestActivationKey` | RequestActivationKeyResult Result<br>TimeSpan Duration |
| Server | 2008 | `SelectLogout` |  |
| Server | 2009 | `GameLogout` | List<SelectInfo> Characters |
| Server | 2010 | `NewCharacter` | NewCharacterResult Result<br>SelectInfo Character |
| Server | 2011 | `DeleteCharacter` | DeleteCharacterResult Result<br>int DeletedIndex |
| Server | 2012 | `StartGame` | StartGameResult Result<br>string Message<br>TimeSpan Duration<br>StartInformation StartInformation |
| Server | 2013 | `MapChanged` | int MapIndex |
| Server | 2014 | `UserLocation` | MirDirection Direction<br>Point Location |
| Server | 2015 | `ObjectRemove` | uint ObjectID |
| Server | 2016 | `ObjectTurn` | uint ObjectID<br>MirDirection Direction<br>Point Location<br>TimeSpan Slow |
| Server | 2017 | `ObjectHarvest` | uint ObjectID<br>MirDirection Direction<br>Point Location<br>TimeSpan Slow |
| Server | 2018 | `ObjectMount` | uint ObjectID<br>HorseType Horse |
| Server | 2019 | `ObjectMove` | uint ObjectID<br>MirDirection Direction<br>Point Location<br>int Distance<br>TimeSpan Slow |
| Server | 2020 | `ObjectDash` | uint ObjectID<br>MirDirection Direction<br>Point Location<br>int Distance<br>MagicType Magic |
| Server | 2021 | `ObjectPushed` | uint ObjectID<br>MirDirection Direction<br>Point Location |
| Server | 2022 | `ObjectAttack` | uint ObjectID<br>MirDirection Direction<br>Point Location<br>MagicType AttackMagic<br>Element AttackElement<br>uint TargetID<br>TimeSpan Slow |
| Server | 2023 | `ObjectRangeAttack` | uint ObjectID<br>MirDirection Direction<br>Point Location<br>MagicType AttackMagic<br>Element AttackElement<br>List<uint> Targets |
| Server | 2024 | `ObjectMagic` | uint ObjectID<br>MirDirection Direction<br>Point CurrentLocation<br>MagicType Type<br>List<uint> Targets<br>List<Point> Locations<br>bool Cast<br>TimeSpan Slow |
| Server | 2025 | `ObjectMining` | uint ObjectID<br>MirDirection Direction<br>Point Location<br>TimeSpan Slow<br>bool Effect |
| Server | 2026 | `ObjectPetOwnerChanged` | uint ObjectID<br>string PetOwner |
| Server | 2027 | `ObjectShow` | uint ObjectID<br>MirDirection Direction<br>Point Location |
| Server | 2028 | `ObjectHide` | uint ObjectID<br>MirDirection Direction<br>Point Location |
| Server | 2029 | `ObjectEffect` | uint ObjectID<br>Effect Effect |
| Server | 2030 | `MapEffect` | Point Location<br>Effect Effect<br>MirDirection Direction |
| Server | 2031 | `ObjectBuffAdd` | uint ObjectID<br>BuffType Type |
| Server | 2032 | `ObjectBuffRemove` | uint ObjectID<br>BuffType Type |
| Server | 2033 | `ObjectPoison` | uint ObjectID<br>PoisonType Poison |
| Server | 2034 | `ObjectPlayer` | int Index<br>uint ObjectID<br>string Name<br>Color NameColour<br>string GuildName<br>MirDirection Direction<br>Point Location<br>MirClass Class<br>MirGender Gender<br>int HairType<br>Color HairColour<br>int Weapon<br>int Shield<br>int Armour<br>Color ArmourColour<br>int ArmourImage<br>int Light<br>bool Dead<br>PoisonType Poison<br>List<BuffType> Buffs<br>HorseType Horse<br>int Helmet<br>int HorseShape |
| Server | 2035 | `ObjectMonster` | uint ObjectID<br>int MonsterIndex<br>Color NameColour<br>string PetOwner<br>MirDirection Direction<br>Point Location<br>bool Dead<br>bool Skeleton<br>PoisonType Poison<br>bool EasterEvent<br>bool HalloweenEvent<br>bool ChristmasEvent<br>List<BuffType> Buffs<br>bool Extra<br>ClientCompanionObject CompanionObject<br>bool Extra<br>int ExtraInt |
| Server | 2036 | `ObjectNPC` | uint ObjectID<br>int NPCIndex<br>Point CurrentLocation<br>MirDirection Direction |
| Server | 2037 | `ObjectItem` | uint ObjectID<br>ClientUserItem Item<br>Point Location |
| Server | 2038 | `ObjectSpell` | uint ObjectID<br>MirDirection Direction<br>Point Location<br>SpellEffect Effect<br>int Power |
| Server | 2039 | `ObjectSpellChanged` | uint ObjectID<br>int Power |
| Server | 2040 | `ObjectNameColour` | uint ObjectID<br>Color Colour |
| Server | 2041 | `PlayerUpdate` | uint ObjectID<br>int Weapon<br>int Shield<br>int Armour<br>Color ArmourColour<br>int ArmourImage<br>int HorseArmour<br>int Helmet<br>int Light |
| Server | 2042 | `MagicToggle` | MagicType Magic<br>bool CanUse |
| Server | 2043 | `DayChanged` | float DayTime |
| Server | 2044 | `LevelChanged` | int Level<br>decimal Experience |
| Server | 2045 | `ObjectLeveled` | uint ObjectID |
| Server | 2046 | `ObjectRevive` | uint ObjectID<br>Point Location<br>bool Effect |
| Server | 2047 | `GainedExperience` | decimal Amount |
| Server | 2048 | `NewMagic` | ClientUserMagic Magic |
| Server | 2049 | `MagicLeveled` | int InfoIndex<br>int Level<br>long Experience |
| Server | 2050 | `MagicCooldown` | int InfoIndex<br>int Delay |
| Server | 2051 | `StatsUpdate` | Stats Stats<br>Stats HermitStats<br>int HermitPoints |
| Server | 2052 | `HealthChanged` | uint ObjectID<br>int Change<br>bool Miss<br>bool Block<br>bool Critical |
| Server | 2053 | `ObjectStats` | uint ObjectID<br>Stats Stats |
| Server | 2054 | `ManaChanged` | uint ObjectID<br>int Change |
| Server | 2055 | `ObjectStruck` | uint ObjectID<br>MirDirection Direction<br>Point Location<br>uint AttackerID<br>Element Element |
| Server | 2056 | `ObjectDied` | uint ObjectID<br>MirDirection Direction<br>Point Location |
| Server | 2057 | `ObjectHarvested` | uint ObjectID<br>MirDirection Direction<br>Point Location |
| Server | 2058 | `ItemsGained` | List<ClientUserItem> Items |
| Server | 2059 | `ItemMove` | GridType FromGrid<br>GridType ToGrid<br>int FromSlot<br>int ToSlot<br>bool MergeItem<br>bool Success |
| Server | 2060 | `ItemSplit` | GridType Grid<br>int Slot<br>long Count<br>int NewSlot<br>bool Success |
| Server | 2061 | `ItemLock` | GridType Grid<br>int Slot<br>bool Locked |
| Server | 2062 | `ItemUseDelay` | TimeSpan Delay |
| Server | 2063 | `ItemChanged` | CellLinkInfo Link<br>bool Success |
| Server | 2064 | `ItemStatsChanged` | GridType GridType<br>int Slot<br>Stats NewStats |
| Server | 2065 | `ItemStatsRefreshed` | GridType GridType<br>int Slot<br>Stats NewStats |
| Server | 2066 | `ItemDurability` | GridType GridType<br>int Slot<br>int CurrentDurability |
| Server | 2067 | `GoldChanged` | long Gold |
| Server | 2068 | `ItemExperience` | CellLinkInfo Target<br>decimal Experience<br>int Level<br>UserItemFlags Flags |
| Server | 2069 | `Chat` | uint ObjectID<br>string Text<br>MessageType Type<br>List<ClientUserItem> Items |
| Server | 2070 | `NPCResponse` | uint ObjectID<br>int Index<br>List<ClientRefineInfo> Extra |
| Server | 2071 | `ItemsChanged` | List<CellLinkInfo> Links<br>bool Success |
| Server | 2072 | `NPCRepair` | List<CellLinkInfo> Links<br>bool Special<br>bool Success<br>TimeSpan SpecialRepairDelay |
| Server | 2073 | `NPCRefinementStone` | List<CellLinkInfo> IronOres<br>List<CellLinkInfo> SilverOres<br>List<CellLinkInfo> DiamondOres<br>List<CellLinkInfo> GoldOres<br>List<CellLinkInfo> Crystal |
| Server | 2074 | `NPCRefine` | RefineType RefineType<br>RefineQuality RefineQuality<br>List<CellLinkInfo> Ores<br>List<CellLinkInfo> Items<br>List<CellLinkInfo> Specials<br>bool Success |
| Server | 2075 | `NPCMasterRefine` | List<CellLinkInfo> Fragment1s<br>List<CellLinkInfo> Fragment2s<br>List<CellLinkInfo> Fragment3s<br>List<CellLinkInfo> Stones<br>List<CellLinkInfo> Specials<br>bool Success |
| Server | 2076 | `NPCClose` |  |
| Server | 2077 | `NPCAccessoryLevelUp` | CellLinkInfo Target<br>List<CellLinkInfo> Links |
| Server | 2078 | `NPCAccessoryUpgrade` | CellLinkInfo Target<br>RefineType RefineType<br>bool Success |
| Server | 2079 | `NPCRefineRetrieve` | int Index |
| Server | 2080 | `RefineList` | List<ClientRefineInfo> List |
| Server | 2081 | `GroupSwitch` | bool Allow |
| Server | 2082 | `GroupMember` | uint ObjectID<br>string Name |
| Server | 2083 | `GroupRemove` | uint ObjectID |
| Server | 2084 | `GroupInvite` | string Name |
| Server | 2085 | `BuffAdd` | ClientBuffInfo Buff |
| Server | 2086 | `BuffRemove` | int Index |
| Server | 2087 | `BuffChanged` | int Index<br>Stats Stats |
| Server | 2088 | `BuffTime` | int Index<br>TimeSpan Time |
| Server | 2089 | `BuffPaused` | int Index<br>bool Paused |
| Server | 2090 | `SafeZoneChanged` | bool InSafeZone |
| Server | 2091 | `CombatTime` |  |
| Server | 2092 | `Inspect` | string Name<br>string GuildName<br>string GuildRank<br>string Partner<br>MirClass Class<br>int Level<br>MirGender Gender<br>Stats Stats<br>Stats HermitStats<br>int HermitPoints<br>List<ClientUserItem> Items<br>int Hair<br>Color HairColour<br>int WearWeight<br>int HandWeight |
| Server | 2093 | `Rankings` | bool OnlineOnly<br>RequiredClass Class<br>int StartIndex<br>int Total<br>List<RankInfo> Ranks |
| Server | 2094 | `StartObserver` | StartInformation StartInformation<br>List<ClientUserItem> Items |
| Server | 2095 | `ObservableSwitch` | bool Allow |
| Server | 2096 | `MarketPlaceHistory` | int Index<br>long SaleCount<br>long LastPrice<br>long AveragePrice<br>int Display |
| Server | 2097 | `MarketPlaceConsign` | List<ClientMarketPlaceInfo> Consignments |
| Server | 2098 | `MarketPlaceSearch` | int Count<br>List<ClientMarketPlaceInfo> Results |
| Server | 2099 | `MarketPlaceSearchCount` | int Count |
| Server | 2100 | `MarketPlaceSearchIndex` | int Index<br>ClientMarketPlaceInfo Result |
| Server | 2101 | `MarketPlaceBuy` | int Index<br>long Count<br>bool Success |
| Server | 2102 | `MarketPlaceStoreBuy` |  |
| Server | 2103 | `MarketPlaceConsignChanged` | int Index<br>long Count |
| Server | 2104 | `MailList` | List<ClientMailInfo> Mail |
| Server | 2105 | `MailNew` | ClientMailInfo Mail |
| Server | 2106 | `MailDelete` | int Index |
| Server | 2107 | `MailItemDelete` | int Index<br>int Slot |
| Server | 2108 | `MailSend` |  |
| Server | 2109 | `ChangeAttackMode` | AttackMode Mode |
| Server | 2110 | `ChangePetMode` | PetMode Mode |
| Server | 2111 | `GameGoldChanged` | int GameGold |
| Server | 2112 | `MountFailed` | HorseType Horse |
| Server | 2114 | `WeightUpdate` | int BagWeight<br>int WearWeight<br>int HandWeight |
| Server | 2115 | `HuntGoldChanged` | int HuntGold |
| Server | 2116 | `TradeRequest` | string Name |
| Server | 2117 | `TradeOpen` | string Name |
| Server | 2118 | `TradeClose` |  |
| Server | 2119 | `TradeAddItem` | CellLinkInfo Cell<br>bool Success |
| Server | 2120 | `TradeAddGold` | long Gold |
| Server | 2121 | `TradeItemAdded` | ClientUserItem Item |
| Server | 2122 | `TradeGoldAdded` | long Gold |
| Server | 2123 | `TradeUnlock` |  |
| Server | 2124 | `GuildCreate` |  |
| Server | 2125 | `GuildInfo` | ClientGuildInfo Guild |
| Server | 2126 | `GuildNoticeChanged` | string Notice |
| Server | 2127 | `GuildNewItem` | int Slot<br>ClientUserItem Item<br>int Count |
| Server | 2128 | `GuildGetItem` | GridType Grid<br>int Slot<br>ClientUserItem Item |
| Server | 2129 | `GuildUpdate` | int MemberLimit<br>int StorageLimit<br>long GuildFunds<br>long DailyGrowth<br>int GuildLevel<br>int Tax<br>long TotalContribution<br>long DailyContribution<br>string DefaultRank<br>GuildPermission DefaultPermission<br>List<ClientGuildMemberInfo> Members |
| Server | 2130 | `GuildKick` | int Index |
| Server | 2131 | `GuildTax` |  |
| Server | 2132 | `GuildIncreaseMember` |  |
| Server | 2133 | `GuildIncreaseStorage` |  |
| Server | 2134 | `GuildInviteMember` |  |
| Server | 2135 | `GuildInvite` | string Name<br>string GuildName |
| Server | 2136 | `GuildStats` | int Index<br>Stats Stats |
| Server | 2137 | `GuildMemberOffline` | int Index |
| Server | 2138 | `GuildMemberOnline` | int Index<br>string Name<br>uint ObjectID |
| Server | 2139 | `GuildMemberContribution` | int Index<br>long Contribution |
| Server | 2140 | `GuildDayReset` |  |
| Server | 2141 | `GuildFundsChanged` | long Change |
| Server | 2142 | `GuildChanged` | uint ObjectID<br>string GuildName<br>string GuildRank |
| Server | 2143 | `GuildWarFinished` | string GuildName |
| Server | 2144 | `GuildWar` | bool Success |
| Server | 2145 | `GuildWarStarted` | string GuildName<br>TimeSpan Duration |
| Server | 2146 | `GuildConquestDate` | int Index<br>TimeSpan WarTime |
| Server | 2147 | `GuildCastleInfo` | int Index<br>string Owner |
| Server | 2148 | `GuildConquestStarted` | int Index |
| Server | 2149 | `GuildConquestFinished` | int Index |
| Server | 2150 | `ReviveTimers` | TimeSpan ItemReviveTime<br>TimeSpan ReincarnationPillTime |
| Server | 2151 | `QuestChanged` | ClientUserQuest Quest |
| Server | 2152 | `CompanionUnlock` | int Index |
| Server | 2153 | `CompanionAdopt` | ClientUserCompanion UserCompanion |
| Server | 2154 | `CompanionRetrieve` | int Index |
| Server | 2155 | `CompanionStore` |  |
| Server | 2156 | `CompanionWeightUpdate` | int BagWeight<br>int MaxBagWeight<br>int InventorySize |
| Server | 2157 | `CompanionItemsGained` | List<ClientUserItem> Items |
| Server | 2158 | `CompanionUpdate` | int Level<br>int Experience<br>int Hunger |
| Server | 2159 | `CompanionSkillUpdate` | Stats Level3<br>Stats Level5<br>Stats Level7<br>Stats Level10<br>Stats Level11<br>Stats Level13<br>Stats Level15 |
| Server | 2160 | `MarriageInvite` | string Name |
| Server | 2161 | `MarriageInfo` | ClientPlayerInfo Partner |
| Server | 2162 | `MarriageRemoveRing` |  |
| Server | 2163 | `MarriageMakeRing` |  |
| Server | 2164 | `MarriageOnlineChanged` | uint ObjectID |
| Server | 2165 | `DataObjectRemove` | uint ObjectID |
| Server | 2166 | `DataObjectPlayer` | uint ObjectID<br>int MapIndex<br>Point CurrentLocation<br>string Name<br>int Health<br>int Mana<br>bool Dead<br>int MaxHealth<br>int MaxMana |
| Server | 2167 | `DataObjectMonster` | uint ObjectID<br>int MapIndex<br>Point CurrentLocation<br>int MonsterIndex<br>string PetOwner<br>int Health<br>Stats Stats<br>bool Dead |
| Server | 2168 | `DataObjectItem` | uint ObjectID<br>int MapIndex<br>Point CurrentLocation<br>int ItemIndex |
| Server | 2169 | `DataObjectLocation` | uint ObjectID<br>int MapIndex<br>Point CurrentLocation |
| Server | 2170 | `DataObjectHealthMana` | uint ObjectID<br>int Health<br>int Mana<br>bool Dead |
| Server | 2171 | `DataObjectMaxHealthMana` | uint ObjectID<br>int MaxHealth<br>int MaxMana<br>Stats Stats |
| Server | 2172 | `BlockAdd` | ClientBlockInfo Info |
| Server | 2173 | `BlockRemove` | int Index |
| Server | 2174 | `HelmetToggle` | bool HideHelmet |
| Server | 2175 | `StorageSize` | int Size |
| Server | 2176 | `PlayerChangeUpdate` | uint ObjectID<br>string Name<br>MirGender Gender<br>int HairType<br>Color HairColour<br>Color ArmourColour |
| Server | 2177 | `FortuneUpdate` | List<ClientFortuneInfo> Fortunes |
| Server | 2178 | `NPCWeaponCraft` | CellLinkInfo Template<br>CellLinkInfo Yellow<br>CellLinkInfo Blue<br>CellLinkInfo Red<br>CellLinkInfo Purple<br>CellLinkInfo Green<br>CellLinkInfo Grey<br>bool Success |
| Server | 2179 | `CheckClientDb` | bool IsUpgrading<br>int CurrentIndex<br>int TotalCount<br>byte[] Datas |
| Server | 2180 | `CheckClientHash` | List<ClientUpgradeItem> ClientFileHash |
| Server | 2181 | `UpgradeClient` | string FileKey<br>int TotalSize<br>int StartIndex<br>byte[] Datas |
| Server | 2182 | `LoginSimple` | LoginResult Result<br>string Message<br>TimeSpan Duration<br>List<SelectInfo> Characters<br>string Address<br>bool TestServer |
| Server | 2183 | `AccountExpand` | List<ClientUserItem> Items<br>List<ClientBlockInfo> BlockList |
| Server | 2184 | `AutoTimeChanged` | long AutoTime |
| Server | 2185 | `SortBagItem` | List<ClientUserItem> Items |
| Server | 2186 | `Qiehuanxunzhaoguaiwumoshi` | bool Moshi01<br>bool Moshi02 |
| Server | 2187 | `SortStorageItem` | List<ClientUserItem> Items |
| Server | 2188 | `WeaponRefineBase` | int LevelLimit<br>int RarityStep |
| Server | 2189 | `SkillConfig` | int SkillLevelLimit |

