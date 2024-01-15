using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    private Poolable poolableObject;
    private int size = 20;

    private List<Poolable> availableObjects;

    private ObjectPool(Poolable _poolable, int _size)
    {
        this.poolableObject = _poolable;
        this.size = _size;
    }

    public static ObjectPool CreatePool(Poolable _poolable, int _size, string _poolName)
    {
        ObjectPool _pool = new ObjectPool(_poolable, _size);
        GameObject _poolRoot = new GameObject(_poolName);
        _pool.PopulatePool(_poolRoot.transform);
        return _pool;
    }

    public static ObjectPool CreatePool(Poolable _poolable, int _size, string _poolName, Transform _parent)
    {
        ObjectPool _pool = new ObjectPool(_poolable, _size);
        _pool.PopulatePool(_parent);
        return _pool;
    }

    void PopulatePool(Transform _parent)
    {
        availableObjects = new List<Poolable>();
        Poolable _temp = null;
        for (int i = 0; i < size; i++)
        {
            _temp = Instantiate(poolableObject, _parent);
            _temp.gameObject.SetActive(false);   
            _temp.pool = this;
            availableObjects.Add(_temp);
        }
    }

    public GameObject GetFromPool()
    {
        if (availableObjects.Count > 0)
        {
            Poolable _temp = availableObjects[0];
            _temp.gameObject.SetActive(true);
            availableObjects.RemoveAt(0);
            return _temp.gameObject;
        }
        return null;
    }

    public void GetFromPool(Vector3 _position, Quaternion _rotation)
    {
        if (availableObjects.Count > 0)
        {
            availableObjects[0].transform.position = _position;
            availableObjects[0].transform.rotation = _rotation;
            availableObjects[0].gameObject.SetActive(true);
            availableObjects.RemoveAt(0);
        }
    }

    public void AddToPool(Poolable _objectToAdd)
    {
        availableObjects.Add(_objectToAdd);
    }
}
