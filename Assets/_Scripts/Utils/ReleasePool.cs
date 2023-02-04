using System;
using System.Collections.Generic;

public class ReleasePool<T>
{
    private readonly List<T> _releasedObjects = new List<T>();

    private readonly Func<T> _newObject;

    public ReleasePool(Func<T> newObject)
    {
        _newObject = newObject;
    }

    public T Get()
    {
        if (_releasedObjects.Count > 0)
        {
            var obj = _releasedObjects[0];
            _releasedObjects.RemoveAt(0);
            return obj;
        }

        return _newObject.Invoke();
    }
    
    public void Release(T obj)
    {
        _releasedObjects.Add(obj);
    }
}
