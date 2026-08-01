using System;
using System.Collections.Generic;

namespace Zircon.Mobile.Game.World
{
    [Serializable]
    public sealed class ZirconMapManifest
    {
        public string Source;
        public int Width;
        public int Height;
        public int BlockingCells;
        public int NonEmptyCells;
        public int ViewX;
        public int ViewY;
        public int ViewWidth;
        public int ViewHeight;
        public List<ZirconMapCellManifest> SampleCells;
        // Runtime chunks keep floor/collision cells in SampleCells and can
        // provide a wider object-anchor envelope here. Tall 48xN map strips
        // extend upward from their owning cell, so their anchors may sit below
        // the logical floor chunk while pixels are still visible on screen.
        public List<ZirconMapCellManifest> ObjectCells;
    }

    [Serializable]
    public sealed class ZirconMapCellManifest
    {
        public int X;
        public int Y;
        public int BackFile;
        public int BackImage;
        public int MiddleFile;
        public int MiddleImage;
        public int FrontFile;
        public int FrontImage;
        public int MiddleAnimationFrame;
        public int FrontAnimationFrame;
        public int Light;
        public bool Blocking;
    }
}
