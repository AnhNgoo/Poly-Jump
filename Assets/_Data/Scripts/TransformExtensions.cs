using UnityEngine;

public static class TransformExtensions
{
    public static T FindDeepChild<T>(this Transform parent, string childName) where T : Object
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            if (child.name == childName)
            {
                if (typeof(T) == typeof(GameObject))
                    return child.gameObject as T;
                return child.GetComponent<T>();
            }
        }
        return null;
    }

    public static Transform FindDeepChild(this Transform parent, string childName)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            if (child.name == childName) return child;
        }
        return null;
    }
}
