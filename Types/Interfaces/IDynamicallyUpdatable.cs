namespace Types.Interfaces;

public interface IDynamicallyUpdatable<T> where T: class
{
    public void UpdateSelfDynamically<T>(T otherObj);
}