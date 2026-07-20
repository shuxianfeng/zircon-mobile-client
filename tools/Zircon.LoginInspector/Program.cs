using System.Net;
using System.Net.Sockets;
using Zircon.Mobile.Core.Protocol;

string email = Environment.GetEnvironmentVariable("ZIRCON_PROBE_EMAIL") ?? string.Empty;
string password = Environment.GetEnvironmentVariable("ZIRCON_PROBE_PASSWORD") ?? string.Empty;
if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
{
    Console.Error.WriteLine("Set ZIRCON_PROBE_EMAIL and ZIRCON_PROBE_PASSWORD.");
    return 2;
}

string host = args.Length > 0 ? args[0] : "zircon.35861344.xyz";
int port = args.Length > 1 && int.TryParse(args[1], out int parsedPort) ? parsedPort : 17000;
string passwordHash = ZirconBinary.Md5Text($"{email}-{password}");
string checksum = ZirconClientPackets.CreateChecksum();
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
using var client = new TcpClient(AddressFamily.InterNetworkV6);

IPAddress[] addresses = await Dns.GetHostAddressesAsync(host, timeout.Token);
IPAddress address = addresses.First(item => item.AddressFamily == AddressFamily.InterNetworkV6);
await client.ConnectAsync(address, port, timeout.Token);
using NetworkStream stream = client.GetStream();
var received = new List<byte>();

while (!timeout.IsCancellationRequested && client.Connected)
{
    byte[] chunk = new byte[8192];
    int read = await stream.ReadAsync(chunk, timeout.Token);
    if (read == 0) return 1;
    received.AddRange(chunk.Take(read));

    while (ZirconBinary.TryReadFrame(received, out ZirconPacketFrame frame))
    {
        byte[]? response = frame.PacketId switch
        {
            ZirconPacketIds.General.Connected => ZirconClientPackets.Connected(),
            ZirconPacketIds.General.Ping => ZirconClientPackets.Ping(),
            ZirconPacketIds.General.GoodVersion => BuildLogin(email, passwordHash, checksum),
            _ => null,
        };

        if (response != null)
            await stream.WriteAsync(response, timeout.Token);

        if (ZirconServerPacketDecoder.TryDecodeLogin(frame, out ZirconDecodedLogin login))
        {
            Console.WriteLine($"login={login.Result} characters={login.Characters.Count}");
            foreach (ZirconCharacterSelectInfo character in login.Characters)
            {
                Console.WriteLine(
                    $"character index={character.Index} name={character.Name} level={character.Level} " +
                    $"class={character.CharacterClass} gender={character.Gender} location={character.Location}");
            }
            return login.Result == ZirconLoginResult.Success ? 0 : 1;
        }
    }
}

return 1;

static byte[] BuildLogin(string email, string passwordHash, string checksum)
{
    var bytes = new List<byte>();
    bytes.AddRange(ZirconClientPackets.SelectLanguage("Chinese"));
    bytes.AddRange(ZirconClientPackets.Login(email, passwordHash, checksum));
    return bytes.ToArray();
}
