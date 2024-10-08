using System;
using UnityEngine;

public class Singleton<T> where T : Singleton<T> {
    protected static T s_instance;

    public static T instance {
        get {
            if (s_instance == null) {
                s_instance = Activator.CreateInstance<T>();
                s_instance.init();
            }
            return s_instance;
        }
    }

    protected virtual void init() { }
}

public abstract class SingletonMonoBehavior<T> : MonoBehaviour where T : SingletonMonoBehavior<T> {
    public static T instance => s_instance;

    private static T s_instance = null;
    private static int instance_count = 0;

    #region YJX.VFX
#if UNITY_EDITOR
    public void SimulateAwakeInEditor() {
        Awake();
    }
#endif
    #endregion

    protected virtual void Awake() {
        if (s_instance == null) {
            s_instance = this as T;
            s_instance.init();
        }
        else {
            if (Application.isPlaying) {
                Destroy(this);
            }
        }

        ++instance_count;
    }

    protected virtual void OnDestroy() {
        --instance_count;
        if (instance_count == 0) {
            s_instance = null;
        }
    }

    public void ForceNull() {
        s_instance = null;
    }

    protected virtual void init() { }
}

public abstract class SingletonMonoBehaviorNoDestroy<T> : MonoBehaviour where T : SingletonMonoBehaviorNoDestroy<T> {
    public static T instance => s_instance;

    private static T s_instance = null;

    protected virtual void Awake() {
        if (s_instance == null) {
            DontDestroyOnLoad(gameObject);
            s_instance = this as T;
            init();
        }
        else {
            Destroy(this);
        }
    }

    protected virtual void init() { }

    public static void ReleaseInstance() {
        if (s_instance != null) {
            Destroy(s_instance);
            s_instance = null;
        }
    }
}

public abstract class SingletonMonoBehaviorAutoCreate<T> : MonoBehaviour where T : SingletonMonoBehaviorAutoCreate<T> {
    private static T s_instance = null;

    public static T instance {
        get {
            if (s_instance != null) {
                return s_instance;
            }

            CreateInstance();
            return s_instance;
        }
    }

    public static void CreateInstance() {
        if (s_instance != null) {
            return;
        }

        var type = typeof(T);
        var objList = GameObject.FindObjectsOfType(type) as GameObject[];
        if (objList != null && objList.Length > 0) {
            Debug.Log("You have more than one " + type +
                                      " in the scene. You only need 1, it's a singleton!");

            foreach (var obj in objList) {
                GameObject.Destroy(obj);
            }
        }

        s_instance = new GameObject(type.Name.ToString()).AddComponent<T>();

    }

    public static void ReleaseInstance() {
        if (s_instance != null) {
            Destroy(s_instance.gameObject);
            s_instance = null;
        }
    }

    protected virtual void OnDestroy() {
        s_instance = null;
    }
}

public abstract class SingletonMonoBehaviorAutoCreateNoDestroy<T> : MonoBehaviour where T : SingletonMonoBehaviorAutoCreateNoDestroy<T> {
    private static T s_instance = null;

    public static T instance {
        get {
            if (s_instance != null) {
                return s_instance;
            }

            CreateInstance();
            return s_instance;
        }
    }

    protected virtual void Awake() {
        DontDestroyOnLoad(gameObject);
        if (s_instance == null) {
            s_instance = this as T;
            init();
        }
        else {
            Destroy(this);
        }
    }

    protected virtual void init() { }

    public static void CreateInstance() {
        if (s_instance != null) {
            return;
        }

        GameObject singletonObject = SingletonGameObject.getObject();
        if (singletonObject == null) {
            return;
        }

        DontDestroyOnLoad(singletonObject);

        T[] objList = GameObject.FindObjectsOfType(typeof(T)) as T[];
        if (objList.Length == 0) {
            singletonObject.AddComponent<T>();
        }
        else if (objList.Length > 1) {
            Debug.Log("You have more than one " + typeof(T).Name + " in the scene. You only need 1, it's a singleton!");
            foreach (T item in objList) {
                Destroy(item);
            }
        }
    }

    public static void ReleaseInstance() {
        if (s_instance != null) {
            Destroy(s_instance);
            s_instance = null;
        }
    }
}

class SingletonGameObject {
    const string s_objName = "SingletonObject";
    static GameObject s_SingletonObject = null;

    public static GameObject getObject() {
        if (s_SingletonObject == null) {
            s_SingletonObject = GameObject.Find(s_objName);
            if (s_SingletonObject == null) {
                Debug.Log("CreateInstance");
                s_SingletonObject = new GameObject(s_objName);
            }
        }
        return s_SingletonObject;
    }

    public static void destroyObject() {
        if (s_SingletonObject != null) {
            GameObject.DestroyImmediate(s_SingletonObject);
            s_SingletonObject = null;
        }
    }
}