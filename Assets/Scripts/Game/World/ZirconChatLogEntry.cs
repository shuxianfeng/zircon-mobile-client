using System;

namespace Zircon.Mobile.Game.World
{
    public sealed class ZirconChatLogEntry
    {
        public ZirconChatLogEntry(uint objectId, int messageType, string text, DateTime receivedUtc)
        {
            ObjectId = objectId;
            MessageType = messageType;
            Text = text ?? string.Empty;
            ReceivedUtc = receivedUtc;
        }

        public uint ObjectId { get; }
        public int MessageType { get; }
        public string Text { get; }
        public DateTime ReceivedUtc { get; }
    }
}
