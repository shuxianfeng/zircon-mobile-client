using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Zircon.Mobile.Core.Protocol;
using Zircon.Mobile.Game.Entities;
using Zircon.Mobile.Game.World;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var options = Options.Parse(args);
        string email = Environment.GetEnvironmentVariable("ZIRCON_PROBE_EMAIL") ?? string.Empty;
        string password = Environment.GetEnvironmentVariable("ZIRCON_PROBE_PASSWORD") ?? string.Empty;
        if (email.Length == 0 || password.Length == 0)
            return Fail("Set ZIRCON_PROBE_EMAIL and ZIRCON_PROBE_PASSWORD; credentials are never accepted on the command line.");

        var world = new ZirconWorldState();
        var buffer = new List<byte>();
        var seenIds = new HashSet<ushort>();
        int applied = 0, monsterFrames = 0, npcFrames = 0, moveFrames = 0, attackFrames = 0, magicFrames = 0;
        bool sentMove = false, sentAttack = false, sentMagic = false, sentOutboundMap = false, sentReturnMap = false;
        bool sawOutboundMap = false, sawReturnMap = false;
        int initialMap = 0;
        ZirconMapPoint initialLocation = default;
        uint localObjectId = 0;
        DateTime inGameUtc = DateTime.MinValue, outboundUtc = DateTime.MinValue, returnUtc = DateTime.MinValue;

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
        using var client = new TcpClient(AddressFamily.InterNetworkV6);
        if (options.LocalAddress != null)
            client.Client.Bind(new IPEndPoint(options.LocalAddress, 0));

        IPAddress remote = (await Dns.GetHostAddressesAsync(options.Host, timeout.Token))
            .First(x => x.AddressFamily == AddressFamily.InterNetworkV6);
        await client.ConnectAsync(remote, options.Port, timeout.Token);
        using NetworkStream stream = client.GetStream();
        Console.WriteLine($"P2 live probe connected local={client.Client.LocalEndPoint} remote={client.Client.RemoteEndPoint}");
        var watch = Stopwatch.StartNew();

        try
        {
            while (!timeout.IsCancellationRequested)
            {
                if (stream.DataAvailable)
                {
                    byte[] chunk = new byte[8192];
                    int read = await stream.ReadAsync(chunk, timeout.Token);
                    if (read == 0) break;
                    buffer.AddRange(chunk.Take(read));
                }
                else
                {
                    await Task.Delay(50, timeout.Token);
                }

                while (ZirconBinary.TryReadFrame(buffer, out ZirconPacketFrame frame))
                {
                    seenIds.Add(frame.PacketId);
                    if (world.ApplyPacket(frame, out string summary))
                    {
                        applied++;
                        if (summary.Contains("world monster")) monsterFrames++;
                        if (summary.Contains("world npc")) npcFrames++;
                    }

                    switch (frame.PacketId)
                    {
                        case ZirconPacketIds.General.Connected:
                            await Send(stream, ZirconClientPackets.Connected(), timeout.Token);
                            break;
                        case ZirconPacketIds.General.Ping:
                            await Send(stream, ZirconClientPackets.Ping(), timeout.Token);
                            break;
                        case ZirconPacketIds.General.CheckVersion:
                            await Send(stream, ZirconClientPackets.Version(Array.Empty<byte>()), timeout.Token);
                            break;
                        case ZirconPacketIds.General.GoodVersion:
                            await Send(stream, Combine(
                                ZirconClientPackets.SelectLanguage("Chinese"),
                                ZirconClientPackets.Login(email, ZirconBinary.Md5Text($"{email}-{password}"), ZirconClientPackets.CreateChecksum())), timeout.Token);
                            break;
                        case ZirconPacketIds.Server.Login:
                        case ZirconPacketIds.Server.LoginSimple:
                            if (!ZirconServerPacketDecoder.TryDecodeLogin(frame, out ZirconDecodedLogin login) || login.Characters.Count == 0)
                                return Fail("Login did not return a usable character list.");
                            ZirconCharacterSelectInfo selected = options.CharacterIndex.HasValue
                                ? login.Characters.First(x => x.Index == options.CharacterIndex.Value)
                                : login.Characters.OrderBy(x => x.Level).First();
                            Console.WriteLine($"login=passed result={login.Result} characters={login.Characters.Count} selectedIndex={selected.Index} selectedLevel={selected.Level}");
                            await Send(stream, ZirconClientPackets.StartGame(selected.Index), timeout.Token);
                            break;
                        case ZirconPacketIds.Server.StartGame:
                            if (!ZirconServerPacketDecoder.TryDecodeStartGame(frame, out ZirconDecodedStartGame start) || start.Result != ZirconStartGameResult.Success || !start.StartInformation.HasValue)
                                return Fail("StartGame was rejected.");
                            var info = start.StartInformation.Value;
                            initialMap = info.MapIndex;
                            initialLocation = info.Location;
                            localObjectId = info.ObjectId;
                            inGameUtc = DateTime.UtcNow;
                            Console.WriteLine($"start=passed map={initialMap} xy={initialLocation.X},{initialLocation.Y} object={localObjectId} skills={info.Magics.Count}");
                            break;
                        case ZirconPacketIds.Server.ObjectMove: moveFrames++; break;
                        case ZirconPacketIds.Server.ObjectAttack: attackFrames++; break;
                        case ZirconPacketIds.Server.ObjectMagic: magicFrames++; break;
                        case ZirconPacketIds.Server.Chat:
                            if (sentOutboundMap && ZirconInGamePacketDecoder.TryDecodeChat(frame, out ZirconChatMessageInfo chat))
                                Console.WriteLine($"server-chat type={chat.MessageType} text={chat.Text}");
                            break;
                        case ZirconPacketIds.Server.MapChanged:
                            if (ZirconMapPacketDecoder.TryDecodeMapChanged(frame, out ZirconMapChangedInfo changed))
                            {
                                if (changed.MapIndex == options.TargetMap) { sawOutboundMap = true; outboundUtc = DateTime.UtcNow; }
                                if (sentReturnMap && changed.MapIndex == initialMap) { sawReturnMap = true; returnUtc = DateTime.UtcNow; }
                                Console.WriteLine($"map-packet map={changed.MapIndex} instance={changed.InstanceIndex} worldMap={world.GetSnapshot().MapIndex}");
                            }
                            break;
                    }
                }

                if (inGameUtc == DateTime.MinValue) continue;
                TimeSpan elapsed = DateTime.UtcNow - inGameUtc;
                ZirconWorldSnapshot snapshot = world.GetSnapshot();
                if (!options.MapOnly && !sentMove && elapsed.TotalSeconds >= 1) { await Send(stream, ZirconClientPackets.Move(2, 1), timeout.Token); sentMove = true; }
                if (!options.MapOnly && !sentAttack && elapsed.TotalSeconds >= 2.5) { await Send(stream, ZirconClientPackets.Attack(0), timeout.Token); sentAttack = true; }
                if (!options.MapOnly && !sentMagic && elapsed.TotalSeconds >= 4) { await Send(stream, ZirconClientPackets.Magic(0, 205, 0, snapshot.Location), timeout.Token); sentMagic = true; }
                if (!sentOutboundMap && elapsed.TotalSeconds >= 6) { await Send(stream, ZirconClientPackets.Chat($"@MAP {options.TargetFile}"), timeout.Token); sentOutboundMap = true; }
                if (sawOutboundMap && !sentReturnMap && (DateTime.UtcNow - outboundUtc).TotalSeconds >= 3)
                {
                    string initialFile = options.MapFiles.GetValueOrDefault(initialMap) ?? throw new InvalidOperationException($"No file name is known for initial map {initialMap}.");
                    await Send(stream, ZirconClientPackets.Chat($"@MAP {initialFile}"), timeout.Token);
                    sentReturnMap = true;
                }
                if (sawReturnMap && (DateTime.UtcNow - returnUtc).TotalSeconds >= 3) break;
            }
        }
        catch (OperationCanceledException) { }

        ZirconWorldSnapshot final = world.GetSnapshot();
        int monsters = final.Entities.Count(x => x.Kind == ZirconEntityKind.Monster);
        int npcs = final.Entities.Count(x => x.Kind == ZirconEntityKind.Npc);
        Console.WriteLine($"P2-A4 summary applied={applied} uniquePackets={seenIds.Count} monsterFrames={monsterFrames} npcFrames={npcFrames} moveFrames={moveFrames} attackFrames={attackFrames} magicFrames={magicFrames} entities={final.Entities.Count} monsters={monsters} npcs={npcs}");
        Console.WriteLine($"P2-A5 summary outbound={sawOutboundMap} returned={sawReturnMap} initialMap={initialMap} finalMap={final.MapIndex} finalXY={final.Location.X},{final.Location.Y}");

        bool a4 = options.MapOnly || (final.HasLocalPlayer && applied > 0 && monsterFrames > 0 && npcFrames > 0 && moveFrames > 0 && attackFrames > 0 && magicFrames > 0);
        bool a5 = sawOutboundMap && sawReturnMap && final.MapIndex == initialMap;
        if (!a4 || !a5) return Fail($"Acceptance failed: P2-A4={a4} P2-A5={a5}");
        Console.WriteLine("P2-A4=PASS P2-A5=PASS");
        return 0;
    }

    private static async Task Send(NetworkStream stream, byte[] bytes, CancellationToken token) => await stream.WriteAsync(bytes, token);
    private static byte[] Combine(params byte[][] packets) => packets.SelectMany(x => x).ToArray();
    private static int Fail(string message) { Console.Error.WriteLine(message); return 1; }

    private sealed class Options
    {
        public string Host { get; init; } = "zircon.35861344.xyz";
        public int Port { get; init; } = 17000;
        public int TimeoutSeconds { get; init; } = 35;
        public IPAddress LocalAddress { get; init; }
        public int? CharacterIndex { get; init; }
        public bool MapOnly { get; init; }
        public int TargetMap { get; init; } = 1;
        public string TargetFile { get; init; } = "0";
        public int TargetX { get; init; } = 160;
        public int TargetY { get; init; } = 234;
        public Dictionary<int, string> MapFiles { get; } = new() { [1] = "0", [5] = "1", [6] = "2" };

        public static Options Parse(string[] args)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < args.Length; i += 2) values[args[i].TrimStart('-')] = args[i + 1];
            return new Options
            {
                Host = values.GetValueOrDefault("host") ?? "zircon.35861344.xyz",
                Port = int.Parse(values.GetValueOrDefault("port") ?? "17000"),
                TimeoutSeconds = int.Parse(values.GetValueOrDefault("timeout") ?? "35"),
                LocalAddress = values.TryGetValue("local-address", out string local) ? IPAddress.Parse(local) : null,
                CharacterIndex = values.TryGetValue("character-index", out string character) ? int.Parse(character) : null,
                MapOnly = values.GetValueOrDefault("map-only") == "true",
                TargetMap = int.Parse(values.GetValueOrDefault("target-map") ?? "1"),
                TargetFile = values.GetValueOrDefault("target-file") ?? "0",
                TargetX = int.Parse(values.GetValueOrDefault("target-x") ?? "160"),
                TargetY = int.Parse(values.GetValueOrDefault("target-y") ?? "234"),
            };
        }
    }
}
