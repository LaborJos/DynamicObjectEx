namespace DynamicObjectEx
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    internal interface IWrappedCollection : IList
    {
        object UnderlyingCollection { get; }
        void Reverse();
    }

    internal class CollectionWrapper<T> : ICollection<T>, IWrappedCollection
    {
        private enum CollectionType
        {
            UnknownType,
            IList,
            Collection,
            GenericCollection,
            GenericEnumerable,
        }

        private readonly IList _list;
        private readonly ICollection<T> _genericCollection;

        private readonly CollectionType _collectionType = CollectionType.UnknownType;

        private readonly ConstructorInfo _constructor = null;

        private object _syncRoot;

        public CollectionWrapper(IList list)
        {
            if (list is ICollection<T>)
            {
                ICollection<T> collection = list as ICollection<T>;

                this._genericCollection = collection;
                this._collectionType = CollectionType.GenericCollection;
            }
            else
            {
                this._list = list;
                this._collectionType = CollectionType.IList;
            }
        }

        public CollectionWrapper(ICollection<T> collection)
        {
            this._genericCollection = collection;
            this._collectionType = CollectionType.GenericCollection;
        }

        public CollectionWrapper(IEnumerable<T> enumerable)
        {
            this._constructor = enumerable.GetType().GetConstructor(new Type[] { typeof(IEnumerable<>).MakeGenericType(enumerable.GetType().GetGenericArguments()) });
            this._genericCollection = enumerable.ToList<T>();
            this._collectionType = CollectionType.GenericEnumerable;
        }

        public CollectionWrapper(ICollection collection)
        {
            this._constructor = collection.GetType().GetConstructor(new Type[] { typeof(ICollection) });
            this._genericCollection = collection.Cast<T>().ToList();
            this._collectionType = CollectionType.Collection;
        }

        public virtual void Add(T item)
        {
            switch (this._collectionType)
            {
                case CollectionType.IList:
                    this._list.Add(item);
                    break;

                case CollectionType.GenericEnumerable:
                case CollectionType.GenericCollection:
                case CollectionType.Collection:
                    this._genericCollection.Add(item);
                    break;

                case CollectionType.UnknownType:
                    throw new NotImplementedException();
            }
        }

        int IList.Add(object value)
        {
            Add((T)value);

            return (Count - 1);
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            switch (this._collectionType)
            {
                case CollectionType.IList:
                    this._list.CopyTo(array, arrayIndex);
                    break;

                case CollectionType.GenericCollection:
                case CollectionType.GenericEnumerable:
                case CollectionType.Collection:
                    this._genericCollection.CopyTo(array, arrayIndex);
                    break;

                case CollectionType.UnknownType:
                    throw new NotImplementedException();
            }
        }

        public virtual int Count
        {
            get
            {
                switch (this._collectionType)
                {
                    case CollectionType.IList:
                        return this._list.Count;

                    case CollectionType.GenericCollection:
                    case CollectionType.GenericEnumerable:
                    case CollectionType.Collection:
                        return this._genericCollection.Count;

                    case CollectionType.UnknownType:
                        throw new NotImplementedException();
                }

                return -1;
            }
        }

        public dynamic UnderlyingCollection
        {
            get
            {
                if (this._collectionType == CollectionType.IList)
                {
                    return this._list;
                }
                else if (this._collectionType == CollectionType.GenericCollection)
                {
                    return this._genericCollection;
                }
                else if (this._collectionType == CollectionType.GenericEnumerable)
                {
                    return (this._constructor == null) ?
                        this._genericCollection.AsEnumerable() :
                        this._constructor.Invoke(new object[] { this._genericCollection.AsEnumerable() });
                }
                else if (this._collectionType == CollectionType.Collection)
                {
                    return (this._constructor == null) ?
                        this._genericCollection as ICollection :
                        this._constructor.Invoke(new object[] { this._genericCollection as ICollection });
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public virtual void Reverse()
        {
            var list = new List<T>();

            foreach (var item in this)
            {
                list.Add(item);
            }

            list.Reverse();

            this.Clear();

            foreach (var item in list)
            {
                this.Add(item);
            }
        }

        public virtual void Clear()
        {
            if (_genericCollection != null)
            {
                _genericCollection.Clear();
            }
            else
            {
                _list.Clear();
            }
        }

        public virtual bool Contains(T item)
        {
            if (_genericCollection != null)
            {
                return _genericCollection.Contains(item);
            }
            else
            {
                return _list.Contains(item);
            }
        }


        public virtual bool IsReadOnly
        {
            get
            {
                if (_genericCollection != null)
                {
                    return _genericCollection.IsReadOnly;
                }
                else
                {
                    return _list.IsReadOnly;
                }
            }
        }

        public virtual bool Remove(T item)
        {
            if (_genericCollection != null)
            {
                return _genericCollection.Remove(item);
            }
            else
            {
                bool contains = _list.Contains(item);

                if (contains)
                {
                    _list.Remove(item);
                }

                return contains;
            }
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return (_genericCollection ?? _list.Cast<T>()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_genericCollection ?? _list).GetEnumerator();
        }

        bool IList.Contains(object value)
        {
            return Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            if (_genericCollection != null)
            {
                throw new InvalidOperationException("Wrapped ICollection<T> does not support IndexOf.");
            }

            return _list.IndexOf((T)value);
        }

        void IList.RemoveAt(int index)
        {
            if (_genericCollection != null)
            {
                throw new InvalidOperationException("Wrapped ICollection<T> does not support RemoveAt.");
            }

            _list.RemoveAt(index);
        }

        void IList.Insert(int index, object value)
        {
            if (_genericCollection != null)
            {
                throw new InvalidOperationException("Wrapped ICollection<T> does not support Insert.");
            }

            _list.Insert(index, (T)value);
        }

        bool IList.IsFixedSize
        {
            get
            {
                if (_genericCollection != null)
                {
                    // ICollection<T> only has IsReadOnly
                    return _genericCollection.IsReadOnly;
                }
                else
                {
                    return _list.IsFixedSize;
                }
            }
        }

        void IList.Remove(object value)
        {
            Remove((T)value);
        }

        object IList.this[int index]
        {
            get
            {
                if (_genericCollection != null)
                {
                    throw new InvalidOperationException("Wrapped ICollection<T> does not support indexer.");
                }

                return _list[index];
            }
            set
            {
                if (_genericCollection != null)
                {
                    throw new InvalidOperationException("Wrapped ICollection<T> does not support indexer.");
                }

                _list[index] = (T)value;
            }
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            CopyTo((T[])array, arrayIndex);
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                }

                return _syncRoot;
            }
        }
    }
}
