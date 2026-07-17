using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Zircon.Mobile.Core.Protocol
{
    public static class ZirconBinary
    {
        public static byte[] EncodePacket(ushort packetId, Action<BinaryWriter> writePayload = null)
        {
            byte[] packet;

            using (var payloadStream = new MemoryStream())
            using (var writer = new BinaryWriter(payloadStream, Encoding.UTF8))
            {
                writer.Write(packetId);
                writePayload?.Invoke(writer);
                packet = payloadStream.ToArray();
            }

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(packet.Length + 4);
                writer.Write(packet);
                return stream.ToArray();
            }
        }

        public static bool TryReadFrame(List<byte> buffer, out ZirconPacketFrame frame)
        {
            frame = default;

            if (buffer == null || buffer.Count < 4)
                return false;

            int length = buffer[0] | buffer[1] << 8 | buffer[2] << 16 | buffer[3] << 24;

            if (length < 6)
                throw new InvalidDataException($"Invalid Zircon packet length: {length}.");

            if (buffer.Count < length)
                return false;

            byte[] raw = buffer.GetRange(0, length).ToArray();
            buffer.RemoveRange(0, length);

            ushort packetId = (ushort)(raw[4] | raw[5] << 8);
            byte[] payload = new byte[length - 6];
            Buffer.BlockCopy(raw, 6, payload, 0, payload.Length);

            frame = new ZirconPacketFrame(length, packetId, payload, raw);
            return true;
        }

        public static void WriteByteArray(BinaryWriter writer, byte[] value)
        {
            value ??= Array.Empty<byte>();
            writer.Write(value.Length);
            writer.Write(value);
        }

        public static byte[] Md5File(string path)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(path);
            return md5.ComputeHash(stream);
        }

        public static string Md5Text(string text)
        {
            using var md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));

            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
                builder.Append(value.ToString("x2"));

            return builder.ToString();
        }

        public static string ToHex(byte[] bytes, int maxBytes = 256)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            int count = Math.Min(bytes.Length, maxBytes);
            var builder = new StringBuilder(count * 3);

            for (int i = 0; i < count; i++)
            {
                if (i > 0) builder.Append(' ');
                builder.Append(bytes[i].ToString("X2"));
            }

            if (bytes.Length > maxBytes)
                builder.Append(" ...");

            return builder.ToString();
        }

        public static byte[] FromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Array.Empty<byte>();

            hex = hex.Replace(" ", string.Empty).Replace("-", string.Empty);

            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex input must contain an even number of digits.", nameof(hex));

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return bytes;
        }
    }
}
