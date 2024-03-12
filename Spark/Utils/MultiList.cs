using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Spark
{
    public class MultiList<T> : IReadOnlyList<T>
    {
        private readonly List<List<T>> lists;

        public MultiList(params List<T>[] lists)
        {
            this.lists = lists.ToList();
        }

        public void Add(List<T> list)
        {
            lists.Add(list);
        }

        public void Clear()
        {
            lists.Clear();
        }

        public T this[int index]
        {
            get
            {
                int currentCount = 0;
                foreach (var list in lists)
                {
                    if (index < currentCount + list.Count)
                    {
                        return list[index - currentCount];
                    }
                    currentCount += list.Count;
                }

                throw new IndexOutOfRangeException();
            }
        }

        public int Count => lists.Sum(list => list.Count);

        public IEnumerator<T> GetEnumerator()
        {
            return new MultiListEnumerator(lists);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class MultiListEnumerator : IEnumerator<T>
        {
            private readonly List<List<T>> lists;
            private int currentListIndex;
            private int currentIndex;

            public MultiListEnumerator(List<List<T>> lists)
            {
                this.lists = lists;
                Reset();
            }

            public T Current => lists[currentListIndex][currentIndex];

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public bool MoveNext()
            {
                if (currentListIndex >= lists.Count - 1 && currentIndex >= lists[currentListIndex].Count - 1)
                    return false;

                currentIndex++;
                if (currentIndex >= lists[currentListIndex].Count)
                {
                    currentIndex = 0;
                    currentListIndex++;
                }

                return true;
            }

            public void Reset()
            {
                currentListIndex = 0;
                currentIndex = -1;
            }
        }
    }
}