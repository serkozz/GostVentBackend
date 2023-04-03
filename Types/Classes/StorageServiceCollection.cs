using Types.Interfaces;

namespace Types.Classes;
public class StorageServiceCollection
{
    public List<IStorageService> StorageServices { get; }

    public StorageServiceCollection()
    {
        StorageServices = new List<IStorageService>();
    }

    public bool TryAddService(IStorageService storageService)
    {
        if (StorageServices.Contains(storageService))
            return false;
        StorageServices.Add(storageService);
        return true;
    }

    public bool TryRemoveService(IStorageService storageService)
    {
        if (!StorageServices.Contains(storageService))
            return false;
        StorageServices.Remove(storageService);
        return true;
    }

    public IStorageService? TryGetService(Type serviceType)
    {
        return StorageServices.FirstOrDefault(service => service.GetType() == serviceType);
    }
}