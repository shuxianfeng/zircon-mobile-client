using System;
using System.IO;
using System.Text;

namespace Zircon.Mobile.Core.Protocol
{
    public readonly struct ZirconLoginResponse
    {
        public ZirconLoginResponse(
            ZirconLoginResult result,
            string message,
            TimeSpan duration,
            int? characterCount)
        {
            Result = result;
            Message = message;
            Duration = duration;
            CharacterCount = characterCount;
        }

        public ZirconLoginResult Result { get; }
        public string Message { get; }
        public TimeSpan Duration { get; }
        public int? CharacterCount { get; }
    }

    public readonly struct ZirconDisconnectResponse
    {
        public ZirconDisconnectResponse(ZirconDisconnectReason reason)
        {
            Reason = reason;
        }

        public ZirconDisconnectReason Reason { get; }
    }

    public static class ZirconServerPackets
    {
        public static bool TryDecodeLogin(ZirconPacketFrame frame, out ZirconLoginResponse response)
        {
            response = default;

            if (frame.PacketId != ZirconPacketIds.Server.Login &&
                frame.PacketId != ZirconPacketIds.Server.LoginSimple)
                return false;

            try
            {
                using (var stream = new MemoryStream(frame.Payload))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    var result = (ZirconLoginResult)reader.ReadByte();
                    string message = ReadStringIfAvailable(reader);
                    TimeSpan duration = ReadTimeSpanIfAvailable(reader);
                    int? characterCount = ReadInt32IfAvailable(reader);

                    response = new ZirconLoginResponse(result, message, duration, characterCount);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool TryDecodeDisconnect(ZirconPacketFrame frame, out ZirconDisconnectResponse response)
        {
            response = default;

            if (frame.PacketId != ZirconPacketIds.General.Disconnect || frame.Payload.Length < 1)
                return false;

            response = new ZirconDisconnectResponse((ZirconDisconnectReason)frame.Payload[0]);
            return true;
        }

        private static string ReadStringIfAvailable(BinaryReader reader)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length)
                return string.Empty;

            return reader.ReadString();
        }

        private static TimeSpan ReadTimeSpanIfAvailable(BinaryReader reader)
        {
            long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
            if (remaining < sizeof(long))
                return TimeSpan.Zero;

            return TimeSpan.FromTicks(reader.ReadInt64());
        }

        private static int? ReadInt32IfAvailable(BinaryReader reader)
        {
            long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
            if (remaining < sizeof(int))
                return null;

            return reader.ReadInt32();
        }
    }
}
