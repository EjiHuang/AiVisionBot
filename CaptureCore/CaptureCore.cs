using Composition.WindowsRuntimeHelpers;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;
using SharpDX.WIC;
using WICBitmap = SharpDX.WIC.Bitmap;
using D2D1PixelFormat = SharpDX.Direct2D1.PixelFormat;
using Bitmap = System.Drawing.Bitmap;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace CaptureCore
{
    public class BasicCapture : IDisposable
    {
        private readonly GraphicsCaptureItem item;
        private readonly Direct3D11CaptureFramePool framePool;
        private readonly GraphicsCaptureSession session;
        private SizeInt32 lastSize;
        private readonly ImagingFactory wicFactory;

        private readonly IDirect3DDevice device;
        private readonly Device d3dDevice;
        private readonly SharpDX.DXGI.SwapChain1 swapChain;

        public delegate void GetOneFrameToBitmapEventHandler(Bitmap frame);
        public GetOneFrameToBitmapEventHandler GetOneFrameFromBitmapEvent;

        public BasicCapture(IDirect3DDevice d, GraphicsCaptureItem i)
        {
            item = i;
            device = d;
            d3dDevice = Direct3D11Helper.CreateSharpDXDevice(device);
            wicFactory = new ImagingFactory();

            // 建立DirectX图形基础设施工厂（DirectX Graphics Infrastructure，DXGI）
            var dxgiFactory = new SharpDX.DXGI.Factory2();

            // 建立交换链
            var description = new SharpDX.DXGI.SwapChainDescription1()
            {
                Width = item.Size.Width,
                Height = item.Size.Height,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Stereo = false,   // 立体声
                SampleDescription = new SharpDX.DXGI.SampleDescription()
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                BufferCount = 2,
                Scaling = SharpDX.DXGI.Scaling.Stretch,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,    // 连续翻转
                AlphaMode = SharpDX.DXGI.AlphaMode.Premultiplied,       // 预乘
                Flags = SharpDX.DXGI.SwapChainFlags.None
            };
            swapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory, d3dDevice, ref description);

            framePool = Direct3D11CaptureFramePool.Create(
                device,
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                i.Size);

            // 创建捕获会话
            session = framePool.CreateCaptureSession(i);
            // 记录最后帧尺寸
            lastSize = i.Size;

            // 帧已到达事件添加
            framePool.FrameArrived += OnFrameArrived;
        }

        public void Dispose()
        {
            session?.Dispose();
            framePool?.Dispose();
            swapChain?.Dispose();
            d3dDevice?.Dispose();
            wicFactory?.Dispose();
        }

        public void StartCapture()
        {
            session.StartCapture();
        }

        /// <summary>
        /// 为交换链创建合成曲面
        /// </summary>
        /// <param name="compositor"></param>
        /// <returns></returns>
        public ICompositionSurface CreateSurface(Compositor compositor)
        {
            return compositor.CreateCompositionSurfaceForSwapChain(swapChain);
        }

        /// <summary>
        /// 帧到达事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            var newSize = false;

            using Direct3D11CaptureFrame frame = sender.TryGetNextFrame();

            if (frame == null) return;

            if (frame.ContentSize.Width != lastSize.Width || frame.ContentSize.Height != lastSize.Height)
            {
                // 我们捕捉到的东西变大了。
                // 我们需要先调整交换链的大小，然后blit像素。
                // 完成此操作后，请注销帧，然后重新创建帧池。
                newSize = true;
                lastSize = frame.ContentSize;
                swapChain.ResizeBuffers(
                    2,
                    lastSize.Width,
                    lastSize.Height,
                    SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    SharpDX.DXGI.SwapChainFlags.None);
            }

            using Texture2D backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
            using Texture2D tex = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface);
            d3dDevice.ImmediateContext.CopyResource(tex, backBuffer);
            // 保存当前帧到位图
            if (GetOneFrameFromBitmapEvent != null)
            {
                TryGetOneFrameToBitmap(tex);
            }
            // GetOneFrameToBitmap(tex);

            swapChain.Present(0, SharpDX.DXGI.PresentFlags.None);

            if (newSize)
            {
                framePool.Recreate(device, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, lastSize);
            }
        }

        public void TryGetOneFrameToBitmap(Texture2D tex)
        {
            using Texture2D copy = new Texture2D(d3dDevice, new Texture2DDescription
            {
                Height = tex.Description.Height,
                Width = tex.Description.Width,
                MipLevels = 1,
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Usage = ResourceUsage.Staging,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            });
            d3dDevice.ImmediateContext.CopyResource(tex, copy);

            var dataBox = d3dDevice.ImmediateContext.MapSubresource(copy, 0, MapMode.Read, MapFlags.None, out DataStream dataStream);

            #region wic version

            var rect = new DataRectangle
            {
                DataPointer = dataStream.DataPointer,
                Pitch = dataBox.RowPitch
            };

            using var wicBitmap = new WICBitmap(wicFactory,
                copy.Description.Width,
                copy.Description.Height,
                SharpDX.WIC.PixelFormat.Format32bppPBGRA,
                rect);

            var width = wicBitmap.Size.Width;
            var height = wicBitmap.Size.Height;
            var gdiBitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);

            try
            {
                var gdiBitmapData = gdiBitmap.LockBits(
                    new Rectangle(0, 0, gdiBitmap.Width, gdiBitmap.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppPArgb);

                wicBitmap.CopyPixels(gdiBitmapData.Stride, gdiBitmapData.Scan0, gdiBitmapData.Height * gdiBitmapData.Stride);

                gdiBitmap.UnlockBits(gdiBitmapData);

                // 回调
                GetOneFrameFromBitmapEvent.Invoke(gdiBitmap);
            }
            finally
            {
                d3dDevice.ImmediateContext.UnmapSubresource(copy, 0);
                dataStream.Dispose();
            }

            #endregion

            //var width = copy.Description.Width;
            //var height = copy.Description.Height;
            //var gdiBitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            //try
            //{
            //    BitmapData bitmapData = gdiBitmap.LockBits(new Rectangle(0, 0, tex.Description.Width, tex.Description.Height),
            //        ImageLockMode.WriteOnly, gdiBitmap.PixelFormat);
            //    try
            //    {
            //        var srcPtr = dataBox.DataPointer;
            //        var dstPtr = bitmapData.Scan0;

            //        for (var y = 0; y < gdiBitmap.Height; y++)
            //        {
            //            memcpy(dstPtr, srcPtr, new UIntPtr((uint)(gdiBitmap.Width * 4)));
            //            srcPtr = IntPtr.Add(srcPtr, dataBox.RowPitch);
            //            dstPtr = IntPtr.Add(dstPtr, bitmapData.Stride);
            //        }
            //    }
            //    finally
            //    {
            //        gdiBitmap.UnlockBits(bitmapData);
            //    }
            //}
            //finally
            //{
            //    d3dDevice.ImmediateContext.UnmapSubresource(copy, 0);
            //}

            //// 回调
            //GetOneFrameFromBitmapEvent.Invoke(gdiBitmap);
        }

        public async Task<Bitmap> TryGetOneFrameAsync()
        {
            bool captureDone = false;
            Texture2D tex;
            Bitmap result = null;

            await Task.Run(() =>
            {
                for (int i = 0; !captureDone; i++)
                {
                    try
                    {
                        if (framePool.TryGetNextFrame() == null)
                            continue;

                        tex = Direct3D11Helper.CreateSharpDXTexture2D(framePool.TryGetNextFrame().Surface);
                        result = new Bitmap(tex.Description.Width, tex.Description.Height, PixelFormat.Format32bppArgb);

                        using Texture2D copy = new Texture2D(d3dDevice, new Texture2DDescription
                        {
                            Height = tex.Description.Height,
                            Width = tex.Description.Width,
                            MipLevels = 1,
                            ArraySize = 1,
                            Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                            Usage = ResourceUsage.Staging,
                            SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                            BindFlags = BindFlags.None,
                            CpuAccessFlags = CpuAccessFlags.Read,
                            OptionFlags = ResourceOptionFlags.None
                        });
                        d3dDevice.ImmediateContext.CopyResource(tex, copy);

                        var dataBox = d3dDevice.ImmediateContext.MapSubresource(copy, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                        try
                        {
                            BitmapData bitmapData = result.LockBits(new Rectangle(0, 0, tex.Description.Width, tex.Description.Height),
                                ImageLockMode.WriteOnly, result.PixelFormat);
                            try
                            {
                                var srcPtr = dataBox.DataPointer;
                                var dstPtr = bitmapData.Scan0;

                                for (var y = 0; y < result.Height; y++)
                                {
                                    memcpy(dstPtr, srcPtr, new UIntPtr((uint)(result.Width * 4)));
                                    srcPtr = IntPtr.Add(srcPtr, dataBox.RowPitch);
                                    dstPtr = IntPtr.Add(dstPtr, bitmapData.Stride);
                                }
                            }
                            finally
                            {
                                result.UnlockBits(bitmapData);
                            }
                        }
                        finally
                        {
                            d3dDevice.ImmediateContext.UnmapSubresource(copy, 0);
                        }

                        captureDone = true;

                    }
                    catch (SharpDX.SharpDXException e)
                    {
                        if (e.ResultCode.Code != SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                        {
                            throw e;
                        }
                    }
                }
            });

            return result;
        }
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);
    }
}
