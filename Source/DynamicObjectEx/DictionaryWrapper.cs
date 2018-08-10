namespace DynamicObjectEx
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    internal interface IWrappedDictionary : IDictionary
    {
        object UnderlyingDictionary { get; }
    }

    internal class DictionaryWrapper<TKey, TValue> : IDictionary<TKey, TValue>, IWrappedDictionary
    {
        private readonly IDictionary dictionary;
        private readonly IDictionary<TKey, TValue> genericDictionary;
        private readonly IReadOnlyDictionary<TKey, TValue> readOnlyDictionary;
        private object syncRoot;

        public DictionaryWrapper(IDictionary @dictionary)
        {
            this.dictionary = @dictionary;
        }

        public DictionaryWrapper(IDictionary<TKey, TValue> dictionary)
        {
            this.genericDictionary = dictionary;
        }

        public DictionaryWrapper(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            this.readOnlyDictionary = dictionary;
        }

        public void Add(TKey key, TValue value)
        {
            if (this.dictionary != null)
            {
                this.dictionary.Add(key, value);
            }
            else if (this.genericDictionary != null)
            {
                this.genericDictionary.Add(key, value);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public bool ContainsKey(TKey key)
        {
            if (this.dictionary != null)
            {
                return this.dictionary.Contains(key);
            }
            else if (this.readOnlyDictionary != null)
            {
                return this.readOnlyDictionary.ContainsKey(key);
            }
            else
            {
                return this.genericDictionary.ContainsKey(key);
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                if (this.dictionary != null)
                {
                    return this.dictionary.Keys.Cast<TKey>().ToList();
                }
                else if (this.readOnlyDictionary != null)
                {
                    return this.readOnlyDictionary.Keys.ToList();
                }
                else
                {
                    return this.genericDictionary.Keys;
                }
            }
        }

        public bool Remove(TKey key)
        {
            if (this.dictionary != null)
            {
                if (this.dictionary.Contains(key))
                {
                    this.dictionary.Remove(key);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (this.readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                return this.genericDictionary.Remove(key);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (this.dictionary != null)
            {
                if (!this.dictionary.Contains(key))
                {
                    value = default(TValue);
                    return false;
                }
                else
                {
                    value = (TValue)this.dictionary[key];
                    return true;
                }
            }
            else if (this.readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                return this.genericDictionary.TryGetValue(key, out value);
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (this.dictionary != null)
                {
                    return this.dictionary.Values.Cast<TValue>().ToList();
                }
                else if (this.readOnlyDictionary != null)
                {
                    return this.readOnlyDictionary.Values.ToList();
                }
                else
                {
                    return this.genericDictionary.Values;
                }
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                if (this.dictionary != null)
                {
                    return (TValue)this.dictionary[key];
                }
                else if (this.readOnlyDictionary != null)
                {
                    return this.readOnlyDictionary[key];
                }
                else
                {
                    return this.genericDictionary[key];
                }
            }
            set
            {
                if (this.dictionary != null)
                {
                    this.dictionary[key] = value;
                }
                else if (this.readOnlyDictionary != null)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    this.genericDictionary[key] = value;
                }
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if (this.dictionary != null)
            {
                ((IList)this.dictionary).Add(item);
            }
            else if (this.readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                if (this.genericDictionary != null) this.genericDictionary.Add(item);
            }
        }

        public void Clear()
        {
            if (this.dictionary != null)
            {
                this.dictionary.Clear();
            }
            else if (this.readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                this.genericDictionary.Clear();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (this.dictionary != null)
            {
                return ((IList)this.dictionary).Contains(item);
            }
            else if (this.readOnlyDictionary != null)
            {
                return this.readOnlyDictionary.Contains(item);
            }
            else
            {
                return this.genericDictionary.Contains(item);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (this.dictionary != null)
            {
                var enumerator = this.dictionary.GetEnumerator();

                try
                {
                    while (enumerator.MoveNext())
                    {
                        var entry = enumerator.Entry;

                        array[arrayIndex++] = new KeyValuePair<TKey, TValue>((TKey)entry.Key, (TValue)entry.Value);
                    }
                }
                finally
                {
                    if (enumerator is IDisposable)
                    {
                        (enumerator as IDisposable).Dispose();
                    }
                }
            }
            else if (this.readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                this.genericDictionary.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                if (this.dictionary != null)
                {
                    return this.dictionary.Count;
                }
                else if (this.readOnlyDictionary != null)
                {
                    return this.readOnlyDictionary.Count;
                }
                else
                {
                    return this.genericDictionary.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                if (this.dictionary != null)
                {
                    return this.dictionary.IsReadOnly;
                }
                else if (this.readOnlyDictionary != null)
                {
                    return true;
                }
                else
                {
                    return this.genericDictionary.IsReadOnly;
                }
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (this.dictionary != null)
            {
                if (this.dictionary.Contains(item.Key))
                {
                    var value = this.dictionary[item.Key];

                    if (object.Equals(value, item.Value))
                    {
                        this.dictionary.Remove(item.Key);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            else if (this.readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                return this.genericDictionary.Remove(item);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (this.dictionary != null)
            {
                return this.dictionary.Cast<DictionaryEntry>().Select(de => new KeyValuePair<TKey, TValue>((TKey)de.Key, (TValue)de.Value)).GetEnumerator();
            }
            else if (this.readOnlyDictionary != null)
            {
                return this.readOnlyDictionary.GetEnumerator();
            }
            else
            {
                return this.genericDictionary.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDictionary.Add(object key, object value)
        {
            if (this.dictionary != null)
            {
                this.dictionary.Add(key, value);
            }
            else if (this.readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                this.genericDictionary.Add((TKey)key, (TValue)value);
            }
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (this.dictionary != null)
                {
                    return this.dictionary[key];
                }
                else if (this.readOnlyDictionary != null)
                {
                    return this.readOnlyDictionary[(TKey)key];
                }
                else
                {
                    return this.genericDictionary[(TKey)key];
                }
            }
            set
            {
                if (this.dictionary != null)
                {
                    this.dictionary[key] = value;
                }
                else if (this.readOnlyDictionary != null)
                {
                    throw new NotSupportedException();
                }
                else
                {
                    this.genericDictionary[(TKey)key] = (TValue)value;
                }
            }
        }

        private struct DictionaryEnumerator<TEnumeratorKey, TEnumeratorValue> : IDictionaryEnumerator
        {
            private readonly IEnumerator<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> enumerator;

            public DictionaryEnumerator(IEnumerator<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> @enumerator)
            {
                this.enumerator = @enumerator;
            }

            public DictionaryEntry Entry
            {
                get { return (DictionaryEntry)Current; }
            }

            public object Key
            {
                get { return Entry.Key; }
            }

            public object Value
            {
                get { return Entry.Value; }
            }

            public object Current
            {
                get { return new DictionaryEntry(enumerator.Current.Key, enumerator.Current.Value); }
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            if (this.dictionary != null)
            {
                return this.dictionary.GetEnumerator();
            }
            else if (this.readOnlyDictionary != null)
            {
                return new DictionaryEnumerator<TKey, TValue>(this.readOnlyDictionary.GetEnumerator());
            }
            else
            {
                return new DictionaryEnumerator<TKey, TValue>(this.genericDictionary.GetEnumerator());
            }
        }

        bool IDictionary.Contains(object key)
        {
            if (this.genericDictionary != null)
            {
                return this.genericDictionary.ContainsKey((TKey)key);
            }
            else if (this.readOnlyDictionary != null)
            {
                return this.readOnlyDictionary.ContainsKey((TKey)key);
            }
            else
            {
                return dictionary.Contains(key);
            }
        }

        bool IDictionary.IsFixedSize
        {
            get
            {
                if (this.genericDictionary != null)
                {
                    return false;
                }
                else if (this.readOnlyDictionary != null)
                {
                    return true;
                }
                else
                {
                    return this.dictionary.IsFixedSize;
                }
            }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                if (this.genericDictionary != null)
                {
                    return this.genericDictionary.Keys.ToList();
                }
                else if (this.readOnlyDictionary != null)
                {
                    return this.readOnlyDictionary.Keys.ToList();
                }
                else
                {
                    return this.dictionary.Keys;
                }
            }
        }

        public void Remove(object key)
        {
            if (this.dictionary != null)
            {
                this.dictionary.Remove(key);
            }
            else if (this.readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                this.genericDictionary.Remove((TKey)key);
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                if (this.genericDictionary != null)
                {
                    return this.genericDictionary.Values.ToList();
                }
                else if (this.readOnlyDictionary != null)
                {
                    return this.readOnlyDictionary.Values.ToList();
                }
                else
                {
                    return this.dictionary.Values;
                }
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (this.dictionary != null)
            {
                this.dictionary.CopyTo(array, index);
            }
            else if (this.readOnlyDictionary != null)
            {
                throw new NotSupportedException();
            }
            else
            {
                this.genericDictionary.CopyTo((KeyValuePair<TKey, TValue>[])array, index);
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                if (this.dictionary != null)
                {
                    return this.dictionary.IsSynchronized;
                }
                else
                {
                    return false;
                }
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (syncRoot == null)
                {
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);
                }

                return syncRoot;
            }
        }

        public object UnderlyingDictionary
        {
            get
            {
                if (this.dictionary != null)
                {
                    return this.dictionary;
                }
                else if (this.readOnlyDictionary != null)
                {
                    return this.readOnlyDictionary;
                }
                else
                {
                    return this.genericDictionary;
                }
            }
        }
    }
}
