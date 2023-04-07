using Dropbox.Api;
using Dropbox.Api.Files;
using OneOf;
using Types.Classes;
using Types.Interfaces;
using Codes = System.Net.HttpStatusCode;

namespace Services;

public class DropboxStorageService : IStorageService
{
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? AppKey { get; private set; }
    public string? AppSecret { get; private set; }
    private readonly DropboxClient _dropboxClient;
    private readonly ConfigurationManager _configManager;
    private const string BASE_ORDERS_DROPBOX_PATH = "/Orders";

    public DropboxStorageService(ConfigurationManager configManager)
    {
        _configManager = configManager;

        AppKey = configManager.GetSection("Storage:Dropbox:AppKey").Value;
        AppSecret = configManager.GetSection("Storage:Dropbox:AppSecret").Value;
        RefreshToken = configManager.GetSection("Storage:Dropbox:RefreshToken").Value;

        _dropboxClient = new DropboxClient(RefreshToken, AppKey, AppSecret);
    }

    public async Task<OneOf<object, ErrorInfo>> UploadFileAsync(string dropboxFolderName, string dropboxFileName, Stream stream)
    {
        try
        {
            FileMetadata uploadRes = await _dropboxClient.Files.UploadAsync($"{BASE_ORDERS_DROPBOX_PATH}/{dropboxFolderName}/{dropboxFileName}",
            WriteMode.Add.Instance,
            autorename: true,
            clientModified: null,
            mute: false,
            propertyGroups: null,
            strictConflict: true,
            contentHash: null,
            body: stream);

            return uploadRes;
        }
        catch (Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Ошибка DropboxAPI: {ex.Message}");
        }
    }

    public async Task<OneOf<object, ErrorInfo>> DeleteAsync(string path)
    {
        try
        {
            DeleteResult deleteResult = await _dropboxClient.Files.DeleteV2Async(path);
            return deleteResult;
        }
        catch (Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Ошибка DropboxAPI: {ex.Message}");
        }
    }

    public async Task<OneOf<object, ErrorInfo>> MoveAsync(string oldPath, string newPath)
    {
        try
        {
            RelocationResult relocationResult = await _dropboxClient.Files.MoveV2Async(
                new RelocationArg(
                    fromPath: oldPath,
                    toPath: newPath,
                    false,
                    autorename: true,
                    allowOwnershipTransfer: false
                )
            );

            return relocationResult;
        }
        catch (Exception ex)
        {
            return new ErrorInfo(Codes.NotFound, $"Ошибка DropboxAPI: {ex.Message}");
        }
    }

    public async Task<OneOf<object, ErrorInfo>> DownloadFileAsync(string dropboxFolderName, string dropboxFileName, string localFilePath)
    {
        using (var response = await _dropboxClient.Files.DownloadAsync($"/{dropboxFolderName}/{dropboxFileName}"))
        {
            var result = await response.GetContentAsStreamAsync();
            using (FileStream fs = File.Create(@$"{localFilePath}"))
            {
                result.CopyTo(fs);
            }
            return null;
        }
    }
}