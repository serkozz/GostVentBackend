using Dropbox.Api;
using Dropbox.Api.Files;
using Types.Interfaces;

namespace Services;

public class DropboxStorageService : IStorageService
{
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? AppKey { get; private set; }
    public string? AppSecret { get; private set; }
    private readonly DropboxClient _dropboxClient;
    private readonly ConfigurationManager _configManager;

    public DropboxStorageService(ConfigurationManager configManager)
    {
        _configManager = configManager;

        AppKey = configManager.GetSection("Storage:Dropbox:AppKey").Value;
        AppSecret = configManager.GetSection("Storage:Dropbox:AppSecret").Value;
        RefreshToken = configManager.GetSection("Storage:Dropbox:RefreshToken").Value;

        _dropboxClient = new DropboxClient(RefreshToken, AppKey, AppSecret);
    }

    public async Task UploadFile(string dropboxFolderName, string dropboxFileName, Stream stream)
    {
        var res = await _dropboxClient.Files.UploadAsync($"/{dropboxFolderName}/{dropboxFileName}",
        WriteMode.Add.Instance,
        autorename: true,
        clientModified: null,
        mute: false,
        propertyGroups: null,
        strictConflict: true,
        contentHash: null,
        body: stream);
    }

    public async Task DownloadFile(string dropboxFolderName, string dropboxFileName, string localFilePath)
    {
        using (var response = await _dropboxClient.Files.DownloadAsync($"/{dropboxFolderName}/{dropboxFileName}"))
        {
            var result = await response.GetContentAsStreamAsync();
            using (FileStream fs = File.Create(@$"{localFilePath}"))
            {
                result.CopyTo(fs);
            }
        }
    }
}