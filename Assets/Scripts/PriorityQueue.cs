using System;
using System.Collections.Generic;
using Global.Timer.Utils;

namespace Global.Timer
{
    public class PriorityQueue<T> where T : IComparable<T>
    {
        private static long _count = long.MinValue;

        private const long ID_MASK = long.MaxValue;

        private IndexedItem[] _items;
        private int _size;
        
#if UNITY_EDITOR
        /// <summary>
        /// 에디터 상에서의 디버깅 용도로만 사용하자.
        /// </summary>
        public List<IndexedItem> indexedItemList;
#endif 

        public PriorityQueue()
            : this(100)
        {
#if UNITY_EDITOR
            indexedItemList = new List<IndexedItem>(100);
#endif
        }

        public PriorityQueue(int capacity)
        {
            _items = new IndexedItem[capacity];
            _size = 0;
        }

        private bool IsHigherPriority(int left, int right)
        {
            return _items[left].CompareTo(_items[right]) < 0;
        }

        private void Percolate(int index)
        {
            if (index >= _size || index < 0)
                return;
            var parent = (index - 1) / 2;
            if (parent < 0 || parent == index)
                return;

            if (IsHigherPriority(index, parent))
            {
                var temp = _items[index];
                _items[index] = _items[parent];
                _items[parent] = temp;
                Percolate(parent);
            }
        }

        private void Heapify(int index)
        {
            if (index >= _size || index < 0)
                return;

            var left = 2 * index + 1;
            var right = 2 * index + 2;
            var first = index;

            if (left < _size && IsHigherPriority(left, first))
                first = left;
            if (right < _size && IsHigherPriority(right, first))
                first = right;
            if (first != index)
            {
                var temp = _items[index];
                _items[index] = _items[first];
                _items[first] = temp;
                Heapify(first);
            }
        }

        public int Count { get { return _size; } }

        public T Peek()
        {
            if (_size == 0)
            {
                Debugs.LogError("Heap이 비었다.");
                return default(T);
            }

            return _items[0].Value;
        }

        private void RemoveAt(int index)
        {
#if UNITY_EDITOR
            indexedItemList.Remove(_items[index]);
#endif
            
            _items[index] = _items[--_size];
            _items[_size] = default(IndexedItem);
            Heapify(0);
            /*
            if (_size < _items.Length / 4)
            {
                var temp = _items;
                _items = new IndexedItem[_items.Length / 2];
                Array.Copy(temp, 0, _items, 0, _size);
            }
            */
        }

        public void Clear()
        {
            _items = new IndexedItem[_items.Length];
            _size = 0;
            
#if UNITY_EDITOR
            indexedItemList.Clear();
#endif
        }

        public T Dequeue()
        {
            var result = Peek();
            RemoveAt(0);
            return result;
        }

        public void Enqueue(T item)
        {
            if (_size >= _items.Length)
            {
                var temp = _items;
                _items = new IndexedItem[_items.Length * 2];
                Array.Copy(temp, _items, temp.Length);
            }

            var index = _size++;
            // 애초에 큐에 long 개 이상의 task가 들어 있으면 문제가 있다..
            _items[index] = new IndexedItem { Value = item, Id = (++_count) & ID_MASK };
            
#if UNITY_EDITOR
            indexedItemList.Add(_items[index]);
            indexedItemList.Sort((lhs, rhs) => lhs.CompareTo(rhs));
#endif
            
            Percolate(index);
        }

        // 만일을 위해 두었지만, 안쓰는게 낫다.
        public bool Remove(T item)
        {
            for (var i = 0; i < _size; ++i)
            {
                if (EqualityComparer<T>.Default.Equals(_items[i].Value, item))
                {
                    RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public struct IndexedItem : IComparable<IndexedItem>
        {
            public T Value;
            public long Id;

            public int CompareTo(IndexedItem other)
            {
                var c = Value.CompareTo(other.Value);
                if (c == 0)
                    c = Id.CompareTo(other.Id);
                return c;
            }
        }
    }
}
