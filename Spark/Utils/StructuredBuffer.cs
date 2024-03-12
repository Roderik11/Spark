using System;
using System.Collections.Generic;
using System.Linq;

using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Spark
{
    public class StructuredBuffer<T> : IList<T>, IDisposable where T : struct
    {
        #region GPUList Members

        private DeviceContext context;
        private Buffer buffer;
        private ShaderResourceView shaderResourceView;
        private UnorderedAccessView unorderedAccessView;

        private int count;
        private int capacity;
        private ResourceOptionFlags miscFlags = ResourceOptionFlags.BufferStructured;
        private BindFlags bindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess;

        /// <summary>
        /// Makes a new list given initial data
        /// </summary>
        /// <param name="context">Device context on which to perform all operations</param>
        public StructuredBuffer(DeviceContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Makes a new list given initial data
        /// </summary>
        /// <param name="context">Device context on which to perform all operations</param>
        public StructuredBuffer(DeviceContext context, ResourceOptionFlags flags, BindFlags bindFlags)
        {
            this.bindFlags = bindFlags;
            this.miscFlags = flags;
            this.context = context;
        }

        /// <summary>
        /// Makes a new empty list with an initial capacity
        /// </summary>
        /// <param name="context">Device context on which to perform all operations</param>
        /// <param name="initialCapacity">The initial capacity</param>
        public StructuredBuffer(DeviceContext context, int initialCapacity)
        {
            this.context = context;
            this.setCapacity(initialCapacity);
        }

        /// <summary>
        /// Makes a new list given initial data
        /// </summary>
        /// <param name="context">Device context on which to perform all operations</param>
        /// <param name="data">The initial data</param>
        public StructuredBuffer(DeviceContext context, IEnumerable<T> data)
        {
            this.context = context;
            this.AddRange(data);
        }

        /// <summary>
        /// Gets the DeviceContext on which to perform all operations
        /// </summary>
        public DeviceContext Context { get { return context; } }

        /// <summary>
        /// Gets the underlying Buffer stored on the GPU
        /// </summary>
        public Buffer Buffer { get { return buffer; } }

        /// <summary>
        /// Gets a ShaderResourceView for use in a shader
        /// </summary>
        public ShaderResourceView ShaderResource { get { return shaderResourceView; } }

        /// <summary>
        /// Gets a UnorderedAccessView for use in a shader
        /// </summary>
        public UnorderedAccessView UnorderedAccess { get { return unorderedAccessView; } }

        /// <summary>
        /// Gets or Sets the internal GPU buffer capacity in number of elements
        /// </summary>
        public int Capacity { get { return capacity; } set { setCapacity(value); } }

        /// <summary>
        /// Adds a range of items to the end of the list
        /// </summary>
        /// <param name="range"></param>
        public void AddRange(IEnumerable<T> range)
        {
            if (range == null)
                throw new ArgumentNullException();

            int dataSize = Utilities.SizeOf<T>();

            if (range is StructuredBuffer<T> && range != this)
            {
                StructuredBuffer<T> list = range as StructuredBuffer<T>;

                if (list.Count == 0)
                    return;
                if (Count + list.Count > Capacity)
                    setCapacity(2 * Count + list.Count);

                count += list.Count;

                list.CopyRangeTo(0, list.Count, this, Count - list.Count);
            }
            else
            {
                T[] data = null;
                if (range is T[])
                    data = (T[])range;
                else if (range is List<T>)
                    data = (range as List<T>).ToArray();
                else
                {
                    List<T> dataList = new List<T>(range);
                    data = dataList.ToArray();
                }

                if (data.Length == 0)
                    return;
                if (Count + data.Length > Capacity)
                    setCapacity(2 * Count + data.Length);

                count += data.Length;

                SetRange(Count - data.Length, data, 0, data.Length);
            }
        }

        /// <summary>
        /// Inserts a range of items into the list
        /// </summary>
        /// <param name="index">Position in the list to insert items</param>
        /// <param name="range">The range of items to insert</param>
        public void InsertRange(int index, IEnumerable<T> range)
        {
            if (range == null)
                throw new ArgumentNullException();
            if (index < 0 || index > Count)
                throw new IndexOutOfRangeException();

            if (index == Count)
            {
                AddRange(range);
            }
            else
            {
                StructuredBuffer<T> newList = null;

                if (range is StructuredBuffer<T>)
                {
                    StructuredBuffer<T> list = range as StructuredBuffer<T>;

                    if (list.Count == 0)
                        return;

                    newList = new StructuredBuffer<T>(Context, list.Count + Count);
                    newList.count = list.Count + Count;
                    this.CopyRangeTo(0, index, newList, 0);
                    list.CopyTo(newList, index);
                    this.CopyRangeTo(index, Count - index, newList, index + list.Count);
                }
                else
                {
                    T[] data = null;
                    if (range is T[])
                        data = (T[])range;
                    else if (range is List<T>)
                        data = (range as List<T>).ToArray();
                    else
                    {
                        List<T> dataList = new List<T>(range);
                        data = dataList.ToArray();
                    }

                    if (data.Length == 0)
                        return;

                    newList = new StructuredBuffer<T>(Context, data.Length + Count);
                    newList.count = data.Length + Count;
                    this.CopyRangeTo(0, index, newList, 0);
                    newList.SetRange(index, data, 0, data.Length);
                    this.CopyRangeTo(index, Count - index, newList, index + data.Length);
                }

                setCapacity(0);
                count = newList.count;
                capacity = newList.capacity;
                buffer = newList.buffer;
                shaderResourceView = newList.shaderResourceView;
                unorderedAccessView = newList.unorderedAccessView;
            }
        }

        /// <summary>
        /// Removes a range of items from the list
        /// </summary>
        /// <param name="index">Index to start removing</param>
        /// <param name="count">Number of items to remove</param>
        public void RemoveRange(int index, int count)
        {
            if (count == 0)
                return;

            if (count < 0)
                throw new IndexOutOfRangeException();
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException();
            if (index + count < 0 || index + count > Count)
                throw new IndexOutOfRangeException();

            if (index + count == Count)
            {
                this.count -= count;
                return;
            }

            StructuredBuffer<T> newList = new StructuredBuffer<T>(Context, Capacity);
            newList.count = this.count - count;
            this.CopyRangeTo(0, index, newList, 0);
            this.CopyRangeTo(index + count, this.count - (index + count), newList, index);

            setCapacity(0);
            this.count = newList.count;
            this.capacity = newList.capacity;
            this.buffer = newList.buffer;
            this.shaderResourceView = newList.shaderResourceView;
            this.unorderedAccessView = newList.unorderedAccessView;
        }

        /// <summary>
        /// Sets a range of data in the list based on an array
        /// </summary>
        /// <param name="index">Index in the GPUList to start setting data</param>
        /// <param name="array">The array of data to use</param>
        /// <param name="arrayIndex">The start index in the array</param>
        /// <param name="count">The number of elements to set</param>
        public void SetRange(int index, T[] array, int arrayIndex, int count)
        {
            if (count == 0)
                return;

            if (count < 0)
                throw new IndexOutOfRangeException();
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException();
            if (index + count < 0 || index + count > Count)
                throw new IndexOutOfRangeException();

            if (array == null)
                throw new ArgumentNullException();
            if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new IndexOutOfRangeException();
            if (arrayIndex + array.Length < 0 || arrayIndex + count > array.Length)
                throw new IndexOutOfRangeException();

            int dataSize = Utilities.SizeOf<T>();

            DataStream stream = new DataStream(dataSize * count, true, true);
            stream.WriteRange<T>(array);
            stream.Seek(arrayIndex, System.IO.SeekOrigin.Begin);
            context.UpdateSubresource(new DataBox(stream.DataPointer, 0, dataSize), buffer, 0, new ResourceRegion()
            {
                Left = dataSize * index,
                Top = 0,
                Front = 0,
                Right = dataSize * (index + count),
                Bottom = 1,
                Back = 1
            });
        }

        /// <summary>
        /// Copies a range of data to an array
        /// </summary>
        /// <param name="index">Index from which to start copying</param>
        /// <param name="count">Number of elements to copy</param>
        /// <param name="array">Destination array to copy to</param>
        /// <param name="arrayIndex">Destination array index to copy to</param>
        public void CopyRangeTo(int index, int count, T[] array, int arrayIndex)
        {
            if (count == 0)
                return;

            if (count < 0)
                throw new IndexOutOfRangeException();
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException();
            if (index + count < 0 || index + count > Count)
                throw new IndexOutOfRangeException();

            if (array == null)
                throw new ArgumentNullException();
            if (arrayIndex < 0 || arrayIndex >= array.Length)
                throw new IndexOutOfRangeException();
            if (arrayIndex + array.Length < 0 || arrayIndex + count > array.Length)
                throw new IndexOutOfRangeException();

            int dataSize = Utilities.SizeOf<T>();
            Buffer stagingBuffer = new Buffer(context.Device, dataSize * count, ResourceUsage.Staging, BindFlags.None, CpuAccessFlags.Read, ResourceOptionFlags.None, 0);

            context.CopySubresourceRegion(buffer, 0, new ResourceRegion
            {
                Left = dataSize * index,
                Top = 0,
                Front = 0,
                Right = dataSize * (index + count),
                Bottom = 1,
                Back = 1
            }, stagingBuffer, 0, 0, 0, 0);

            DataStream stream;
            context.MapSubresource(stagingBuffer, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out stream);
            stream.ReadRange<T>(array, arrayIndex, count);
            context.UnmapSubresource(stagingBuffer, 0);

            stagingBuffer.Dispose();
        }

        /// <summary>
        /// Copies a range of data to another GPUList
        /// </summary>
        /// <param name="index">Index from which to start copying</param>
        /// <param name="count">Number of elements to copy</param>
        /// <param name="list">Destination list to copy to</param>
        /// <param name="listIndex">Destination list index to copy to</param>
        public void CopyRangeTo(int index, int count, StructuredBuffer<T> list, int listIndex)
        {
            if (count == 0)
                return;

            if (list == this)
                throw new Exception("Cannot copy range into self...yet");

            if (count < 0)
                throw new IndexOutOfRangeException();
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException();
            if (index + count < 0 || index + count > Count)
                throw new IndexOutOfRangeException();

            if (list == null)
                throw new ArgumentNullException();
            if (listIndex < 0 || listIndex >= list.Count)
                throw new IndexOutOfRangeException();
            if (listIndex + list.Count < 0 || listIndex + count > list.Count)
                throw new IndexOutOfRangeException();

            int dataSize = Utilities.SizeOf<T>();
            context.CopySubresourceRegion(buffer, 0,
                new ResourceRegion
                {
                    Left = dataSize * index,
                    Top = 0,
                    Front = 0,
                    Right = dataSize * (index + count),
                    Bottom = 1,
                    Back = 1
                }, list.buffer, 0, dataSize * listIndex, 0, 0);
        }

        /// <summary>
        /// Copys the list to another GPUList
        /// </summary>
        /// <param name="array">The list to copy data to</param>
        /// <param name="arrayIndex">The starting index in the array</param>
        public void CopyTo(StructuredBuffer<T> list, int listIndex)
        {
            CopyRangeTo(0, count, list, listIndex);
        }

        /// <summary>
        /// Trims excess capacity on the internal GPU buffer to match the count
        /// </summary>
        public void TrimExcess()
        {
            setCapacity(Count);
        }

        /// <summary>
        /// Returns a copy of the list into an array
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] data = new T[Count];
            CopyTo(data, 0);
            return data;
        }

        /// <summary>
        /// Sets the internal capacity of the list
        /// </summary>
        /// <param name="newCapacity"></param>
        private void setCapacity(int newCapacity)
        {
            if (newCapacity < 0)
                throw new ArgumentOutOfRangeException();

            if (newCapacity == capacity)
                return;

            if (newCapacity == 0)
            {
                if (unorderedAccessView != null)
                    unorderedAccessView.Dispose();
                if (shaderResourceView != null)
                    shaderResourceView.Dispose();
                if (buffer != null)
                    buffer.Dispose();

                unorderedAccessView = null;
                shaderResourceView = null;
                buffer = null;

                capacity = count = 0;
                return;
            }

            int dataSize = Utilities.SizeOf<T>();
            Buffer newBuffer = new Buffer
                (context.Device,
                dataSize * newCapacity,
                ResourceUsage.Default,
                bindFlags,
                CpuAccessFlags.None,
                miscFlags,
                dataSize);

            int sizeToCopy = Math.Min(newCapacity, Count);
            if (sizeToCopy > 0)
                context.CopySubresourceRegion(buffer, 0, new ResourceRegion { Left = 0, Top = 0, Front = 0, Right = dataSize * sizeToCopy, Bottom = 1, Back = 1 }, newBuffer, 0, 0, 0, 0);

            count = sizeToCopy;
            capacity = newCapacity;

            if (unorderedAccessView != null)
                unorderedAccessView.Dispose();
            if (shaderResourceView != null)
                shaderResourceView.Dispose();
            if (buffer != null)
                buffer.Dispose();

            buffer = newBuffer;

            if (miscFlags.HasFlag(ResourceOptionFlags.DrawIndirectArguments))
            {
                unorderedAccessView = new UnorderedAccessView(context.Device, buffer, new UnorderedAccessViewDescription
                {
                    Format = SharpDX.DXGI.Format.R32_UInt,
                    Dimension = UnorderedAccessViewDimension.Buffer,
                    Buffer = new UnorderedAccessViewDescription.BufferResource { FirstElement = 0, ElementCount = 5, Flags = 0 }
                });
            }
            else
                unorderedAccessView = new UnorderedAccessView(context.Device, buffer);

            if(bindFlags.HasFlag(BindFlags.ShaderResource))
                shaderResourceView = new ShaderResourceView(context.Device, buffer);
        }

        #endregion GPUList Members

        #region IDisposable Members

        /// <summary>
        /// Disposes the list by destroying all GPU resources used
        /// </summary>
        public void Dispose()
        {
            setCapacity(0);
        }

        #endregion IDisposable Members

        #region IList<T> Members

        /// <summary>
        /// Adds one item to the end of the list
        /// </summary>
        /// <param name="item">The item to add</param>
        public void Add(T item)
        {
            T[] array = new T[] { item };
            AddRange(array);
        }

        /// <summary>
        /// Gets the index in the list of the first item found
        /// </summary>
        /// <param name="item">The item whose index is searched for</param>
        /// <returns>Index of the item found, or -1 if not found</returns>
        public int IndexOf(T item)
        {
            T[] data = new T[count];
            CopyTo(data, 0);

            for (int i = 0; i < data.Length; i++)
                if (data[i].Equals(item))
                    return i;
            return -1;
        }

        /// <summary>
        /// Inserts an item at a specific index
        /// </summary>
        /// <param name="index">The index in the list to insert the item</param>
        /// <param name="item">The item to insert</param>
        public void Insert(int index, T item)
        {
            T[] array = new T[] { item };
            InsertRange(index, array);
        }

        /// <summary>
        /// Removes an item at a specific index
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        /// <summary>
        /// Gets or Sets an element at some index in the list
        /// </summary>
        /// <param name="index">Index in the list to get or set</param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                    throw new IndexOutOfRangeException();

                T[] array = new T[1];
                CopyRangeTo(index, 1, array, 0);
                return array[0];
            }
            set
            {
                if (index < 0 || index >= count)
                    throw new IndexOutOfRangeException();

                T[] array = new T[] { value };
                SetRange(index, array, 0, 1);
            }
        }

        #endregion IList<T> Members

        #region ICollection<T> Members

        /// <summary>
        /// Clears the list of all elements
        /// </summary>
        public void Clear()
        {
            count = 0;
        }

        /// <summary>
        /// Returns true if the item is contained in the list, or false if not
        /// </summary>
        /// <param name="item">The item to check containment for</param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            foreach (T i in this)
                if (i.Equals(item))
                    return true;
            return false;
        }

        /// <summary>
        /// Copys the list to an array
        /// </summary>
        /// <param name="array">The array to copy data to</param>
        /// <param name="arrayIndex">The starting index in the array</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyRangeTo(0, count, array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of items in the list
        /// </summary>
        public int Count { get { return count; } }

        /// <summary>
        /// Returns whether or not the list is read only
        /// </summary>
        public bool IsReadOnly { get { return false; } }

        /// <summary>
        /// Removes an item from the list. Only the first item found is removed
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>True if the item is removed, false otherwise</returns>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
                RemoveAt(index);
            return index >= 0;
        }

        #endregion ICollection<T> Members

        #region IEnumerable<T> Members

        /// <summary>
        /// Gets the enumerator for this list. Any changes made while enumerating will not be reflected
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            T[] array = new T[count];
            CopyTo(array, 0);
            return array.AsEnumerable<T>().GetEnumerator();
        }

        #endregion IEnumerable<T> Members

        #region IEnumerable Members

        /// <summary>
        /// Gets the enumerator for this list. Any changes made while enumerating will not be reflected
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IEnumerable Members
    }
}