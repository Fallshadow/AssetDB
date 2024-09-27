using System;
using System.Collections.Generic;
using UnityEngine;

public static class HandlerManager {

    private static readonly Dictionary<Type, BaseHandler> dictHandlers = new Dictionary<Type, BaseHandler>();

    public static void Initialize() {
        addHandler<LoginHandler>();
    }

    public static void Release() {
        foreach (KeyValuePair<Type, BaseHandler> item in dictHandlers) {
            item.Value.Release();
        }

        dictHandlers.Clear();
    }

    private static void addHandler<T>() where T : BaseHandler {
        Debug.Assert(dictHandlers.ContainsKey(typeof(T)) == false);
        BaseHandler handler = (T)Activator.CreateInstance(typeof(T));
        handler.Init();
        dictHandlers[typeof(T)] = handler;
    }
}