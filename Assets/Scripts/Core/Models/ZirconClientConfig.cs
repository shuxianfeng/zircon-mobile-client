namespace Zircon.Mobile.Core.Models
{
    public sealed class ZirconClientConfig
    {
        public string Host { get; set; } = "zircon.35861344.xyz";
        public int Port { get; set; } = 17000;
        public string Language { get; set; } = "Chinese";
        public string Checksum { get; set; } = "MobileProbeCheck2026";
        public bool PreferIpv6 { get; set; } = true;
        public int ReceiveBufferSize { get; set; } = 8192;
    }
}
