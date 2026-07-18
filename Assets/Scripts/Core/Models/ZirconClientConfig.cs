namespace Zircon.Mobile.Core.Models
{
    public sealed class ZirconClientConfig
    {
        public string Host { get; set; } = "192.168.0.100";
        public int Port { get; set; } = 17000;
        public string Language { get; set; } = "Chinese";
        public string Checksum { get; set; } = "MobileProbeCheck2026";
        public bool PreferIpv6 { get; set; } = false;
        public int ReceiveBufferSize { get; set; } = 8192;
    }
}
