using System;
using UnityEngine;
using UnityEngine.UI;

[Flags]
public enum NavigationDirection { 
    Left = 1 << 0,
    Right = 1 << 1,
    Up = 1 << 2, 
    Down = 1 << 3,
}

public class NavigationAssigner : MonoBehaviour
{

    public Selectable target;

    public NavigationDirection direction;

    void OnEnable()
    {
        Selectable current = GetComponent<Selectable>();

        if (current == null)
            return;

        Navigation navigation = target.navigation;


        foreach (NavigationDirection item in Enum.GetValues(typeof(NavigationDirection)))
        {
            if ((direction & item) != item)
                continue;

            switch (item)
            {
                case NavigationDirection.Left:
                    navigation.selectOnLeft = current;
                    break;
                case NavigationDirection.Right:
                    navigation.selectOnRight = current;
                    break;
                case NavigationDirection.Up:
                    navigation.selectOnUp = current;
                    break;
                case NavigationDirection.Down:
                    navigation.selectOnDown = current;
                    break;
            }
        }

        target.navigation = navigation;
    }
}
