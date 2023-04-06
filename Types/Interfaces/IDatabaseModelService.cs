using OneOf;
using Types.Classes;

namespace Types.Interfaces;

public interface IDatabaseModelService<T> where T: class
{
    public OneOf<T,ErrorInfo> Add(T obj);
    public OneOf<T,ErrorInfo> Delete(T obj);
    public OneOf<T,ErrorInfo> Update(T obj);
}

