using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Rebar.Unity.Editor
{
    [Serializable]
    public class EditorState
    {
        [Serializable]
        private class Pair<T>
        {
            public string Key;
            public T Value;

            public static implicit operator KeyValuePair<string, T>(Pair<T> t) =>
                new KeyValuePair<string, T>(t.Key, t.Value);

            public static implicit operator Pair<T> (KeyValuePair<string, T> kvp) =>
                new Pair<T> 
                {
                    Key = kvp.Key,
                    Value = kvp.Value
                };
        }

        private class PropertyImpl<T> : Property<T>
        {
            public PropertyImpl(EditorState state, string key) : base(state, key) { }
        }

        public abstract class Property<T> 
        {
            private readonly EditorState _state;
            private readonly string _key;

            public bool Exists => _state.PropertyExists<T>(_key);
            public T Value 
            {
                get => Exists ? _state.GetProperty<T>(_key) : throw new InvalidOperationException($"Property {_key} doesn't exist anymore");
                set => _state.SetProperty<T>(_key, value);
            }

            protected Property(EditorState state, string key)
            {
                _state = state;
                _key = key;
            }

            public bool Remove() => _state.Remove<T>(_key);

            public static implicit operator T(Property<T> property) => 
                property.Exists ? property.Value : default(T);
        }

        private const string EDITOR_STATES_FOLDER = "EditorStates";

        public static EditorState LoadOrCreateFor(UnityEngine.Object obj)
        {
            EditorState state = new EditorState(obj.GetInstanceID());
            string name = state.FileName;
            if (File.Exists(name)) 
            {
                EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(name), state);

            }
            else state.Save();
            return state;
        }

        [SerializeField]
        private int _instanceId = 0;
        [SerializeField]
        private List<Pair<bool>> _bools = new List<Pair<bool>>();
        [SerializeField]
        private List<Pair<int>> _enums = new List<Pair<int>>();
        [SerializeField]
        private List<Pair<int>> _ints = new List<Pair<int>>();
        [SerializeField]
        private List<Pair<float>> _floats = new List<Pair<float>>();
        [SerializeField]
        private List<Pair<string>> _strings = new List<Pair<string>>();
        [SerializeField]
        private List<Pair<UnityEngine.Object>> _objects = new List<Pair<UnityEngine.Object>>();

        private Dictionary<string, bool> _boolStates = null;
        private Dictionary<string, string> _stringStates = null;
        private Dictionary<string, int> _enumStates = null;
        private Dictionary<string, int> _intStates = null;
        private Dictionary<string, float> _floatStates = null;
        private Dictionary<string, UnityEngine.Object> _objectStates = null;

        private int InstanceId => _instanceId;
        private string FileName => $"{Application.persistentDataPath}/{EDITOR_STATES_FOLDER}/{InstanceId}.json";

        private EditorState(int instanceId) => _instanceId = instanceId;

        private void InitSingleDict<T>(ref List<Pair<T>> list, ref Dictionary<string, T> dict)
        {
            dict = list.ToDictionary(p => p.Key, p => p.Value);
            list.Clear();
        }

        private void FillSingleList<T>(ref List<Pair<T>> list, ref Dictionary<string, T> dict)
        {
            foreach (var kvp in dict) 
                list.Add(kvp);
        }

        private void InitDictionaries()
        {
            if (_boolStates != null) return;
            InitSingleDict(ref _bools, ref _boolStates);
            InitSingleDict(ref _enums, ref _enumStates);
            InitSingleDict(ref _ints, ref _intStates);
            InitSingleDict(ref _floats, ref _floatStates);
            InitSingleDict(ref _strings, ref _stringStates);
            InitSingleDict(ref _objects, ref _objectStates);
        } 

        private void Save()
        {
            InitDictionaries();
            
            FillSingleList(ref _bools, ref _boolStates);
            FillSingleList(ref _enums, ref _enumStates);
            FillSingleList(ref _ints, ref _intStates);
            FillSingleList(ref _floats, ref _floatStates);
            FillSingleList(ref _strings, ref _stringStates);
            FillSingleList(ref _objects, ref _objectStates);

            string name = FileName;
            Directory.CreateDirectory(Directory.GetParent(name).FullName);
            File.WriteAllText(name, EditorJsonUtility.ToJson(this, true));

            _bools.Clear();
            _enums.Clear();
            _ints.Clear();
            _floats.Clear();
            _strings.Clear();
            _objects.Clear();
        }

        private string BuildActualKey<T>(string key) => $"{key}__{typeof(T).Name}";

        public void SetProperty<T>(string key, T value) 
        {
            InitDictionaries();
            Type typeOfT = typeof(T);
            key = BuildActualKey<T>(key);
            if (typeOfT == typeof(bool))
                _boolStates[key] = (bool)(object)value;
            else if (typeOfT == typeof(string))
                _stringStates[key] = (string)(object)value;
            else if (typeof(Enum).IsAssignableFrom(typeOfT))
                _enumStates[key] = (int)(object)value;
            else if (typeOfT == typeof(int))
                _intStates[key] = (int)(object)value;
            else if (typeOfT == typeof(float))
                _floatStates[key] = (float)(object)value;
            else if (typeof(UnityEngine.Object).IsAssignableFrom(typeOfT))
                _objectStates[key] = (UnityEngine.Object)(object)value;
            else throw new InvalidOperationException($"EditorState doesn't support type {typeOfT.Name}");

            Save();
        }

        public bool PropertyExists<T>(string key)
        {
            InitDictionaries();
            Type typeOfT = typeof(T);
            key = BuildActualKey<T>(key);
            if (typeOfT == typeof(bool))
                return _boolStates.ContainsKey(key);
            else if (typeOfT == typeof(string))
                return _stringStates.ContainsKey(key);
            else if (typeof(Enum).IsAssignableFrom(typeOfT))
                return _enumStates.ContainsKey(key);
            else if (typeOfT == typeof(int))
                return _intStates.ContainsKey(key);
            else if (typeOfT == typeof(float))
                return _floatStates.ContainsKey(key);
            else if (typeof(UnityEngine.Object).IsAssignableFrom(typeOfT))
                return _objectStates.ContainsKey(key);
            return false;
        }

        public T GetProperty<T>(string key)
        {
            InitDictionaries();
            Type typeOfT = typeof(T);
            string actualKey = BuildActualKey<T>(key);

            try 
            {
                if (typeOfT == typeof(bool))
                    return (T)(object)_boolStates[actualKey];
                else if (typeOfT == typeof(string))
                    return (T)(object)_stringStates[actualKey];
                else if (typeof(Enum).IsAssignableFrom(typeOfT))
                    return (T)(object)_enumStates[actualKey];
                else if (typeOfT == typeof(int))
                    return (T)(object)_intStates[actualKey];
                else if (typeOfT == typeof(float))
                    return (T)(object)_floatStates[actualKey];
                else if (typeof(UnityEngine.Object).IsAssignableFrom(typeOfT))
                    return (T)(object)_objectStates[actualKey];
            } 
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException($"Unknown state property {key}");
            }
            throw new KeyNotFoundException($"Unknown state property {key}");
        }

        public bool Remove<T>(string key)
        {
            InitDictionaries();
            bool removed = false;
            Type typeOfT = typeof(T);
            key = BuildActualKey<T>(key);
            if (typeOfT == typeof(bool))
                removed = _boolStates.Remove(key);
            else if (typeOfT == typeof(string))
                removed = _stringStates.Remove(key);
            else if (typeof(Enum).IsAssignableFrom(typeOfT))
                removed = _enumStates.Remove(key);
            else if (typeOfT == typeof(int))
                removed = _intStates.Remove(key);
            else if (typeOfT == typeof(float))
                removed = _floatStates.Remove(key);
            else if (typeof(UnityEngine.Object).IsAssignableFrom(typeOfT))
                removed = _objectStates.Remove(key);
            return removed;
        }

        public Property<T> GetPropertyAccess<T>(string key)
        {
            if (PropertyExists<T>(key)) return new PropertyImpl<T>(this, key);
            throw new ArgumentException($"Property {key} doesn't exists");
        }

        public T GetPropertyOrValue<T>(string key, T defValue)
        {
            if (PropertyExists<T>(key)) return GetProperty<T>(key);
            return defValue;
        }
    }
}
