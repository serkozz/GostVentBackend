namespace Types.Interfaces;

public interface IDatabaseModelService<T> where T: class
{
    public T? Add(T obj);
    public T? Delete(T obj);
    public T? Update(T obj);
}

