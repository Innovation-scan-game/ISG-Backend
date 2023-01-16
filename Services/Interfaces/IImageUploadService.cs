using HttpMultipartParser;

namespace Services.Interfaces;

public interface IImageUploadService
{
    Task<string> UploadImage(FilePart file, Enums.BlobContainerName imageContainerName);
}