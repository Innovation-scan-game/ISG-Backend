using System.Drawing;
using System.Security.Cryptography;
using HttpMultipartParser;

namespace IsolatedFunctions.Helper;

public static class Helpers
{
    private static readonly MD5 Algo = MD5.Create();

    private const int MaxWidth = 512;

    public static string GenerateMd5Hash(Stream stream)
    {
        byte[] hash = Algo.ComputeHash(stream);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }

#pragma warning disable CA1416
    public static Stream ResizeImage(FilePart filePart)
    {
        Image image = Image.FromStream(filePart.Data);
        var stream = new MemoryStream();

        if (image.Width > MaxWidth)
        {
            var resized = image.GetThumbnailImage(MaxWidth, MaxWidth * image.Height / image.Width, null, IntPtr.Zero);
            resized.Save(stream, image.RawFormat);
        }
        else
        {
            image.Save(stream, image.RawFormat);
        }


        stream.Position = 0;
        return stream;
    }
#pragma warning restore CA1416
}
