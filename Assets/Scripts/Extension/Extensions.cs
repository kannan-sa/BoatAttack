using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Extensions
{
    public static void DestroyChildren(this Transform transform)
    {
        for (int i = transform.childCount - 1; i > -1; i--)
            GameObject.Destroy(transform.GetChild(i).gameObject);
    }

    public static T[] Populate<T,I>(this Transform transform, T prefab, IEnumerable<I> items) where T : MonoBehaviour, IView<I>
    {
        List<T> newItems = new List<T>();
        foreach (var item in items)
        {
            T newItem = Object.Instantiate(prefab, transform, false);
            newItem.Initialize(item);
            newItems.Add(newItem);
        }
        return newItems.ToArray();
    }
}
