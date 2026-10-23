using Avalonia.Headless.XUnit;
using FileLauncher.App;
using SkiaSharp;

namespace FileLauncher.Desktop.Tests;

public sealed class SafeBitmapLoaderFormatTests
{
    [AvaloniaFact]
    public void Supported_formats_decode_and_resize()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cpfl-skia-formats-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            using var bitmap = new SKBitmap(32, 16);
            bitmap.Erase(SKColors.OrangeRed);
            using var image = SKImage.FromBitmap(bitmap);
            foreach (var format in new[] { SKEncodedImageFormat.Png, SKEncodedImageFormat.Jpeg, SKEncodedImageFormat.Webp })
            {
                string path = Path.Combine(dir, format.ToString());
                using var encoded = image.Encode(format, 90);
                Assert.NotNull(encoded);
                File.WriteAllBytes(path, encoded.ToArray());
                using var decoded = SafeBitmapLoader.Load(path, 16);
                Assert.Equal(16, decoded.PixelSize.Width);
                Assert.Equal(8, decoded.PixelSize.Height);
            }
            string gif = Path.Combine(dir, "one.gif");
            File.WriteAllBytes(gif, Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"));
            using (var decoded = SafeBitmapLoader.Load(gif, 16)) Assert.Equal(1, decoded.PixelSize.Width);
            string bmp = Path.Combine(dir, "two.bmp");
            using (var writer = new BinaryWriter(File.Create(bmp)))
            {
                // BITMAPFILEHEADER and BITMAPINFOHEADER for a 2x2, 24-bit BMP.
                writer.Write((byte)'B');
                writer.Write((byte)'M');
                writer.Write(70);
                writer.Write(0);
                writer.Write(54);
                writer.Write(40);
                writer.Write(2);
                writer.Write(2);
                writer.Write((ushort)1);
                writer.Write((ushort)24);
                writer.Write(0);
                writer.Write(16);
                writer.Write(2835);
                writer.Write(2835);
                writer.Write(0);
                writer.Write(0);
                writer.Write(new byte[] { 0, 0, 255, 0, 0, 255, 0, 0, 0, 0, 255, 0, 0, 255, 0, 0 });
            }
            using (var decoded = SafeBitmapLoader.Load(bmp, 1)) Assert.Equal(1, decoded.PixelSize.Width);
            string ico = Path.Combine(dir, "png.ico");
            using (var png = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var writer = new BinaryWriter(File.Create(ico)))
            {
                byte[] bytes = png.ToArray();
                // One ICO directory entry containing the PNG payload.
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)1);
                writer.Write((byte)32);
                writer.Write((byte)16);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(bytes.Length);
                writer.Write(22);
                writer.Write(bytes);
            }
            using (var decoded = SafeBitmapLoader.Load(ico, 16)) Assert.Equal(16, decoded.PixelSize.Width);
        }
        finally { Directory.Delete(dir, true); }
    }
}
