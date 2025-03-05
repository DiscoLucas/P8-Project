using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    protected virtual void Awake() => Instance = this as T;
}


public abstract class SingletonPersistent<T> : Singleton<T> where T : MonoBehaviour
{
    
    protected override void Awake()
    {
        
        if (Instance != null)
        {
            
        }
        else
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }
    }



}
