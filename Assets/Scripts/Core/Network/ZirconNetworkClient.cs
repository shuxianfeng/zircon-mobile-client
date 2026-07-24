using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zircon.Mobile.Core.Models;
using Zircon.Mobile.Core.Protocol;

namespace Zircon.Mobile.Core.Network
{
    public sealed class ZirconNetworkClient : IDisposable
    {
        private readonly ZirconClientConfig config;
        private readonly List<byte> receiveBuffer = new List<byte>();
        private TcpClient client;
        private NetworkStream stream;
        private CancellationTokenSource receiveLoopCts;

        public ZirconNetworkClient(ZirconClientConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public ZirconConnectionState State { get; private set; } = ZirconConnectionState.Disconnected;
        public event EventHandler<ZirconPacketEventArgs> PacketReceived;
        public event Action<string> Log;
        public event Action<ZirconConnectionState> StateChanged;

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            SetState(ZirconConnectionState.Connecting);

            IPAddress address = await ResolveAddressAsync(cancellationToken).ConfigureAwait(false);
            client = new TcpClient(address.AddressFamily) { NoDelay = true };
            Task connectTask = client.ConnectAsync(address, config.Port);
            Task timeoutTask = Task.Delay(Math.Max(500, config.ConnectTimeoutMilliseconds), cancellationToken);
            Task completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
            if (completed != connectTask)
            {
                client.Close();
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException($"Connection to {config.Host}:{config.Port} timed out.");
            }
            await connectTask.ConfigureAwait(false);
            stream = client.GetStream();

            SetState(ZirconConnectionState.Connected);
            receiveLoopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = Task.Run(() => ReceiveLoopAsync(receiveLoopCts.Token), receiveLoopCts.Token);
        }

        public Task SendLoginAsync(string email, string plainPassword, CancellationToken cancellationToken = default)
        {
            string passwordHash = Md5Text($"{email}-{plainPassword}");
            return SendLoginHashAsync(email, passwordHash, cancellationToken);
        }

        public async Task SendLoginHashAsync(string email, string passwordHash, CancellationToken cancellationToken = default)
        {
            SetState(ZirconConnectionState.LoggingIn);
            await SendAsync(ZirconClientPackets.SelectLanguage(config.Language), cancellationToken).ConfigureAwait(false);
            await SendAsync(ZirconClientPackets.Login(email, passwordHash, config.Checksum), cancellationToken).ConfigureAwait(false);
        }

        public Task SendStartGameAsync(int characterIndex, CancellationToken cancellationToken = default)
        {
            SetState(ZirconConnectionState.LoadingMap);
            return SendAsync(ZirconClientPackets.StartGame(characterIndex), cancellationToken);
        }
        public Task SendTurnAsync(byte direction, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.Turn(direction), cancellationToken);
        }

        public Task SendMoveAsync(byte direction, int distance, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.Move(direction, distance), cancellationToken);
        }

        public Task SendAttackAsync(byte direction, ZirconMirAction action = ZirconMirAction.Attack, int attackMagic = 0, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.Attack(direction, action, attackMagic), cancellationToken);
        }

        public Task SendMagicAsync(byte direction, int magicType, uint target, ZirconMapPoint location, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.Magic(direction, magicType, target, location), cancellationToken);
        }

        public Task SendChatAsync(string text, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.Chat(text), cancellationToken);
        }

        public Task SendNpcCallAsync(uint objectId, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.NpcCall(objectId), cancellationToken);
        }
        public Task SendPickUpAsync(byte pickType, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.PickUp(pickType), cancellationToken);
        }

        public Task SendItemMoveAsync(ZirconGridType fromGrid, ZirconGridType toGrid, int fromSlot, int toSlot, bool mergeItem, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.ItemMove(fromGrid, toGrid, fromSlot, toSlot, mergeItem), cancellationToken);
        }

        public Task SendItemUseAsync(ZirconGridType grid, int slot, long count = 1, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.ItemUse(grid, slot, count), cancellationToken);
        }

        public Task SendItemLockAsync(ZirconGridType grid, int slot, bool locked, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.ItemLock(grid, slot, locked), cancellationToken);
        }

        public Task SendNpcButtonAsync(int buttonId, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.NpcButton(buttonId), cancellationToken);
        }

        public Task SendNpcBuyAsync(int itemInfoIndex, long amount, bool guildFunds = false, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.NpcBuy(itemInfoIndex, amount, guildFunds), cancellationToken);
        }

        public Task SendQuestAcceptAsync(int index, CancellationToken cancellationToken = default) => SendAsync(ZirconClientPackets.QuestAccept(index), cancellationToken);
        public Task SendQuestCompleteAsync(int index, int choiceIndex, CancellationToken cancellationToken = default) => SendAsync(ZirconClientPackets.QuestComplete(index, choiceIndex), cancellationToken);
        public Task SendQuestTrackAsync(int index, bool track, CancellationToken cancellationToken = default) => SendAsync(ZirconClientPackets.QuestTrack(index, track), cancellationToken);
        public Task SendMagicKeyAsync(int magicType, byte set1Key, byte set2Key, byte set3Key, byte set4Key, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.MagicKey(magicType, set1Key, set2Key, set3Key, set4Key), cancellationToken);
        }

        public Task SendSortStorageItemAsync(CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.SortStorageItem(), cancellationToken);
        }
        public Task SendNpcSellAsync(IReadOnlyList<ZirconCellLinkInfo> links, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.NpcSell(links), cancellationToken);
        }

        public Task SendNpcRepairAsync(IReadOnlyList<ZirconCellLinkInfo> links, bool special, bool guildFunds, CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.NpcRepair(links, special, guildFunds), cancellationToken);
        }
        public Task SendNpcCloseAsync(CancellationToken cancellationToken = default)
        {
            return SendAsync(ZirconClientPackets.NpcClose(), cancellationToken);
        }
        public async Task SendAsync(byte[] packet, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new InvalidOperationException("Client is not connected.");

            await stream.WriteAsync(packet, 0, packet.Length, cancellationToken).ConfigureAwait(false);
            Log?.Invoke($"send bytes={packet.Length} hex={ZirconBinary.ToHex(packet, 96)}");
        }

        public void Disconnect()
        {
            receiveLoopCts?.Cancel();
            stream?.Dispose();
            client?.Close();
            stream = null;
            client = null;
            SetState(ZirconConnectionState.Disconnected);
        }

        public void Dispose()
        {
            Disconnect();
            receiveLoopCts?.Dispose();
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var temp = new byte[Math.Max(1024, config.ReceiveBufferSize)];

            try
            {
                while (!cancellationToken.IsCancellationRequested && client != null && client.Connected)
                {
                    int read = await stream.ReadAsync(temp, 0, temp.Length, cancellationToken).ConfigureAwait(false);
                    if (read <= 0)
                        break;

                    for (int i = 0; i < read; i++)
                        receiveBuffer.Add(temp[i]);

                    while (ZirconBinary.TryReadFrame(receiveBuffer, out ZirconPacketFrame frame))
                        HandleFrame(frame, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Log?.Invoke($"receive error={ex.GetType().Name} message={ex.Message}");
            }

            if (!cancellationToken.IsCancellationRequested)
                Disconnect();
        }

        private void HandleFrame(ZirconPacketFrame frame, CancellationToken cancellationToken)
        {
            Log?.Invoke($"recv id={frame.PacketId} length={frame.Length} hex={ZirconBinary.ToHex(frame.RawBytes, 96)}");

            if (frame.PacketId == ZirconPacketIds.General.Connected)
            {
                _ = SendAsync(ZirconClientPackets.Connected(), cancellationToken);
            }
            else if (frame.PacketId == ZirconPacketIds.General.Ping)
            {
                _ = SendAsync(ZirconClientPackets.Ping(), cancellationToken);
            }
            else if (frame.PacketId == ZirconPacketIds.General.CheckVersion)
            {
                SetState(ZirconConnectionState.VersionChecking);
                _ = SendAsync(ZirconClientPackets.Version(Array.Empty<byte>()), cancellationToken);
            }
            else if (frame.PacketId == ZirconPacketIds.General.GoodVersion)
            {
                SetState(ZirconConnectionState.ReadyForLogin);
            }
            else if (frame.PacketId == ZirconPacketIds.Server.Login || frame.PacketId == ZirconPacketIds.Server.LoginSimple)
            {
                if (ZirconServerPacketDecoder.TryDecodeLogin(frame, out ZirconDecodedLogin login) &&
                    login.Result == ZirconLoginResult.Success)
                    SetState(ZirconConnectionState.SelectingCharacter);
                else
                    SetState(ZirconConnectionState.ReadyForLogin);
            }
            else if (frame.PacketId == ZirconPacketIds.Server.StartGame)
            {
                SetState(ZirconConnectionState.InGame);
            }

            PacketReceived?.Invoke(this, new ZirconPacketEventArgs(frame));
        }

        private async Task<IPAddress> ResolveAddressAsync(CancellationToken cancellationToken)
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(config.Host).ConfigureAwait(false);
            foreach (IPAddress address in addresses)
            {
                if (config.PreferIpv6 && address.AddressFamily == AddressFamily.InterNetworkV6)
                    return address;
                if (!config.PreferIpv6 && address.AddressFamily == AddressFamily.InterNetwork)
                    return address;
            }

            if (addresses.Length == 0)
                throw new InvalidOperationException($"Unable to resolve host: {config.Host}");

            return addresses[0];
        }

        private void SetState(ZirconConnectionState state)
        {
            if (State == state)
                return;

            State = state;
            StateChanged?.Invoke(state);
            Log?.Invoke($"state={state}");
        }

        private static string Md5Text(string text)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (byte value in bytes)
                    builder.Append(value.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}


