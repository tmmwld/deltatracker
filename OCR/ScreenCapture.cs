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
        public static Bitmap CaptureRegion(Rectangle region)
        {
            var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
            
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    region.Left,
                    region.Top,
                    0,
                    0,
                    region.Size,
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
