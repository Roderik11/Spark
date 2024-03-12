using System;
using System.Collections.Generic;

namespace Spark
{
    public class Observable<T> : List<T>
    {
        #region Events

        /// <summary>
        /// Raises when an item is added to the list.
        /// </summary>
        public event EventHandler<ListEventArgs<T>> ItemAdded;

        /// <summary>
        /// Raises before an item is added to the list.
        /// </summary>
        public event EventHandler<ListEventArgs<T>> BeforeItemAdded;

        /// <summary>
        /// Raises when an item is removed from the list.
        /// </summary>
        public event EventHandler<ListEventArgs<T>> ItemRemoved;

        /// <summary>
        /// Raises before an item is removed from the list.
        /// </summary>
        public event EventHandler<ListEventArgs<T>> BeforeItemRemoved;

        /// <summary>
        /// Raises when the items are cleared from the list.
        /// </summary>
        public event EventHandler<EventArgs> ItemsCleared;

        /// <summary>
        /// Raises before the items are cleared from the list.
        /// </summary>
        public event EventHandler<EventArgs> BeforeItemsCleared;

        /// <summary>
        /// Raises when the Items are sorted using Sort
        /// </summary>
        public event EventHandler<EventArgs> ItemsSorted;

        #endregion Events

        #region IList Methods

        // Summary:
        //     Gets or sets the element at the specified index.
        //
        // Parameters:
        //   index:
        //     The zero-based index of the element to get or set.
        //
        // Returns:
        //     The element at the specified index.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is not a valid index in the System.Collections.Generic.IList<T>.
        //
        //   System.NotSupportedException:
        //     The property is set and the System.Collections.Generic.IList<T> is read-only.
        //[Xml.XmlIgnore]
        public new T this[int index]
        {
            get { return base[index]; }
            set { base[index] = value; }
        }

        //
        // Summary:
        //     Inserts an item to the System.Collections.Generic.IList<T> at the specified
        //     index.
        //
        // Parameters:
        //   index:
        //     The zero-based index at which item should be inserted.
        //
        //   item:
        //     The object to insert into the System.Collections.Generic.IList<T>.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is not a valid index in the System.Collections.Generic.IList<T>.
        //
        //   System.NotSupportedException:
        //     The System.Collections.Generic.IList<T> is read-only.
        public new void Insert(int index, T item)
        {
            ListEventArgs<T> args = new ListEventArgs<T>(item);

            OnBeforeItemAdded(this, args);

            if (!args.Cancel)
            {
                base.Insert(index, item);
                OnItemAdded(this, args);
            }

            args = null;
        }

        public new void InsertRange(int index, IEnumerable<T> collection)
        {
            if (index < 0) return;
            if (index > Count - 1) return;

            foreach (T t in collection)
            {
                Insert(index, t);
                index++;
            }
        }

        public new void Sort()
        {
            base.Sort();
            OnItemsSorted(this, new EventArgs());
        }

        public new void Sort(Comparison<T> comparison)
        {
            base.Sort(comparison);
            OnItemsSorted(this, new EventArgs());
        }

        public new void Sort(IComparer<T> comparer)
        {
            base.Sort(comparer);
            OnItemsSorted(this, new EventArgs());
        }

        public new void Sort(int index, int count, IComparer<T> comparer)
        {
            base.Sort(index, count, comparer);
            OnItemsSorted(this, new EventArgs());
        }

        //
        // Summary:
        //     Removes the System.Collections.Generic.IList<T> item at the specified index.
        //
        // Parameters:
        //   index:
        //     The zero-based index of the item to remove.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     index is not a valid index in the System.Collections.Generic.IList<T>.
        //
        //   System.NotSupportedException:
        //     The System.Collections.Generic.IList<T> is read-only.
        public new void RemoveAt(int index)
        {
            ListEventArgs<T> args = new ListEventArgs<T>(this[index]);

            OnBeforeItemRemoved(this, args);

            if (!args.Cancel)
            {
                base.RemoveAt(index);

                OnItemRemoved(this, args);
            }

            args = null;
        }

        public new void RemoveRange(int index, int count)
        {
            if (index < 0 || count < 1) return;
            if (index > Count - 1) return;

            for (int i = 0; i < count; i++)
            {
                if (index > Count - 1) break;

                RemoveAt(index);
            }
        }

        public new int RemoveAll(Predicate<T> match)
        {
            int result = 0;
            int count = Count - 1;

            for (int i = count; i >= 0; i--)
            {
                if (match.Invoke(this[i]))
                {
                    result++;

                    ListEventArgs<T> args = new ListEventArgs<T>(this[i]);
                    OnBeforeItemRemoved(this, args);

                    if (!args.Cancel)
                    {
                        base.RemoveAt(i);

                        OnItemRemoved(this, args);
                    }
                }
            }
            return result;
        }

        public new void Reverse()
        {
            base.Reverse();
            OnItemsSorted(this, new EventArgs());
        }

        public new void Reverse(int index, int count)
        {
            base.Reverse(index, count);
            OnItemsSorted(this, new EventArgs());
        }

        #endregion IList Methods

        #region ICollection Methods and Properties

        // Summary:
        //     Adds an item to the System.Collections.Generic.ICollection<T>.
        //
        // Parameters:
        //   item:
        //     The object to add to the System.Collections.Generic.ICollection<T>.
        //
        // Exceptions:
        //   System.NotSupportedException:
        //     The System.Collections.Generic.ICollection<T> is read-only.
        public new void Add(T item)
        {
            ListEventArgs<T> args = new ListEventArgs<T>(item);

            OnBeforeItemAdded(this, args);

            if (!args.Cancel)
            {
                base.Add(item);
                OnItemAdded(this, args);
            }

            args = null;
        }

        // Summary:
        //     Adds a range if items to the System.Collections.Generic.ICollection<T>.
        //
        // Parameters:
        //   item:
        //     The range of objects to add to the System.Collections.Generic.ICollection<T>.
        //
        // Exceptions:
        //   System.NotSupportedException:
        //     The System.Collections.Generic.ICollection<T> is read-only.
        public new void AddRange(IEnumerable<T> list)
        {
            foreach (T t in list)
                Add(t);
        }

        //
        // Summary:
        //     Removes all items from the System.Collections.Generic.ICollection<T>.
        //
        // Exceptions:
        //   System.NotSupportedException:
        //     The System.Collections.Generic.ICollection<T> is read-only.
        public new void Clear()
        {
            OnBeforeItemsCleared(this, new EventArgs());
            base.Clear();
            OnItemsCleared(this, new EventArgs());
        }

        //
        // Summary:
        //     Removes the first occurrence of a specific object from the System.Collections.Generic.ICollection<T>.
        //
        // Parameters:
        //   item:
        //     The object to remove from the System.Collections.Generic.ICollection<T>.
        //
        // Returns:
        //     true if item was successfully removed from the System.Collections.Generic.ICollection<T>;
        //     otherwise, false. This method also returns false if item is not found in
        //     the original System.Collections.Generic.ICollection<T>.
        //
        // Exceptions:
        //   System.NotSupportedException:
        //     The System.Collections.Generic.ICollection<T> is read-only.
        public new bool Remove(T item)
        {
            ListEventArgs<T> args = new ListEventArgs<T>(item);

            OnBeforeItemRemoved(this, args);

            if (!args.Cancel)
            {
                bool happened = base.Remove(item);

                if (happened)
                    OnItemRemoved(this, args);

                return happened;
            }

            args = null;

            return false;
        }

        #endregion ICollection Methods and Properties

        #region Event Methods

        /// <summary>
        /// Raises when an Item is added to the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">GenericItemEventArgs</param>
        protected virtual void OnItemAdded(object sender, ListEventArgs<T> e)
        {
            if (ItemAdded != null)
                ItemAdded(sender, e);
        }

        /// <summary>
        /// Raises before an Item is added to the list.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">GenericItemEventArgs</param>
        protected virtual void OnBeforeItemAdded(object sender, ListEventArgs<T> e)
        {
            if (BeforeItemAdded != null)
                BeforeItemAdded(sender, e);
        }

        /// <summary>
        /// Raises when an Item is removed from the list.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventsArgs</param>
        protected virtual void OnItemRemoved(object sender, ListEventArgs<T> e)
        {
            if (ItemRemoved != null)
                ItemRemoved(sender, e);
        }

        /// <summary>
        /// Raises before an Item is removed from the list.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">GenericItemEventArgs</param>
        protected virtual void OnBeforeItemRemoved(object sender, ListEventArgs<T> e)
        {
            if (BeforeItemRemoved != null)
                BeforeItemRemoved(sender, e);
        }

        /// <summary>
        /// Raises when the Items are cleared from this list.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        protected virtual void OnItemsCleared(object sender, EventArgs e)
        {
            if (ItemsCleared != null)
                ItemsCleared(sender, e);
        }

        /// <summary>
        /// Raises before the Items are cleared from this list.
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        protected virtual void OnBeforeItemsCleared(object sender, EventArgs e)
        {
            if (BeforeItemsCleared != null)
                BeforeItemsCleared(sender, e);
        }

        /// <summary>
        /// Raises when the Items are sorted using Sort
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        protected virtual void OnItemsSorted(object sender, EventArgs e)
        {
            if (ItemsSorted != null)
                ItemsSorted(sender, e);
        }

        #endregion Event Methods
    }

    public class ListEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Item
        /// </summary>
        public T Item { get; private set; }

        public bool Cancel { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="item"></param>
        public ListEventArgs(T item)
        {
            this.Item = item;
        }
    }
}