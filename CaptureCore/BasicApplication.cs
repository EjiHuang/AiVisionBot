using Composition.WindowsRuntimeHelpers;
using System;
using System.Drawing;
using System.Numerics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;

namespace CaptureCore
{
    public class BasicApplication : IDisposable
    {
        private Compositor compositor;
        private readonly ContainerVisual root;

        private readonly SpriteVisual content;
        private readonly CompositionSurfaceBrush brush;

        private readonly IDirect3DDevice device;
        private BasicCapture capture;

        public BasicApplication(Compositor c)
        {
            compositor = c;
            device = Direct3D11Helper.CreateDevice();

            // Setup the root.
            root = compositor.CreateContainerVisual();
            root.RelativeSizeAdjustment = Vector2.One;

            // Setup the content.
            brush = compositor.CreateSurfaceBrush();    // 创建面画刷
            brush.HorizontalAlignmentRatio = 0.5f;
            brush.VerticalAlignmentRatio = 0.5f;
            brush.Stretch = CompositionStretch.Uniform;

            var shadow = compositor.CreateDropShadow();
            shadow.Mask = brush;

            content = compositor.CreateSpriteVisual();
            content.AnchorPoint = new Vector2(0.5f);
            content.RelativeOffsetAdjustment = new Vector3(0.5f, 0.5f, 0);
            content.RelativeSizeAdjustment = Vector2.One;
            content.Size = new Vector2(-20, -20);
            content.Brush = brush;
            content.Shadow = shadow;

            root.Children.InsertAtTop(content);
        }

        public Visual Visual => root;

        public void StopCapture()
        {
            capture?.Dispose();
            brush.Surface = null;
        }

        public void StartCaptureFromItem(GraphicsCaptureItem item)
        {
            StopCapture();
            capture = new BasicCapture(device, item);

            var surface = capture.CreateSurface(compositor);
            brush.Surface = surface;

            capture.StartCapture();
        }

        //public Bitmap TryGetOneFrame()
        //{
        //    return capture.CurrentFrameBitmap;
        //}

        public void Dispose()
        {
            StopCapture();
            compositor = null;
            root.Dispose();
            content.Dispose();
            brush.Dispose();
            device.Dispose();
        }
    }
}
