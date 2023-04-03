using Types.Interfaces;

namespace Services;

public class FileSystemStorageService : IStorageService
{

    public FileSystemStorageService()
    {
        
    }

    public async Task UploadFile(string folderName, string fileName, Stream stream)
    {
        throw new NotImplementedException();
    }

    public async Task DownloadFile(string folderName, string fileName, string localPath)
    {
        throw new NotImplementedException();
    }
}