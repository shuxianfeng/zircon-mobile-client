using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Zircon.Mobile.Core.Models;
using Zircon.Mobile.Core.Network;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.World;

namespace Zircon.Mobile.UI.Login
{
    public sealed class ZirconProtocolProbeBehaviour : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private bool connectOnStart;
        [SerializeField] private bool showDebugOverlay;

        [Header("Server")]
        [SerializeField] private string host = "zircon.35861344.xyz";
        [SerializeField] private int port = 17000;
        [SerializeField] private bool preferIpv6 = true;

        [Header("Login")]
        [SerializeField] private string email;
        [SerializeField] private string password;
        [SerializeField] private int startCharacterIndex = -1;
        [SerializeField] private bool startFirstCharacterAfterLogin;

        [Header("Debug World")]
        [SerializeField] private ZirconWorldDebugRenderer worldRenderer;

        [Header("Actions")]
        [SerializeField] private int actionDirection;
        [SerializeField] private int moveDistance = 1;
        [SerializeField] private int pickType;
        [SerializeField] private string chatText = "hello from mobile probe";

        private readonly StringBuilder logBuilder = new StringBuilder();
        private readonly ConcurrentQueue<string> pendingLogs = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<Action> pendingMainThreadActions = new ConcurrentQueue<Action>();
        private readonly ZirconWorldState worldState = new ZirconWorldState();
        private ZirconNetworkClient client;
        private CancellationTokenSource cts;
        private Vector2 scrollPosition;
        private IReadOnlyList<ZirconCharacterSelectInfo> characters = Array.Empty<ZirconCharacterSelectInfo>();

        public event Action<IReadOnlyList<ZirconCharacterSelectInfo>> CharactersChanged;
        public event Action<ZirconConnectionState> ConnectionStateChanged;
        public IReadOnlyList<ZirconCharacterSelectInfo> Characters => characters;
        public ZirconConnectionState ConnectionState => client?.State ?? ZirconConnectionState.Disconnected;

        private async void Start()
        {
            if (connectOnStart)
                await ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            cts = new CancellationTokenSource();
            var config = new ZirconClientConfig
            {
                Host = host,
                Port = port,
                PreferIpv6 = preferIpv6,
            };

            client = new ZirconNetworkClient(config);
            client.Log += AppendLog;
            client.StateChanged += state =>
            {
                AppendLog($"state changed: {state}");
                pendingMainThreadActions.Enqueue(() => ConnectionStateChanged?.Invoke(state));
            };
            client.PacketReceived += OnPacketReceived;
            worldState.Reset();
            worldRenderer?.Clear();

            try
            {
                await client.ConnectAsync(cts.Token);
            }
            catch (Exception ex)
            {
                AppendLog($"connect failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        private async void OnGUI()
        {
            if (!showDebugOverlay)
                return;
            GUILayout.BeginArea(new Rect(16, 16, Screen.width - 32, Screen.height - 32));

            GUILayout.Label($"Zircon Mobile Protocol Probe - {client?.State.ToString() ?? "NotStarted"}");
            host = GUILayout.TextField(host);
            email = GUILayout.TextField(email);
            password = GUILayout.PasswordField(password, '*');
            startFirstCharacterAfterLogin = GUILayout.Toggle(startFirstCharacterAfterLogin, "Start first character after successful login");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Character", GUILayout.Width(70));
            startCharacterIndex = ParseIntField(startCharacterIndex, GUILayout.TextField(startCharacterIndex.ToString(), GUILayout.Width(48)));
            GUILayout.EndHorizontal();
            DrawWorldSummary();
            DrawActionControls();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Connect", GUILayout.Height(42)))
                await ReconnectAsync();

            if (GUILayout.Button("Login", GUILayout.Height(42)))
                await LoginAsync();


            if (GUILayout.Button("Start Char", GUILayout.Height(42)))
                await StartCharacterAsync();
            if (GUILayout.Button("Disconnect", GUILayout.Height(42)))
                Disconnect();
            GUILayout.EndHorizontal();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.TextArea(logBuilder.ToString(), GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private async Task ReconnectAsync()
        {
            Disconnect();
            await ConnectAsync();
        }

        public async Task ConnectAndLoginAsync(string accountEmail, string plainPassword)
        {
            email = accountEmail ?? string.Empty;
            password = plainPassword ?? string.Empty;

            if (client == null || cts == null || client.State == ZirconConnectionState.Disconnected)
                await ConnectAsync();

            await LoginAsync();
        }

        private async Task LoginAsync()
        {
            if (client == null)
                await ConnectAsync();

            if (client == null)
                return;

            try
            {
                await client.SendLoginAsync(email, password, cts.Token);
            }
            catch (Exception ex)
            {
                AppendLog($"login send failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public Task StartCharacterAsync(int characterIndex)
        {
            startCharacterIndex = characterIndex;
            return StartCharacterAsync();
        }

        public async Task StartCharacterAsync()
        {
            if (client == null || cts == null)
            {
                AppendLog("start game skipped: client is not connected");
                return;
            }

            if (startCharacterIndex < 0)
            {
                AppendLog("start game skipped: character index is negative");
                return;
            }

            try
            {
                await client.SendStartGameAsync(startCharacterIndex, cts.Token);
                AppendLog($"sent start game character={startCharacterIndex}");
            }
            catch (Exception ex)
            {
                AppendLog($"start game send failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        private async void OnPacketReceived(object sender, ZirconPacketEventArgs e)
        {
            if (ZirconServerPacketDecoder.TryDecodeLogin(e.Frame, out ZirconDecodedLogin login))
            {
                IReadOnlyList<ZirconCharacterSelectInfo> loginCharacters = login.Characters;
                characters = loginCharacters;
                pendingMainThreadActions.Enqueue(() => CharactersChanged?.Invoke(loginCharacters));
                AppendLog($"login result={login.Result} characters={login.Characters.Count}");
                foreach (ZirconCharacterSelectInfo character in login.Characters)
                    AppendLog($"character index={character.Index} name={character.Name} level={character.Level}");

                if (login.Result == ZirconLoginResult.Success && startFirstCharacterAfterLogin && login.Characters.Count > 0)
                {
                    int selected = startCharacterIndex >= 0 ? startCharacterIndex : login.Characters[0].Index;
                    await client.SendStartGameAsync(selected, cts.Token);
                }
            }
            else if (ZirconServerPacketDecoder.TryDecodeStartGame(e.Frame, out ZirconDecodedStartGame startGame))
            {
                AppendLog($"start game result={startGame.Result}");
                if (startGame.StartInformation.HasValue)
                {
                    ZirconStartInformation info = startGame.StartInformation.Value;
                    AppendLog($"in game name={info.Name} map={info.MapIndex} location={info.Location.X},{info.Location.Y} hp={info.CurrentHp} mp={info.CurrentMp}");
                }

                if (worldState.ApplyPacket(e.Frame, out string worldSummary))
                    AppendLog(worldSummary);
            }
            else
            {
                if (worldState.ApplyPacket(e.Frame, out string worldSummary))
                    AppendLog(worldSummary);
                else
                    AppendLog(ZirconPacketSummary.Describe(e.Frame));
            }
        }

        public bool IsInGame => client != null && cts != null && client.State == ZirconConnectionState.InGame;

        public ZirconWorldSnapshot GetWorldSnapshot()
        {
            return worldState.GetSnapshot();
        }

        public async Task SendTurnCommandAsync(byte direction)
        {
            if (!CanSendInGameAction())
                return;

            try
            {
                await client.SendTurnAsync(direction, cts.Token);
                AppendLog($"sent turn direction={direction}");
            }
            catch (Exception ex)
            {
                AppendLog($"turn send failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task SendMoveCommandAsync(byte direction, int distance)
        {
            if (!CanSendInGameAction())
                return;

            int clampedDistance = Mathf.Clamp(distance, 1, 2);
            try
            {
                await client.SendMoveAsync(direction, clampedDistance, cts.Token);
                AppendLog($"sent move direction={direction} distance={clampedDistance}");
            }
            catch (Exception ex)
            {
                AppendLog($"move send failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task SendAttackCommandAsync(byte direction, int attackMagic = 0)
        {
            if (!CanSendInGameAction())
                return;

            try
            {
                await client.SendAttackAsync(direction, ZirconMirAction.Attack, attackMagic, cts.Token);
                AppendLog($"sent attack direction={direction} magic={attackMagic}");
            }
            catch (Exception ex)
            {
                AppendLog($"attack send failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task SendNpcCallCommandAsync(uint objectId)
        {
            if (!CanSendInGameAction())
                return;

            try
            {
                await client.SendNpcCallAsync(objectId, cts.Token);
                AppendLog($"sent npc call object={objectId}");
            }
            catch (Exception ex)
            {
                AppendLog($"npc call failed: {ex.GetType().Name} {ex.Message}");
            }
        }
        public async Task SendPickUpCommandAsync(byte type)
        {
            if (!CanSendInGameAction())
                return;

            try
            {
                await client.SendPickUpAsync(type, cts.Token);
                AppendLog($"sent pickup type={type}");
            }
            catch (Exception ex)
            {
                AppendLog($"pickup send failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task SendMagicCommandAsync(byte direction, int magicType, uint target, ZirconMapPoint location)
        {
            if (!CanSendInGameAction())
                return;

            try
            {
                await client.SendMagicAsync(direction, magicType, target, location, cts.Token);
                AppendLog($"sent magic direction={direction} type={magicType} target={target} location={location.X},{location.Y}");
            }
            catch (Exception ex)
            {
                AppendLog($"magic send failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task SendChatCommandAsync(string text)
        {
            if (!CanSendInGameAction())
                return;

            if (string.IsNullOrWhiteSpace(text))
            {
                AppendLog("chat text is empty");
                return;
            }

            try
            {
                await client.SendChatAsync(text, cts.Token);
                AppendLog($"sent chat length={text.Length}");
            }
            catch (Exception ex)
            {
                AppendLog($"chat send failed: {ex.GetType().Name} {ex.Message}");
            }
        }
        public async Task SendGamePacketCommandAsync(byte[] packet, string label)
        {
            if (!CanSendInGameAction() || packet == null) return;
            try { await client.SendAsync(packet, cts.Token); AppendLog("sent " + (label ?? "game packet")); }
            catch (Exception ex) { AppendLog($"game packet failed: {ex.GetType().Name} {ex.Message}"); }
        }
        public async Task SendQuestAcceptCommandAsync(int index) => await SendQuestCommandAsync(() => client.SendQuestAcceptAsync(index, cts.Token), $"quest accept={index}");
        public async Task SendQuestCompleteCommandAsync(int index, int choiceIndex) => await SendQuestCommandAsync(() => client.SendQuestCompleteAsync(index, choiceIndex, cts.Token), $"quest complete={index} choice={choiceIndex}");
        public async Task SendQuestTrackCommandAsync(int index, bool track) => await SendQuestCommandAsync(() => client.SendQuestTrackAsync(index, track, cts.Token), $"quest track={index} enabled={track}");

        private async Task SendQuestCommandAsync(Func<Task> send, string message)
        {
            if (!CanSendInGameAction()) return;
            try { await send(); AppendLog("sent " + message); }
            catch (Exception ex) { AppendLog($"quest send failed: {ex.GetType().Name} {ex.Message}"); }
        }
        public async Task SendMagicKeyCommandAsync(int infoIndex, int magicType, byte set1Key, byte set2Key, byte set3Key, byte set4Key)
        {
            if (!CanSendInGameAction())
                return;

            try
            {
                await client.SendMagicKeyAsync(magicType, set1Key, set2Key, set3Key, set4Key, cts.Token);
                worldState.SetSkillKeys(infoIndex, set1Key, set2Key, set3Key, set4Key);
                AppendLog($"sent magic key info={infoIndex} type={magicType} keys={set1Key}/{set2Key}/{set3Key}/{set4Key}");
            }
            catch (Exception ex)
            {
                AppendLog($"magic key send failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task SendSortStorageCommandAsync()
        {
            if (!CanSendInGameAction())
                return;

            try
            {
                await client.SendSortStorageItemAsync(cts.Token);
                AppendLog("sent storage sort");
            }
            catch (Exception ex)
            {
                AppendLog($"storage sort failed: {ex.GetType().Name} {ex.Message}");
            }
        }
        public async Task SendItemMoveCommandAsync(ZirconGridType fromGrid, ZirconGridType toGrid, int fromSlot, int toSlot, bool mergeItem)
        {
            if (!CanSendInGameAction())
                return;
            try
            {
                await client.SendItemMoveAsync(fromGrid, toGrid, fromSlot, toSlot, mergeItem, cts.Token);
                AppendLog($"sent item move {fromGrid}:{fromSlot} -> {toGrid}:{toSlot} merge={mergeItem}");
            }
            catch (Exception ex)
            {
                AppendLog($"item move failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task SendItemUseCommandAsync(ZirconGridType grid, int slot, long count = 1)
        {
            if (!CanSendInGameAction())
                return;
            try
            {
                await client.SendItemUseAsync(grid, slot, count, cts.Token);
                AppendLog($"sent item use {grid}:{slot} count={count}");
            }
            catch (Exception ex)
            {
                AppendLog($"item use failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task SendItemLockCommandAsync(ZirconGridType grid, int slot, bool locked)
        {
            if (!CanSendInGameAction())
                return;
            try
            {
                await client.SendItemLockAsync(grid, slot, locked, cts.Token);
                AppendLog($"sent item lock {grid}:{slot} locked={locked}");
            }
            catch (Exception ex)
            {
                AppendLog($"item lock failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task SendNpcButtonCommandAsync(int buttonId)
        {
            if (!CanSendInGameAction())
                return;
            try
            {
                await client.SendNpcButtonAsync(buttonId, cts.Token);
                AppendLog($"sent npc button={buttonId}");
            }
            catch (Exception ex)
            {
                AppendLog($"npc button failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task SendNpcBuyCommandAsync(int itemInfoIndex, long amount)
        {
            if (!CanSendInGameAction())
                return;
            try
            {
                await client.SendNpcBuyAsync(itemInfoIndex, amount, false, cts.Token);
                AppendLog($"sent npc buy item={itemInfoIndex} amount={amount}");
            }
            catch (Exception ex)
            {
                AppendLog($"npc buy failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task SendNpcSellCommandAsync(IReadOnlyList<ZirconCellLinkInfo> links)
        {
            if (!CanSendInGameAction()) return;
            try
            {
                await client.SendNpcSellAsync(links, cts.Token);
                AppendLog($"sent npc sell links={links?.Count ?? 0}");
            }
            catch (Exception ex) { AppendLog($"npc sell failed: {ex.GetType().Name} {ex.Message}"); }
        }

        public async Task SendNpcRepairCommandAsync(IReadOnlyList<ZirconCellLinkInfo> links, bool special, bool guildFunds)
        {
            if (!CanSendInGameAction()) return;
            try
            {
                await client.SendNpcRepairAsync(links, special, guildFunds, cts.Token);
                AppendLog($"sent npc repair links={links?.Count ?? 0} special={special} guild={guildFunds}");
            }
            catch (Exception ex) { AppendLog($"npc repair failed: {ex.GetType().Name} {ex.Message}"); }
        }
        public async Task SendNpcCloseCommandAsync()
        {
            if (!CanSendInGameAction())
                return;
            try
            {
                await client.SendNpcCloseAsync(cts.Token);
                AppendLog("sent npc close");
            }
            catch (Exception ex)
            {
                AppendLog($"npc close failed: {ex.GetType().Name} {ex.Message}");
            }
        }
        private void DrawWorldSummary()
        {
            ZirconWorldSnapshot snapshot = worldState.GetSnapshot();
            if (snapshot.HasLocalPlayer && snapshot.LocalPlayer != null)
            {
                GUILayout.Label($"World: {snapshot.LocalPlayer.Name} map={snapshot.MapIndex} xy={snapshot.Location.X},{snapshot.Location.Y} entities={snapshot.Entities.Count} chat={snapshot.ChatMessages.Count} stats={snapshot.Stats.Count} weight={snapshot.BagWeight}/{snapshot.WearWeight}/{snapshot.HandWeight} gold={snapshot.Gold} gameGold={snapshot.GameGold} huntGold={snapshot.HuntGold} skillLimit={snapshot.SkillLevelLimit}");
            }
            else
            {
                GUILayout.Label($"World: waiting for StartGame entities={snapshot.Entities.Count} chat={snapshot.ChatMessages.Count}");
            }
        }

        private void DrawActionControls()
        {
            GUILayout.Label("Actions");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Direction", GUILayout.Width(70));
            actionDirection = ParseIntField(actionDirection, GUILayout.TextField(actionDirection.ToString(), GUILayout.Width(42)));
            GUILayout.Label("Distance", GUILayout.Width(62));
            moveDistance = ParseIntField(moveDistance, GUILayout.TextField(moveDistance.ToString(), GUILayout.Width(42)));
            GUILayout.Label("Pick", GUILayout.Width(36));
            pickType = ParseIntField(pickType, GUILayout.TextField(pickType.ToString(), GUILayout.Width(42)));
            GUILayout.EndHorizontal();

            chatText = GUILayout.TextField(chatText);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Turn", GUILayout.Height(34)))
                _ = SendTurnAsync();

            if (GUILayout.Button("Move", GUILayout.Height(34)))
                _ = SendMoveAsync();

            if (GUILayout.Button("PickUp", GUILayout.Height(34)))
                _ = SendPickUpAsync();

            if (GUILayout.Button("Chat", GUILayout.Height(34)))
                _ = SendChatAsync();
            GUILayout.EndHorizontal();
        }

        private async Task SendTurnAsync()
        {
            await SendTurnCommandAsync(ClampByte(actionDirection, 0, 7));
        }

        private async Task SendMoveAsync()
        {
            await SendMoveCommandAsync(ClampByte(actionDirection, 0, 7), moveDistance);
        }

        private async Task SendPickUpAsync()
        {
            await SendPickUpCommandAsync(ClampByte(pickType, 0, 255));
        }

        private async Task SendChatAsync()
        {
            await SendChatCommandAsync(chatText);
        }

        public bool CanSendInGameAction()
        {
            if (client == null || cts == null || client.State != ZirconConnectionState.InGame)
            {
                AppendLog("action skipped: client is not in game");
                return false;
            }

            return true;
        }

        private static int ParseIntField(int fallback, string text)
        {
            return int.TryParse(text, out int value) ? value : fallback;
        }

        private static byte ClampByte(int value, int min, int max)
        {
            return (byte)Mathf.Clamp(value, min, max);
        }

        private void AppendLog(string message)
        {
            pendingLogs.Enqueue(message);
        }

        private void FlushLogs()
        {
            while (pendingLogs.TryDequeue(out string message))
            {
                logBuilder.Append('[').Append(DateTime.Now.ToString("HH:mm:ss")).Append("] ").AppendLine(message);
                Debug.Log(message);
            }
        }

        private void Update()
        {
            while (pendingMainThreadActions.TryDequeue(out Action action))
                action();

            FlushLogs();
            worldRenderer?.Render(worldState.GetSnapshot());
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        private void Disconnect()
        {
            cts?.Cancel();
            client?.Dispose();
            client = null;
            cts?.Dispose();
            cts = null;
            worldState.Reset();
            worldRenderer?.Clear();
            characters = Array.Empty<ZirconCharacterSelectInfo>();
            CharactersChanged?.Invoke(characters);
            ConnectionStateChanged?.Invoke(ZirconConnectionState.Disconnected);
        }
    }
}








