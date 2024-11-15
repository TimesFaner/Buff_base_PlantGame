using System;
using System.Collections;
using System.Collections.Generic;

namespace Mirror
{
    public class SyncSet<T> : SyncObject, ISet<T>
    {
        public delegate void SyncSetChanged(Operation op, T item);

        public enum Operation : byte
        {
            OP_ADD,
            OP_CLEAR,
            OP_REMOVE
        }

        // list of changes.
        // -> insert/delete/clear is only ONE change
        // -> changing the same slot 10x caues 10 changes.
        // -> note that this grows until next sync(!)
        // TODO Dictionary<key, change> to avoid ever growing changes / redundant changes!
        private readonly List<Change> changes = new();

        protected readonly ISet<T> objects;

        // how many changes we need to ignore
        // this is needed because when we initialize the list,
        // we might later receive changes that have already been applied
        // so we need to skip them
        private int changesAhead;

        public SyncSet(ISet<T> objects)
        {
            this.objects = objects;
        }

        public int Count => objects.Count;
        public bool IsReadOnly => !IsWritable();

        public bool Add(T item)
        {
            if (objects.Add(item))
            {
                AddOperation(Operation.OP_ADD, item, true);
                return true;
            }

            return false;
        }

        void ICollection<T>.Add(T item)
        {
            if (objects.Add(item)) AddOperation(Operation.OP_ADD, item, true);
        }

        public void Clear()
        {
            objects.Clear();
            AddOperation(Operation.OP_CLEAR, true);
        }

        public bool Contains(T item)
        {
            return objects.Contains(item);
        }

        public void CopyTo(T[] array, int index)
        {
            objects.CopyTo(array, index);
        }

        public bool Remove(T item)
        {
            if (objects.Remove(item))
            {
                AddOperation(Operation.OP_REMOVE, item, true);
                return true;
            }

            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return objects.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == this)
            {
                Clear();
                return;
            }

            // remove every element in other from this
            foreach (var element in other) Remove(element);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            if (other is ISet<T> otherSet)
            {
                IntersectWithSet(otherSet);
            }
            else
            {
                var otherAsSet = new HashSet<T>(other);
                IntersectWithSet(otherAsSet);
            }
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return objects.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return objects.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return objects.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return objects.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return objects.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return objects.SetEquals(other);
        }

        // custom implementation so we can do our own Clear/Add/Remove for delta
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == this)
                Clear();
            else
                foreach (var element in other)
                    if (!Remove(element))
                        Add(element);
        }

        // custom implementation so we can do our own Clear/Add/Remove for delta
        public void UnionWith(IEnumerable<T> other)
        {
            if (other != this)
                foreach (var element in other)
                    Add(element);
        }

        public event SyncSetChanged Callback;

        public override void Reset()
        {
            changes.Clear();
            changesAhead = 0;
            objects.Clear();
        }

        // throw away all the changes
        // this should be called after a successful sync
        public override void ClearChanges()
        {
            changes.Clear();
        }

        private void AddOperation(Operation op, T item, bool checkAccess)
        {
            if (checkAccess && IsReadOnly)
                throw new InvalidOperationException("SyncSets can only be modified by the owner.");

            var change = new Change
            {
                operation = op,
                item = item
            };

            if (IsRecording())
            {
                changes.Add(change);
                OnDirty?.Invoke();
            }

            Callback?.Invoke(op, item);
        }

        private void AddOperation(Operation op, bool checkAccess)
        {
            AddOperation(op, default, checkAccess);
        }

        public override void OnSerializeAll(NetworkWriter writer)
        {
            // if init,  write the full list content
            writer.WriteUInt((uint)objects.Count);

            foreach (var obj in objects) writer.Write(obj);

            // all changes have been applied already
            // thus the client will need to skip all the pending changes
            // or they would be applied again.
            // So we write how many changes are pending
            writer.WriteUInt((uint)changes.Count);
        }

        public override void OnSerializeDelta(NetworkWriter writer)
        {
            // write all the queued up changes
            writer.WriteUInt((uint)changes.Count);

            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                writer.WriteByte((byte)change.operation);

                switch (change.operation)
                {
                    case Operation.OP_ADD:
                        writer.Write(change.item);
                        break;

                    case Operation.OP_CLEAR:
                        break;

                    case Operation.OP_REMOVE:
                        writer.Write(change.item);
                        break;
                }
            }
        }

        public override void OnDeserializeAll(NetworkReader reader)
        {
            // if init,  write the full list content
            var count = (int)reader.ReadUInt();

            objects.Clear();
            changes.Clear();

            for (var i = 0; i < count; i++)
            {
                var obj = reader.Read<T>();
                objects.Add(obj);
            }

            // We will need to skip all these changes
            // the next time the list is synchronized
            // because they have already been applied
            changesAhead = (int)reader.ReadUInt();
        }

        public override void OnDeserializeDelta(NetworkReader reader)
        {
            var changesCount = (int)reader.ReadUInt();

            for (var i = 0; i < changesCount; i++)
            {
                var operation = (Operation)reader.ReadByte();

                // apply the operation only if it is a new change
                // that we have not applied yet
                var apply = changesAhead == 0;
                T item = default;

                switch (operation)
                {
                    case Operation.OP_ADD:
                        item = reader.Read<T>();
                        if (apply)
                        {
                            objects.Add(item);
                            // add dirty + changes.
                            // ClientToServer needs to set dirty in server OnDeserialize.
                            // no access check: server OnDeserialize can always
                            // write, even for ClientToServer (for broadcasting).
                            AddOperation(Operation.OP_ADD, item, false);
                        }

                        break;

                    case Operation.OP_CLEAR:
                        if (apply)
                        {
                            objects.Clear();
                            // add dirty + changes.
                            // ClientToServer needs to set dirty in server OnDeserialize.
                            // no access check: server OnDeserialize can always
                            // write, even for ClientToServer (for broadcasting).
                            AddOperation(Operation.OP_CLEAR, false);
                        }

                        break;

                    case Operation.OP_REMOVE:
                        item = reader.Read<T>();
                        if (apply)
                        {
                            objects.Remove(item);
                            // add dirty + changes.
                            // ClientToServer needs to set dirty in server OnDeserialize.
                            // no access check: server OnDeserialize can always
                            // write, even for ClientToServer (for broadcasting).
                            AddOperation(Operation.OP_REMOVE, item, false);
                        }

                        break;
                }

                if (!apply)
                    // we just skipped this change
                    changesAhead--;
            }
        }

        private void IntersectWithSet(ISet<T> otherSet)
        {
            var elements = new List<T>(objects);

            foreach (var element in elements)
                if (!otherSet.Contains(element))
                    Remove(element);
        }

        private struct Change
        {
            internal Operation operation;
            internal T item;
        }
    }

    public class SyncHashSet<T> : SyncSet<T>
    {
        public SyncHashSet() : this(EqualityComparer<T>.Default)
        {
        }

        public SyncHashSet(IEqualityComparer<T> comparer) : base(
            new HashSet<T>(comparer ?? EqualityComparer<T>.Default))
        {
        }

        // allocation free enumerator
        public new HashSet<T>.Enumerator GetEnumerator()
        {
            return ((HashSet<T>)objects).GetEnumerator();
        }
    }

    public class SyncSortedSet<T> : SyncSet<T>
    {
        public SyncSortedSet() : this(Comparer<T>.Default)
        {
        }

        public SyncSortedSet(IComparer<T> comparer) : base(new SortedSet<T>(comparer ?? Comparer<T>.Default))
        {
        }

        // allocation free enumerator
        public new SortedSet<T>.Enumerator GetEnumerator()
        {
            return ((SortedSet<T>)objects).GetEnumerator();
        }
    }
}