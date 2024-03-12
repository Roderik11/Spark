using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace Spark
{
    public interface IIndex
    {
        int GetIndex();
    }

    /// <summary>
    /// QuickSet provides fast insertion, removal and uniqueness
    /// </summary>
    public class QuickSet<T> where T : class, IIndex
    {
        private readonly List<T> list = new List<T>();
        private readonly Dictionary<int, int> indices = new Dictionary<int, int>();

        public ReadOnlyCollection<T> Collection;

        public int Count { get { return list.Count; } }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public QuickSet()
        {
            Collection = new ReadOnlyCollection<T>(list);
        }

        public T this[int index]
        {
            get { return list.Count > index ? list[index] : null; }
        }

        public bool TryGetValue(int itemId, out T result)
        {
            if (indices.TryGetValue(itemId, out int index))
            {
                result = list[index];
                return true;
            }

            result = null;
            return false;
        }

        public bool Add(T item)
        {
            if (item == null) return false;

            var index = item.GetIndex();
            if (indices.TryGetValue(index, out _))
                return false;

            indices.Add(index, list.Count);
            list.Add(item);
            return true;
        }

        public void AddUnsafe(T item)
        {
            if (item == null) return;

            var index = item.GetIndex();
            indices.Add(index, list.Count);
            list.Add(item);
        }

        public void Add(IEnumerable<T> collection)
        {
            foreach (var ent in collection)
                Add(ent);
        }

        public bool Remove(T item)
        {
            if (item == null) return false;

            var index = item.GetIndex();

            if (indices.TryGetValue(index, out int id))
            {
                FastRemove(id);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            if (Count < 1) return;

            list.Clear();
            indices.Clear();
        }

        public bool Contains(T item)
        {
            if (item == null) return false;

            return indices.ContainsKey(item.GetIndex());
        }

        void FastRemove(int index)
        {
            int last = list.Count - 1;
            if (index > last) return;

            var a = list[index];
            var b = list[last];

            indices[b.GetIndex()] = index;
            indices.Remove(a.GetIndex());

            list[index] = b;
            list[last] = a;

            list.RemoveAt(last);
        }
    }

    public class EntitySet : QuickSet<Entity> { }

}
