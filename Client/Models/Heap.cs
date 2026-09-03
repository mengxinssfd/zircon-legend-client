using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Models
{
    public class Heap<T> where T : IHeapItem<T>
    {
        private T[] items;
        private int currentItemCount;

        public Heap(int maxHeapSize)
        {
            items = new T[maxHeapSize];
        }

        // ！ 修复：清空堆以复用数组，避免每次寻路都分配 MaxSize 大小的数组导致 GC 压力
        public void Reset()
        {
            currentItemCount = 0;
        }

        public bool Contains(T item)
        {
            // ！ 修复：节点 HeapIndex 可能在复用场景下为 -1 或越界，先做边界保护再取值
            int index = item.HeapIndex;
            if (index < 0 || index >= currentItemCount) return false;
            return Equals(items[index], item);
        }

        public void Add(T item)
        {
            item.HeapIndex = currentItemCount;
            items[currentItemCount] = item;
            SortUp(item);
            ++currentItemCount;
        }

        public T RemoveFirst()
        {
            T obj = items[0];
            --currentItemCount;
            items[0] = items[currentItemCount];
            items[0].HeapIndex = 0;
            SortDown(items[0]);
            return obj;
        }

        public void UpdateItem(T item)
        {
            SortUp(item);
        }

        public int Count
        {
            get
            {
                return currentItemCount;
            }
        }

        private void SortDown(T item)
        {
            while (true)
            {
                int index1 = item.HeapIndex * 2 + 1;
                int index2 = item.HeapIndex * 2 + 2;
                if (index1 < currentItemCount)
                {
                    int index3 = index1;
                    if (index2 < currentItemCount && items[index1].CompareTo(items[index2]) < 0)
                        index3 = index2;
                    if (item.CompareTo(items[index3]) < 0)
                        Swap(item, items[index3]);
                    else
                        goto label_6;
                }
                else
                    break;
            }
            return;
        label_6:;
        }

        private void SortUp(T item)
        {
            int index = (item.HeapIndex - 1) / 2;
            while (true)
            {
                T obj = items[index];
                if (item.CompareTo(obj) > 0)
                {
                    Swap(item, obj);
                    index = (item.HeapIndex - 1) / 2;
                }
                else
                    break;
            }
        }

        private void Swap(T itemA, T itemB)
        {
            items[itemA.HeapIndex] = itemB;
            items[itemB.HeapIndex] = itemA;
            int heapIndex = itemA.HeapIndex;
            itemA.HeapIndex = itemB.HeapIndex;
            itemB.HeapIndex = heapIndex;
        }
    }
}
