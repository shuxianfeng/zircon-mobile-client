using System.IO;

namespace Zircon.Mobile.Core.Protocol
{
    public readonly struct ZirconMapChangedInfo
    {
        public ZirconMapChangedInfo(int mapIndex, int instanceIndex)
        {
            MapIndex = mapIndex;
            InstanceIndex = instanceIndex;
        }

        public int MapIndex { get; }
        public int InstanceIndex { get; }
    }

    public readonly struct ZirconUserLocationInfo
    {
        public ZirconUserLocationInfo(byte direction, ZirconMapPoint location)
        {
            Direction = direction;
            Location = location;
        }

        public byte Direction { get; }
        public ZirconMapPoint Location { get; }
    }

    public static class ZirconMapPacketDecoder
    {
        public static bool TryDecodeMapChanged(ZirconPacketFrame frame, out ZirconMapChangedInfo value)
        {
            value = default;
            if (frame.PacketId != ZirconPacketIds.Server.MapChanged ||
                (frame.Payload.Length != sizeof(int) && frame.Payload.Length != sizeof(int) * 2))
                return false;

            using (var stream = new MemoryStream(frame.Payload))
            using (var reader = new BinaryReader(stream))
            {
                int mapIndex = reader.ReadInt32();
                int instanceIndex = frame.Payload.Length == sizeof(int) * 2 ? reader.ReadInt32() : -1;
                value = new ZirconMapChangedInfo(mapIndex, instanceIndex);
                return true;
            }
        }

        public static bool TryDecodeUserLocation(ZirconPacketFrame frame, out ZirconUserLocationInfo value)
        {
            value = default;
            if (frame.PacketId != ZirconPacketIds.Server.UserLocation || frame.Payload.Length != 9)
                return false;

            using (var stream = new MemoryStream(frame.Payload))
            using (var reader = new BinaryReader(stream))
            {
                value = new ZirconUserLocationInfo(
                    reader.ReadByte(),
                    new ZirconMapPoint(reader.ReadInt32(), reader.ReadInt32()));
                return true;
            }
        }
    }
}
