using System.Net;
using System.Net.Sockets;
using Zircon.Mobile.Core.Network;
using Zircon.Mobile.Core.Protocol;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        ProbeOptions options;

        try
        {
            options = ProbeOptions.Parse(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            ProbeOptions.PrintUsage();
            return 2;
        }

        if (options.Help)
        {
            ProbeOptions.PrintUsage();
            return 0;
        }

        var state = ZirconConnectionState.Disconnected;
        var received = new List<byte>();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
        using var client = new TcpClient(options.ForceIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork);

        try
        {
            state = ZirconConnectionState.Connecting;
            Console.WriteLine($"[{DateTimeOffset.Now:O}] state={state} host={options.Host} port={options.Port}");

            IPAddress[] addresses = await Dns.GetHostAddressesAsync(options.Host, cts.Token);
            Console.WriteLine($"[{DateTimeOffset.Now:O}] dns={string.Join(",", addresses.Select(x => x.ToString()))}");

            IPAddress address = addresses.FirstOrDefault(x => x.AddressFamily == client.Client.AddressFamily)
                                ?? addresses.First();

            await client.ConnectAsync(address, options.Port, cts.Token);
            state = ZirconConnectionState.Connected;

            using NetworkStream stream = client.GetStream();
            Console.WriteLine($"[{DateTimeOffset.Now:O}] state={state} remote={address}:{options.Port}");

            while (!cts.IsCancellationRequested && client.Connected)
            {
                byte[] chunk = new byte[8192];
                int read = await stream.ReadAsync(chunk, cts.Token);

                if (read == 0)
                {
                    Console.WriteLine($"[{DateTimeOffset.Now:O}] disconnected=remote_closed");
                    return 0;
                }

                received.AddRange(chunk.Take(read));

                while (ZirconBinary.TryReadFrame(received, out ZirconPacketFrame frame))
                {
                    string summary = ZirconPacketSummary.Describe(frame);
                    string hex = options.DumpHex ? $" hex={ZirconBinary.ToHex(frame.RawBytes)}" : string.Empty;
                    Console.WriteLine(
                        $"[{DateTimeOffset.Now:O}] recv id={frame.PacketId} name={PacketName(frame.PacketId)} length={frame.Length} payload={frame.Payload.Length} summary=\"{summary}\"{hex}");

                    byte[]? response = HandleFrame(frame, options, ref state);

                    if (response != null)
                    {
                        await stream.WriteAsync(response, cts.Token);
                        string sendHex = options.DumpHex ? $" hex={ZirconBinary.ToHex(response)}" : string.Empty;
                        Console.WriteLine(
                            $"[{DateTimeOffset.Now:O}] send state={state} length={response.Length}{sendHex}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[{DateTimeOffset.Now:O}] timeout_seconds={options.TimeoutSeconds} state={state}");
            return 1;
        }
        catch (SocketException ex)
        {
            Console.Error.WriteLine($"[{DateTimeOffset.Now:O}] socket_error={ex.SocketErrorCode} message={ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[{DateTimeOffset.Now:O}] error={ex.GetType().Name} message={ex.Message}");
            return 1;
        }

        return 0;
    }

    private static byte[]? HandleFrame(ZirconPacketFrame frame, ProbeOptions options, ref ZirconConnectionState state)
    {
        switch (frame.PacketId)
        {
            case ZirconPacketIds.General.Connected:
                return ZirconClientPackets.Connected();

            case ZirconPacketIds.General.Ping:
                return ZirconClientPackets.Ping();

            case ZirconPacketIds.General.CheckVersion:
                state = ZirconConnectionState.VersionChecking;
                return ZirconClientPackets.Version(options.ClientHash);

            case ZirconPacketIds.General.GoodVersion:
                state = ZirconConnectionState.ReadyForLogin;
                return BuildReadyForLoginResponse(options, ref state);

            case ZirconPacketIds.Server.Login:
            case ZirconPacketIds.Server.LoginSimple:
                state = ZirconConnectionState.SelectingCharacter;
                return options.StartCharacterIndex.HasValue
                    ? ZirconClientPackets.StartGame(options.StartCharacterIndex.Value)
                    : null;

            case ZirconPacketIds.Server.StartGame:
                state = ZirconConnectionState.InGame;
                return BuildInGameActionResponse(frame, options);
        }

        if (state == ZirconConnectionState.ReadyForLogin && options.CanLogin)
        {
            state = ZirconConnectionState.LoggingIn;
            return options.UseLoginSimple
                ? ZirconClientPackets.LoginSimple(options.Email!, options.PasswordHash!, options.Checksum)
                : ZirconClientPackets.Login(options.Email!, options.PasswordHash!, options.Checksum);
        }

        return null;
    }

    private static byte[]? BuildReadyForLoginResponse(ProbeOptions options, ref ZirconConnectionState state)
    {
        var packets = new List<byte>();

        if (!string.IsNullOrWhiteSpace(options.Language))
            packets.AddRange(ZirconClientPackets.SelectLanguage(options.Language));

        if (options.CanLogin)
        {
            state = ZirconConnectionState.LoggingIn;
            packets.AddRange(options.UseLoginSimple
                ? ZirconClientPackets.LoginSimple(options.Email!, options.PasswordHash!, options.Checksum)
                : ZirconClientPackets.Login(options.Email!, options.PasswordHash!, options.Checksum));
        }

        return packets.Count == 0 ? null : packets.ToArray();
    }

    private static byte[]? BuildInGameActionResponse(ZirconPacketFrame frame, ProbeOptions options)
    {
        if (!ZirconServerPacketDecoder.TryDecodeStartGame(frame, out ZirconDecodedStartGame startGame) || startGame.Result != ZirconStartGameResult.Success)
            return null;

        var packets = new List<byte>();
        if (startGame.StartInformation.HasValue)
        {
            ZirconStartInformation info = startGame.StartInformation.Value;
            Console.WriteLine($"[{DateTimeOffset.Now:O}] start character={info.Name} object={info.ObjectId} map={info.MapIndex} xy={info.Location.X},{info.Location.Y} items={info.Items.Count} skills={info.Magics.Count}");
            foreach (ZirconUserItemInfo item in info.Items)
                Console.WriteLine($"[{DateTimeOffset.Now:O}] item index={item.Index} infoIndex={item.InfoIndex} slot={item.Slot} count={item.Count} level={item.Level} flags={item.Flags}");
            foreach (ZirconUserMagicInfo magic in info.Magics)
                Console.WriteLine($"[{DateTimeOffset.Now:O}] skill index={magic.Index} infoIndex={magic.InfoIndex} level={magic.Level} exp={magic.Experience} cooldownMs={magic.Cooldown.TotalMilliseconds:0} keys={magic.Set1Key},{magic.Set2Key},{magic.Set3Key},{magic.Set4Key}");
        }

        for (int i = 0; i < options.MoveCount; i++)
        {
            byte direction = options.MoveDirections[i % options.MoveDirections.Count];
            packets.AddRange(ZirconClientPackets.Move(direction, options.MoveDistance));
            Console.WriteLine($"[{DateTimeOffset.Now:O}] queue Client.Move direction={direction} distance={options.MoveDistance}");
        }

        if (options.AttackDirection.HasValue)
        {
            packets.AddRange(ZirconClientPackets.Attack(options.AttackDirection.Value, ZirconMirAction.Attack, options.AttackMagic));
            Console.WriteLine($"[{DateTimeOffset.Now:O}] queue Client.Attack direction={options.AttackDirection.Value} magic={options.AttackMagic}");
        }
        if (options.NpcCallObjectId.HasValue)
        {
            packets.AddRange(ZirconClientPackets.NpcCall(options.NpcCallObjectId.Value));
            Console.WriteLine($"[{DateTimeOffset.Now:O}] queue Client.NPCCall object={options.NpcCallObjectId.Value}");
        }
        if (options.MagicType.HasValue)
        {
            ZirconMapPoint location = options.MagicLocation ?? startGame.StartInformation?.Location ?? default;
            packets.AddRange(ZirconClientPackets.Magic(options.MagicDirection, options.MagicType.Value, options.MagicTarget, location));
            Console.WriteLine($"[{DateTimeOffset.Now:O}] queue Client.Magic direction={options.MagicDirection} type={options.MagicType.Value} target={options.MagicTarget} xy={location.X},{location.Y}");
        }
        if (options.PickUp)
        {
            packets.AddRange(ZirconClientPackets.PickUp(options.PickUpType));
            Console.WriteLine($"[{DateTimeOffset.Now:O}] queue Client.PickUp type={options.PickUpType}");
        }
        if (options.DropAndPickUp && startGame.StartInformation.HasValue)
        {
            const int unsafeFlags = 1 | 32 | 64 | 128;
            ZirconUserItemInfo? candidate = startGame.StartInformation.Value.Items
                .Where(item => item.Count > 1 && (item.Flags & unsafeFlags) == 0)
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Slot)
                .Select(item => (ZirconUserItemInfo?)item)
                .FirstOrDefault();

            if (candidate.HasValue)
            {
                packets.AddRange(ZirconClientPackets.ItemDrop(candidate.Value.Slot, 1));
                packets.AddRange(ZirconClientPackets.PickUp(0));
                Console.WriteLine($"[{DateTimeOffset.Now:O}] queue Client.ItemDrop slot={candidate.Value.Slot} count=1 infoIndex={candidate.Value.InfoIndex}");
                Console.WriteLine($"[{DateTimeOffset.Now:O}] queue Client.PickUp type=0 after drop");
            }
            else
            {
                Console.WriteLine($"[{DateTimeOffset.Now:O}] skip drop-and-pick-up reason=no-safe-stacked-item");
            }
        }
        return packets.Count == 0 ? null : packets.ToArray();
    }

    private static string PacketName(ushort id)
    {
        return id switch
        {
            ZirconPacketIds.General.Connected => "General.Connected",
            ZirconPacketIds.General.Ping => "General.Ping",
            ZirconPacketIds.General.CheckVersion => "General.CheckVersion",
            ZirconPacketIds.General.Version => "General.Version",
            ZirconPacketIds.General.GoodVersion => "General.GoodVersion",
            ZirconPacketIds.General.PingResponse => "General.PingResponse",
            ZirconPacketIds.General.Disconnect => "General.Disconnect",
            ZirconPacketIds.Server.Login => "Server.Login",
            ZirconPacketIds.Server.StartGame => "Server.StartGame",
            ZirconPacketIds.Server.ObjectTurn => "Server.ObjectTurn",
            ZirconPacketIds.Server.ObjectMove => "Server.ObjectMove",
            ZirconPacketIds.Server.ObjectAttack => "Server.ObjectAttack",
            ZirconPacketIds.Server.ObjectMagic => "Server.ObjectMagic",
            ZirconPacketIds.Server.ObjectItem => "Server.ObjectItem",
            ZirconPacketIds.Server.MagicToggle => "Server.MagicToggle",
            ZirconPacketIds.Server.NewMagic => "Server.NewMagic",
            ZirconPacketIds.Server.MagicLeveled => "Server.MagicLeveled",
            ZirconPacketIds.Server.MagicCooldown => "Server.MagicCooldown",
            ZirconPacketIds.Server.ItemsGained => "Server.ItemsGained",
            ZirconPacketIds.Server.ItemChanged => "Server.ItemChanged",
            ZirconPacketIds.Server.DataObjectItem => "Server.DataObjectItem",
            ZirconPacketIds.Server.CombatTime => "Server.CombatTime",
            ZirconPacketIds.Server.NpcResponse => "Server.NPCResponse",
            ZirconPacketIds.Server.NpcClose => "Server.NPCClose",
            ZirconPacketIds.Server.DataObjectLocation => "Server.DataObjectLocation",
            ZirconPacketIds.Server.LoginSimple => "Server.LoginSimple",
            _ => "Unknown",
        };
    }
}

internal sealed class ProbeOptions
{
    public string Host { get; private init; } = "zircon.35861344.xyz";
    public int Port { get; private init; } = 17000;
    public int TimeoutSeconds { get; private init; } = 30;
    public string Language { get; private init; } = "Chinese";
    public bool ForceIpv6 { get; private init; }
    public bool UseLoginSimple { get; private init; }
    public bool DumpHex { get; private init; } = true;
    public bool Help { get; private init; }
    public string? Email { get; private init; }
    public string? PasswordHash { get; private init; }
    public string Checksum { get; private init; } = ZirconClientPackets.CreateChecksum();
    public byte[] ClientHash { get; private init; } = Array.Empty<byte>();
    public int? StartCharacterIndex { get; private init; }
    public IReadOnlyList<byte> MoveDirections { get; private init; } = Array.Empty<byte>();
    public int MoveDistance { get; private init; } = 1;
    public int MoveCount { get; private init; }
    public byte? AttackDirection { get; private init; }
    public int AttackMagic { get; private init; }
    public uint? NpcCallObjectId { get; private init; }
    public int? MagicType { get; private init; }
    public byte MagicDirection { get; private init; }
    public uint MagicTarget { get; private init; }
    public ZirconMapPoint? MagicLocation { get; private init; }
    public bool PickUp { get; private init; }
    public byte PickUpType { get; private init; }
    public bool DropAndPickUp { get; private init; }
    public bool CanLogin => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(PasswordHash);
    public bool CanMove => MoveDirections.Count > 0 && MoveCount > 0;

    public static ProbeOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Unexpected argument: {arg}");

            string key = arg[2..];
            if (key is "help" or "ipv6" or "login-simple" or "password-is-hash" or "no-hex" or "pick-up" or "drop-and-pick-up")
            {
                values[key] = "true";
                continue;
            }

            if (i + 1 >= args.Length)
                throw new ArgumentException($"Missing value for --{key}");

            values[key] = args[++i];
        }

        string? email = values.GetValueOrDefault("email");
        string? password = values.GetValueOrDefault("password");
        string? passwordHash = values.GetValueOrDefault("password-hash");

        if (!string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(email))
            passwordHash = ZirconBinary.Md5Text($"{email}-{password}");

        IReadOnlyList<byte> moveDirections = ParseMoveDirections(values);
        int moveCount = ParseInt(values.GetValueOrDefault("move-count"), moveDirections.Count > 0 ? moveDirections.Count : 0, "move-count");
        int moveDistance = ParseInt(values.GetValueOrDefault("move-distance"), 1, "move-distance");

        ZirconMapPoint? magicLocation = null;
        string? magicX = values.GetValueOrDefault("magic-x");
        string? magicY = values.GetValueOrDefault("magic-y");
        if (!string.IsNullOrWhiteSpace(magicX) || !string.IsNullOrWhiteSpace(magicY))
        {
            if (string.IsNullOrWhiteSpace(magicX) || string.IsNullOrWhiteSpace(magicY))
                throw new ArgumentException("--magic-x and --magic-y must be supplied together.");

            magicLocation = new ZirconMapPoint(ParseInt(magicX, 0, "magic-x"), ParseInt(magicY, 0, "magic-y"));
        }

        byte[] clientHash = Array.Empty<byte>();
        string? clientHashHex = values.GetValueOrDefault("client-hash");
        string? clientBinary = values.GetValueOrDefault("client-binary");

        if (!string.IsNullOrWhiteSpace(clientHashHex))
            clientHash = ZirconBinary.FromHex(clientHashHex);
        else if (!string.IsNullOrWhiteSpace(clientBinary))
            clientHash = ZirconBinary.Md5File(clientBinary);

        return new ProbeOptions
        {
            Help = values.ContainsKey("help"),
            Host = values.GetValueOrDefault("host") ?? "zircon.35861344.xyz",
            Port = ParseInt(values.GetValueOrDefault("port"), 17000, "port"),
            TimeoutSeconds = ParseInt(values.GetValueOrDefault("timeout"), 30, "timeout"),
            Language = values.GetValueOrDefault("language") ?? "Chinese",
            ForceIpv6 = values.ContainsKey("ipv6"),
            DumpHex = !values.ContainsKey("no-hex"),
            UseLoginSimple = values.ContainsKey("login-simple"),
            Email = email,
            PasswordHash = passwordHash,
            Checksum = values.GetValueOrDefault("checksum") ?? ZirconClientPackets.CreateChecksum(),
            ClientHash = clientHash,
            StartCharacterIndex = values.TryGetValue("start-character", out string? character)
                ? ParseInt(character, 0, "start-character")
                : null,
            MoveDirections = moveDirections,
            MoveCount = moveCount,
            MoveDistance = moveDistance,
            AttackDirection = ParseDirection(values.GetValueOrDefault("attack-direction"), "attack-direction"),
            AttackMagic = ParseInt(values.GetValueOrDefault("attack-magic"), 0, "attack-magic"),
            NpcCallObjectId = ParseUInt(values.GetValueOrDefault("npc-call-object"), "npc-call-object"),
            MagicType = values.TryGetValue("magic-type", out string? magicType) ? ParseInt(magicType, 0, "magic-type") : null,
            MagicDirection = ParseDirection(values.GetValueOrDefault("magic-direction"), "magic-direction") ?? 0,
            MagicTarget = ParseUInt(values.GetValueOrDefault("magic-target"), "magic-target") ?? 0,
            MagicLocation = magicLocation,
            PickUp = values.ContainsKey("pick-up"),
            PickUpType = ParseByte(values.GetValueOrDefault("pick-up-type"), 0, "pick-up-type"),
            DropAndPickUp = values.ContainsKey("drop-and-pick-up"),
        };
    }

    private static byte? ParseDirection(string? text, string name)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (!byte.TryParse(text, out byte direction) || direction > 7)
            throw new ArgumentException($"Invalid {name}: {text}. Expected 0-7.");

        return direction;
    }

    private static IReadOnlyList<byte> ParseMoveDirections(Dictionary<string, string?> values)
    {
        string? text = values.GetValueOrDefault("move-sequence") ?? values.GetValueOrDefault("move-direction");
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<byte>();

        var result = new List<byte>();
        foreach (string part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!byte.TryParse(part, out byte direction) || direction > 7)
                throw new ArgumentException($"Invalid move direction: {part}. Expected 0-7.");

            result.Add(direction);
        }

        return result;
    }

    public static void PrintUsage()
    {
        Console.WriteLine(
            """
            Zircon.ProtocolProbe

            Usage:
              dotnet run --project tools/Zircon.ProtocolProbe -- [options]

            Options:
              --host <host>                 Default: zircon.35861344.xyz
              --port <port>                 Default: 17000
              --timeout <seconds>           Default: 30
              --client-hash <hex>           MD5 bytes to send in General.Version
              --client-binary <path>        File to MD5 for General.Version
              --language <name>             Default: Chinese
              --email <email>               Enables login when paired with a password option
              --password <plain>            Sends MD5(email + "-" + password), matching PC login
              --password-hash <md5>         Sends an already hashed remembered password
              --checksum <value>            Default: random 20-character client checksum
              --login-simple                Use packet 1111 instead of 1008
              --start-character <index>     Send StartGame after login response
              --move-direction <0-7>        Send one Client.Move after StartGame success
              --move-sequence <csv>         Send Client.Move directions after StartGame, e.g. 2,4,6,0
              --move-distance <value>       Default: 1
              --move-count <value>          Default: number of directions in move sequence
              --attack-direction <0-7>      Send one Client.Attack after StartGame success
              --attack-magic <value>        Default: 0 (normal attack)
              --npc-call-object <objectId>   Send Client.NPCCall after StartGame success
              --magic-type <value>           Send Client.Magic with this MagicType
              --magic-direction <0-7>        Default: 0
              --magic-target <objectId>      Default: 0
              --magic-x <value>              Target X; defaults to current character X
              --magic-y <value>              Target Y; defaults to current character Y
              --pick-up                      Send one Client.PickUp after StartGame success
              --pick-up-type <0-255>         Default: 0
              --drop-and-pick-up             Drop one safe stacked inventory item and pick it back up
              --ipv6                        Force IPv6 TcpClient
              --no-hex                      Hide raw packet hex in logs
              --help                        Show this help
            """);
    }

    private static uint? ParseUInt(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!uint.TryParse(value, out uint result))
            throw new ArgumentException($"Invalid unsigned integer for --{name}: {value}");

        return result;
    }
    private static byte ParseByte(string? value, byte fallback, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        if (!byte.TryParse(value, out byte result))
            throw new ArgumentException($"Invalid byte for --{name}: {value}");

        return result;
    }

    private static int ParseInt(string? value, int fallback, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        if (!int.TryParse(value, out int result))
            throw new ArgumentException($"Invalid integer for --{name}: {value}");

        return result;
    }
}

