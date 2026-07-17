using System;

namespace Zircon.Mobile.Core.Protocol
{
    public readonly struct ZirconPacketFrame
    {
        public ZirconPacketFrame(int length, ushort packetId, byte[] payload, byte[] rawBytes)
        {
            Length = length;
            PacketId = packetId;
            Payload = payload ?? Array.Empty<byte>();
            RawBytes = rawBytes ?? Array.Empty<byte>();
        }

        public int Length { get; }
        public ushort PacketId { get; }
        public byte[] Payload { get; }
        public byte[] RawBytes { get; }
    }
}
