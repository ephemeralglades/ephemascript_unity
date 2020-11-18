using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ephemascript
{
    public class Progress : MonoBehaviour
    {
        public static Progress S;

        /// <summary>
        /// Override me with a function that saves the `store` in a text file to save dialogue data!
        /// </summary>
        public virtual void Save()
        {
            Debug.Log("Saving Data");
        }

        public virtual void Awake()
        {
            if (S != null)
            {
                return;
            }
            S = this;
        }

        public Dictionary<string, string> store = new Dictionary<string, string>();
        public class SavableStoreValue<T>
        {
            public SavableStoreValue(string _storeKey, T _defaultValue)
            {
                defaultValue = _defaultValue;
                storeKey = _storeKey;
            }
            public T Get()
            {
                if (!Initialized)
                {
                    Debug.LogError("WARNING! Calling before initialized");
                }
                return parseFromStore(S.store[storeKey]);
            }
            public void Set(T to, bool saveAfter = true)
            {
                if (!Initialized)
                {
                    Debug.LogError("WARNING! Calling before initialized");
                }
                S.store[storeKey] = to.ToString();
                value = parseFromStore(S.store[storeKey]);
                if (saveAfter)
                    Progress.S.Save();
            }

            public bool Initialized { get; private set; } = false;
            public void Initialize(string _storeKey = null, T _defaultVal = default(T))
            {
                if (_storeKey != null)
                {
                    storeKey = _storeKey;
                    defaultValue = _defaultVal;
                }
                if (S.store.ContainsKey(storeKey))
                {
                    value = parseFromStore(S.store[storeKey]);
                }
                else
                {
                    S.store[storeKey] = defaultValue.ToString();
                    value = defaultValue;
                }
                Initialized = true;
            }

            #region private members
            private T value;
            private string storeKey;
            private T defaultValue;
            private T parseFromStore(string val)
            {
                if (typeof(T).IsEnum)
                {
                    return (T)Enum.Parse(typeof(T), val, true);
                }
                if (typeof(T).Equals(typeof(Vector3)))
                {
                    val = val.Substring(1, val.Length - 2);
                    string[] splitval = val.Split(',');
                    return (T)(object)(new Vector3(float.Parse(splitval[0]), float.Parse(splitval[1]), float.Parse(splitval[2])));
                }
                return (T)Convert.ChangeType(val, typeof(T));
            }
            #endregion
        }
    }

}
