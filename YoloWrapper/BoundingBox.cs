using System.Runtime.InteropServices;

namespace YoloWrapper
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BoundingBox
    {
        public uint x, y, w, h;
        public float prob;
        public uint obj_id;
        public uint track_id;
        public uint frames_counter;
        public float x_3d, y_3d, z_3d;
    }
}
