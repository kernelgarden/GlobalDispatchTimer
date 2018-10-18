using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Global.Timer.Utils
{
    public static class Utils
    {
        public static void SafetyAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
            else
                Debugs.LogError("The key already exists");
        }
        
        public static void Execute(this System.Action action)
        {
            if (action == null) return;
            action();
        }

        public static void Execute<T>(this System.Action<T> action, T t)
        {
            if (action == null) return;
            action(t);
        }

        public static void Execute<T1, T2>(this System.Action<T1, T2> action, T1 t1, T2 t2)
        {
            if (action == null) return;
            action(t1, t2);
        }

        public static void Execute<T1, T2, T3>(this System.Action<T1, T2, T3> action, T1 t1, T2 t2, T3 t3)
        {
            if (action == null) return;
            action(t1, t2, t3);
        }

        public static void Execute<T1, T2, T3, T4>(this System.Action<T1, T2, T3, T4> action, T1 t1, T2 t2, T3 t3, T4 t4)
        {
            if (action == null) return;
            action(t1, t2, t3, t4);
        }
        
        public static void DisposeNullChk(this IDisposable self)
        {
            if (self == null) return;
            self.Dispose();
        }
    }
}