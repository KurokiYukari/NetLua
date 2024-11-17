/*
 * See LICENSE file
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace NetLua
{
    public interface ILuaTable : IDictionary<LuaObject, LuaObject>
    {
        IReadOnlyList<LuaObject> List { get; }

        void Insert(int index, LuaObject item);
        void RemoveAt(int index);
        void Sort(Comparison<LuaObject> comparison = null);
    }

    public class LuaTable : ILuaTable
    {
        private readonly Dictionary<LuaObject, LuaObject> _dictionary = new Dictionary<LuaObject, LuaObject>();
        private readonly List<LuaObject> _list = new List<LuaObject>();
        public IReadOnlyList<LuaObject> List => _list;

        public LuaObject this[LuaObject key]
        {
            get
            {
                TryGetValue(key, out var value);
                return value ?? LuaObject.Nil;
            }
            set
            {
                if (key == null || key.IsNil)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                value ??= LuaObject.Nil;

                if (key.TryConvertToInt(out int index))
                {
                    if (index > 0 && index <= _list.Count)
                    {
                        _list[index - 1] = value;
                        while (_list.Count > 0 && _list[^1].IsNil)
                        {
                            _list.RemoveAt(_list.Count - 1);
                        }
                        return;
                    }
                    else if (index == _list.Count + 1)
                    {
                        Add(value);
                        return;
                    }
                }

                if (value.IsNil)
                {
                    _dictionary.Remove(key);
                }
                else
                {
                    _dictionary[key] = value;
                }
            }
        }

        class KeysCollection : ICollection<LuaObject>
        {
            private readonly LuaTable _table;

            public KeysCollection(LuaTable table)
            {
                _table = table;
            }

            public int Count => _table.Count;
            public bool IsReadOnly => true;

            public void Add(LuaObject item)
            {
                throw new NotImplementedException();
            }

            public bool Remove(LuaObject item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(LuaObject item)
            {
                return _table.ContainsKey(item);
            }

            public void CopyTo(LuaObject[] array, int arrayIndex)
            {
                foreach (var (k, _) in _table)
                {
                    array[arrayIndex++] = k;
                }
            }

            public IEnumerator<LuaObject> GetEnumerator()
            {
                foreach (var (k, _) in _table)
                {
                    yield return k;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private KeysCollection _keys;
        public ICollection<LuaObject> Keys => _keys ??= new KeysCollection(this);

        class ValuesCollection : ICollection<LuaObject>
        {
            private readonly LuaTable _table;

            public ValuesCollection(LuaTable table)
            {
                _table = table;
            }

            public int Count => _table.Count;
            public bool IsReadOnly => true;

            public void Add(LuaObject item)
            {
                throw new NotImplementedException();
            }

            public bool Remove(LuaObject item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(LuaObject item)
            {
                return _table.ContainsValue(item);
            }

            public void CopyTo(LuaObject[] array, int arrayIndex)
            {
                foreach (var (_, v) in _table)
                {
                    array[arrayIndex++] = v;
                }
            }

            public IEnumerator<LuaObject> GetEnumerator()
            {
                foreach (var (_, v) in _table)
                {
                    yield return v;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private ValuesCollection _values;
        public ICollection<LuaObject> Values => _values ??= new ValuesCollection(this);

        public int Count => _list.Count + _dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(LuaObject key, LuaObject value)
        {
            if (key.TryConvertToInt(out int index) && index == _list.Count + 1)
            {
                Add(value);
            }
            else
            {
                _dictionary.Add(key, value);
            }
        }

        public void Add(KeyValuePair<LuaObject, LuaObject> item)
        {
            Add(item.Key, item.Value);
        }

        public void Add(LuaObject item)
        {
            if (item == null || item.IsNil)
            {
                return;
            }

            _list.Add(item);

            FixList();
        }

        private void FixList()
        {
            while (_dictionary.Remove(_list.Count + 1, out var value))
            {
                _list.Add(value);
            }
        }

        public void Clear()
        {
            _list.Clear();
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<LuaObject, LuaObject> item)
        {
            if (TryGetValue(item.Key, out var value))
            {
                return value == item.Value;
            }
            return false;
        }

        public bool ContainsValue(LuaObject item)
        {
            if (_list.Contains(item))
            {
                return true;
            }

            return _dictionary.ContainsValue(item);
        }

        public bool ContainsKey(LuaObject key)
        {
            return TryGetValue(key, out _);
        }

        public void CopyTo(KeyValuePair<LuaObject, LuaObject>[] array, int arrayIndex)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                array[arrayIndex++] = new KeyValuePair<LuaObject, LuaObject>(i + 1, _list[i]);
            }
            foreach (var item in _dictionary)
            {
                array[arrayIndex++] = item;
            }
        }

        public int IndexOf(LuaObject item)
        {
            var index = _list.IndexOf(item);
            if (index >= 0)
            {
                return index + 1;
            }

            // consider about dict index?
            return -1;
        }

        public void Insert(int index, LuaObject item)
        {
            if (index == _list.Count + 1)
            {
                Add(item);
            }
            else
            {
                _list.Insert(index - 1, item);
            }
        }

        public bool Remove(LuaObject key)
        {
            bool contains = ContainsKey(key);
            if (contains)
            {
                this[key] = LuaObject.Nil;
            }
            return contains;
        }

        public bool Remove(KeyValuePair<LuaObject, LuaObject> item)
        {
            bool contains = Contains(item);
            if (contains)
            {
                this[item.Key] = LuaObject.Nil;
            }
            return contains;
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index - 1);
        }

        public bool TryGetValue(LuaObject key, out LuaObject value)
        {
            if (key == null || key.IsNil)
            {
                value = LuaObject.Nil;
                return false;
            }

            if (key.TryConvertToInt(out int index) && index > 0 && index <= _list.Count)
            {
                value = _list[index - 1];
                return true;
            }
            if (_dictionary.TryGetValue(key, out value))
            {
                return true;
            }

            value = LuaObject.Nil;
            return false;
        }

        public void Sort(Comparison<LuaObject> comparison)
        {
            _list.Sort(comparison);
        }

        public struct Enumerator : IEnumerator<KeyValuePair<LuaObject, LuaObject>>
        {
            public static Enumerator Create(LuaTable table)
            {
                var result = new Enumerator();
                result._table = table;
                result.Reset();
                return result;
            }

            private LuaTable _table;
            private int _listIndex;

            public readonly KeyValuePair<LuaObject, LuaObject> Current
            {
                get
                {
                    if (_listEnumerator != null)
                    {
                        return KeyValuePair.Create(LuaObject.FromNumber(_listIndex), _listEnumerator.Value.Current);
                    }
                    if (_dictEnumerator != null)
                    {
                        return _dictEnumerator.Value.Current;
                    }
                    return default;
                }
            }
            readonly object IEnumerator.Current => Current;

            private List<LuaObject>.Enumerator? _listEnumerator;
            private Dictionary<LuaObject, LuaObject>.Enumerator? _dictEnumerator;

            public bool MoveNext()
            {
                if (_listEnumerator != null)
                {
                    if (_listEnumerator.Value.MoveNext())
                    {
                        _listIndex++;
                        return true;
                    }
                    else
                    {
                        _listEnumerator = null;
                    }
                }
                if (_dictEnumerator != null)
                {
                    if (_dictEnumerator.Value.MoveNext())
                    {
                        return true;
                    }
                    else
                    {
                        _dictEnumerator = null;
                    }
                }
                return false;
            }

            public void Reset()
            {
                _listIndex = 0;
                _listEnumerator = _table._list.GetEnumerator();
                _dictEnumerator = _table._dictionary.GetEnumerator();
            }

            public void Dispose()
            {
                _listIndex = 0;
                _listEnumerator = null;
                _dictEnumerator = null;
            }
        }

        public Enumerator GetEnumerator()
        {
            return Enumerator.Create(this);
        }

        IEnumerator<KeyValuePair<LuaObject, LuaObject>> IEnumerable<KeyValuePair<LuaObject, LuaObject>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
