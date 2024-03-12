using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
    public class Pool
    {
        public static event Action OnReset;

        public static void Reset()
        {
            OnReset?.Invoke();
        }
    }

    public class Pool<T> : Pool where T : new()
    {
        private readonly Queue<T> _queue = new Queue<T>();

        private Func<T> getCallback;
        private Action<T> releaseCallback;

        private int countNew;

        public int CountActive { get { return countNew - _queue.Count; } }
        public int CountPool { get { return _queue.Count; } }

        public Pool() { }

        public Pool(Func<T> getCallback)
        {
            this.getCallback = getCallback;
        }

        public Pool(Func<T> getCallback, Action<T> releaseCallback)
        {
            this.getCallback = getCallback;
            this.releaseCallback = releaseCallback;
        }

        public virtual void FillPool(int count)
        {
            for (int i = 0; i < count; ++i)
                Release(CreateElement());
        }

        public virtual T Get()
        {
            T element;

            while (_queue.Count > 0)
            {
                element = _queue.Dequeue();

                if (element != null)
                {
                    return element;
                }
            }

            element = CreateElement();

            countNew++;
            return element;
        }

        protected virtual T CreateElement()
        {
            T element;
            if (getCallback != null)
            {
                element = getCallback();
            }
            else
            {
                element = new T();
            }
            return element;
        }

        public override string ToString()
        {
            return string.Format("{0} / {1}", CountActive, CountPool);
        }

        public virtual void ReleaseAndClearCollection(List<T> collection)
        {
            for (int i = 0; i < collection.Count; ++i)
                Release(collection[i]);

            collection.Clear();
        }

        public virtual void ReleaseAndClearCollection<K>(K[] collection) where K : class, T
        {
            for (int i = 0; i < collection.Length; ++i)
            {
                Release(collection[i]);
                collection[i] = null;
            }
        }

        public virtual void Release(T element)
        {
            if (element == null)
                return;

            //if (_stack.Count > 0 && ReferenceEquals(_stack.Peek(), element))
            //{
            //    // return;
            //    Debug.LogError("Trying to destroy object that is already released to pool.");
            //}

            releaseCallback?.Invoke(element);
            _queue.Enqueue(element);
        }
    }

}
