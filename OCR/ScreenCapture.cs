using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace DeltaForceTracker.OCR
{
    public static class ScreenCapture
    {
        // P/Invoke for DPI detection
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private const int LOGPIXELSX = 88;
        private const int LOGPIXELSY = 90;

        /// <summary>
        /// Get the current DPI scale factor of the primary screen.
        /// Returns 1.0 for 100% scaling, 1.25 for 125%, 1.5 for 150%, etc.
        /// </summary>
        public static double GetDpiScale()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            try
            {
                int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
                // 96 DPI = 100% scaling (standard)
                return dpiX / 96.0;
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        /// <summary>
        /// Convert WPF logical coordinates (device-independent pixels) to physical screen coordinates.
        /// This is necessary because CopyFromScreen uses physical pixels but WPF uses logical pixels.
        /// </summary>
        public static Rectangle ConvertToPhysicalPixels(Rectangle logicalRect)
        {
            double scale = GetDpiScale();
            return new Rectangle(
                (int)Math.Round(logicalRect.X * scale),
                (int)Math.Round(logicalRect.Y * scale),
                (int)Math.Round(logicalRect.Width * scale),
                (int)Math.Round(logicalRect.Height * scale)
            );
        }

        /// <summary>
        /// Capture a specific region of the screen.
        /// The input region should be in WPF logical coordinates.
        /// </summary>
        public static Bitmap CaptureRegion(Rectangle region)
        {
            // Convert logical coordinates to physical pixels for accurate capture
            Rectangle physicalRegion = ConvertToPhysicalPixels(region);
            
            var bitmap = new Bitmap(physicalRegion.Width, physicalRegion.Height, PixelFormat.Format32bppArgb);
            
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    physicalRegion.Left,
                    physicalRegion.Top,
                    0,
                    0,
                    physicalRegion.Size,
                    CopyPixelOperation.SourceCopy
                );
            }

            return bitmap;
        }

        public static Rectangle GetPrimaryScreenBounds()
        {
            return Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        }

        public static Bitmap CaptureFullScreen()
        {
            var bounds = GetPrimaryScreenBounds();
            return CaptureRegion(bounds);
        }
    }
}
