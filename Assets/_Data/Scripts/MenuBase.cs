using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class MenuBase : LoadComponents
{
    [ShowInInspector] public abstract MenuType menuType { get; }
    public virtual void Open(object data = null)
    {
        gameObject.SetActive(true);
    }

    public virtual void Close()
    {
        gameObject.SetActive(false);
    }
}
