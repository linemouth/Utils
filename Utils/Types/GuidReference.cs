using System;
using System.Collections.Generic;

namespace Utils
{
    public class GuidReference<T> : IEquatable<GuidReference<T>>, IEquatable<Guid> where T : class
    {
        public Guid Guid { get; }
        public T Item => GetItem(Guid);
        public bool IsEmpty => Guid == Guid.Empty;
        public bool Exists => items.TryGetValue(Guid, out WeakReference<T> reference) && reference.TryGetTarget(out T target) && target != null;

        private static readonly Dictionary<Guid, WeakReference<T>> items = new Dictionary<Guid, WeakReference<T>>();

        /// <summary>Creates a reference to an object identified by a GUID.</summary>
        /// <remarks>The object may or may not exist.</remarks>
        /// <param name="guid">The GUID which refers to an item.</param>
        public GuidReference(Guid guid) => Guid = guid;
        /// <summary>Creates a reference to the given item, instantiating a new GUID.</summary>
        /// <remarks>A new entry is created to allow lookups to the referred object.</remarks>
        /// <param name="item">The item to which the reference points.</param>
        public GuidReference(T item) : this(Guid.NewGuid(), item) { }
        /// <summary>Creates a reference which points to an existing object.</summary>
        /// <remarks>A new entry is created to allow lookups to the referred object.</remarks>
        /// <param name="guid">The GUID which refers to the object.</param>
        /// <param name="item">The object to which the reference points.</param>
        public GuidReference(Guid guid, T item) : this(guid) => SetGuid(guid, item);
        public static void SetGuid(Guid guid, T item) => items[guid] = new WeakReference<T>(item);
        public static T GetItem(Guid guid) => items.TryGetValue(guid, out WeakReference<T> reference) && reference.TryGetTarget(out T item) ? item : null;
        public bool Equals(GuidReference<T> other) => Guid.Equals(other.Guid);
        public bool Equals(Guid guid) => Guid.Equals(guid);
        public override bool Equals(object obj)
        {
            if(obj is GuidReference<T> reference) { return Equals(reference); }
            if(obj is Guid guid) { return Equals(guid); }
            return false;
        }
        public override int GetHashCode() => Guid.GetHashCode();
        public override string ToString() => Guid.ToString();
    }
}
