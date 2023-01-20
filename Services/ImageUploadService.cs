using System.Security.Cryptography;
using HttpMultipartParser;
using Services.Interfaces;
using System.Drawing;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Services;

public class ImageUploadService : IImageUploadService
{
    private static readonly MD5 Algo = MD5.Create();
    private readonly BlobServiceClient _blobServiceClient;

    private const int MaxWidth = 512;

    public async Task<string> UploadImage(FilePart file, Enums.BlobContainerName imageContainerName)
    {
        if (!IsContentTypeAllowed(file.ContentType))
            throw new ArgumentException("Invalid image file type!");
        if (!IsContentSizeAppropriate(file.Data))
            throw new ArgumentException("File size must be between 1 KByte and 5 MB!");

        string extension = file.ContentType is "image/png" ? ".png" : ".jpg";

        Stream stream = ResizeImage(file);
        string md5 = GenerateMd5Hash(stream);


        var blobContainterClient = _blobServiceClient.GetBlobContainerClient(imageContainerName.ToString());

        BlobClient blob = blobContainterClient.GetBlobClient(md5 + extension);
        stream.Position = 0;
        await blob.UploadAsync(stream, new BlobHttpHeaders {ContentType = file.ContentType});
        return blob.Uri.ToString();
    }

    private static bool IsContentSizeAppropriate(Stream data)
    {
        return data.Length is > 1024 and < 1024 * 1024 * 5;
    }


    private static bool IsContentTypeAllowed(string contentType)
    {
        string[] allowedContent = {"image/png", "image/jpeg"};
        return allowedContent.Contains(contentType);
    }

    private static string GenerateMd5Hash(Stream stream)
    {
        byte[] hash = Algo.ComputeHash(stream);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }

    private static Stream ResizeImage(FilePart filePart)
    {
        Image image = Image.FromStream(filePart.Data);
        var stream = new MemoryStream();

        if (image.Width > MaxWidth)
        {
            var resized = image.GetThumbnailImage(MaxWidth, MaxWidth * image.Height / image.Width, null, IntPtr.Zero);
        }
        else
        {
            image.Save(stream, image.RawFormat);
        }

        stream.Position = 0;
        return stream;
    }

    //constructor
    public ImageUploadService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }
}
