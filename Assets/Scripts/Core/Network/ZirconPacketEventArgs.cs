using System;
using Zircon.Mobile.Core.Protocol;

namespace Zircon.Mobile.Core.Network
{
    public sealed class ZirconPacketEventArgs : EventArgs
    {
        public ZirconPacketEventArgs(ZirconPacketFrame frame)
        {
            Frame = frame;
        }

        public ZirconPacketFrame Frame { get; }
    }
}
