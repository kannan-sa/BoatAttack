using UnityEngine;

public interface IView<T>
{
    void Initialize(T model);
}
