using OneOf;
using Types.Classes;

namespace Types.Interfaces;

public interface IStorageService
{
    public Task<OneOf<object, ErrorInfo>> UploadFileAsync(string folderName, string fileName, Stream stream);

    public Task<OneOf<object, ErrorInfo>> DownloadFileAsync(string folderName, string fileName, string localPath);
}