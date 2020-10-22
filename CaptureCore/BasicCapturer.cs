using Composition.WindowsRuntimeHelpers;
using System;
using System.Drawing;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Capture;

namespace CaptureCore
{
    public class BasicCapturer : IDisposable
    {
        private readonly IDirect3DDevice device;
        public BasicCapture capture_core;

        public BasicCapturer()
        {
            device = Direct3D11Helper.CreateDevice();
        }

        public void StopCapture()
        {
            capture_core?.Dispose();
        }

        public void StartCaptureFromItem(GraphicsCaptureItem item)
        {
            StopCapture();
            capture_core = new BasicCapture(device, item);

            capture_core.StartCapture();
        }

        public Bitmap TryGetOneFrame()
        {
            return default;
        }

        public void Dispose()
        {
            StopCapture();
            device.Dispose();
        }
    }
}
