using System.Collections.Generic;

public class ObjectPool<T> : IObjectPool<T>
{
    private Stack<PoolableObject<T>> pool;

    public bool Contains(PoolableObject<T> poolableObject) => pool.Contains(poolableObject);

    public void Dispose(PoolableObject<T> disposedObject)
    {
        pool.Push(disposedObject);
    }
}