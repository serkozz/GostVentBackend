using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Types.Classes;
using Services;

namespace Backend.Controllers;

[ApiController]
[Route("{controller}")]
public class FileStorageController : ControllerBase
{
    private readonly ILogger<DatabaseController> _logger;
    private readonly StorageServiceCollection _storageServiceCollection;

    public FileStorageController(ILogger<DatabaseController> logger, StorageServiceCollection storageServiceCollection)
    {
        _logger = logger;
        _storageServiceCollection = storageServiceCollection;
    }

    private DropboxStorageService? GetDropboxStorageService()
    {
        DropboxStorageService? dropboxStorageService = _storageServiceCollection.StorageServices.FirstOrDefault(service => service.GetType() == typeof(DropboxStorageService)) as DropboxStorageService;
        return dropboxStorageService;
    }


    [HttpPost("UploadImage")]
    [Route("/storage")]
    [Authorize()]
    // http://localhost:5072/storage
    public IResult? UploadFile()
    {
        IResult result = Results.Ok();
        return result;
    }
}