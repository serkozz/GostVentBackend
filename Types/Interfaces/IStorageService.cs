namespace Types.Interfaces;

public interface IStorageService
{
    public Task UploadFile(string folderName, string fileName, Stream stream);

    public Task DownloadFile(string folderName, string fileName, string localPath);
}