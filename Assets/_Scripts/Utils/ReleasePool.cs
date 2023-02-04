using System;
using System.Collections.Generic;

public class ReleasePool<T>
{
    private readonly List<T> _releasedObjects = new List<T>();

    private readonly Func<T> _newObject;
    private readonly Action<T> _onAcquire;
    private readonly Action<T> _onRelease;

    public ReleasePool(Func<T> newObject, Action<T> onAcquire, Action<T> onRelease)
    {
        _newObject = newObject;
        _onAcquire = onAcquire;
        _onRelease = onRelease;
    }

    private T GetOrCreate()
    {
        if (_releasedObjects.Count > 0)
        {
            var obj = _releasedObjects[0];
            _releasedObjects.RemoveAt(0);
            return obj;
        }
        
        return _newObject.Invoke();
    }
    
    public T Get()
    {
        var obj = GetOrCreate();
        _onAcquire.Invoke(obj);
        return obj;
    }
    
    public void Release(T obj)
    {
        _onRelease.Invoke(obj);
        _releasedObjects.Add(obj);
    }
}
