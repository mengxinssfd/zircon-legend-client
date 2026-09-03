using Client.Scenes.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Client.Models
{
    public class PathFinder
    {
        private Node[,] Grid;
        public MapControl Map;

        // ！ 修复：复用堆/集合/邻居缓冲区，避免每次寻路分配 MaxSize 大数组与 HashSet，大幅降低挂机寻路的 GC 压力
        private int _searchId;
        private readonly Heap<Node> _heap;
        private readonly HashSet<Node> _closedSet = new HashSet<Node>();
        private readonly List<Node> _neighbours = new List<Node>(8);

        public int MaxSize
        {
            get
            {
                return Map.Width * Map.Height;
            }
        }

        public PathFinder(MapControl map)
        {
            Map = map;
            CreateGrid();
            _heap = new Heap<Node>(MaxSize);
        }

        private void CreateGrid()
        {
            Grid = new Node[Map.Width, Map.Height];
            for (int x = 0; x < Map.Width; ++x)
            {
                for (int y = 0; y < Map.Height; ++y)
                    Grid[x, y] = new Node(Map, x, y);
            }
        }

        public List<Node> FindPath(Point start, Point target, int maxExpansions = 65536)
        {
            Node node1 = GetNode(start);
            Node node2 = GetNode(target);
            _searchId++;
            _heap.Reset();
            _closedSet.Clear();

            // 起始点初始化
            Touch(node1);
            node1.GCost = 0;
            node1.HCost = GetDistance(node1, node2);
            node1.Parent = null;
            _heap.Add(node1);

            int expanded = 0;
            while (_heap.Count > 0)
            {
                Node node3 = _heap.RemoveFirst();
                node3.HeapIndex = -1;             // 已移出堆，避免复用场景下被误判在堆中
                _closedSet.Add(node3);
                // ！ 修复：限制最大展开节点数。目标不可达时 A* 会展开整块连通区域导致数百毫秒卡顿，
                // 展开超过上限即视为不可达快速返回。挂机范围 ≤25 格，正当短路径远用不到此上限。
                if (++expanded > maxExpansions)
                    return (List<Node>)null;
                if (node3 == node2)
                    return RetracePath(node1, node2);

                FillNeighbours(node3);
                foreach (Node neighbour in _neighbours)
                {
                    // 已走到(closed)的节点不再展开，合法的未到位邻居略过
                    if (!neighbour.Walkable || _closedSet.Contains(neighbour)) continue;

                    Touch(neighbour); // 本回合首次触碰时重置陈旧成本/Parent/HeapIndex

                    int num = node3.GCost + GetDistance(node3, neighbour);
                    if (num < neighbour.GCost || !_heap.Contains(neighbour))
                    {
                        neighbour.GCost = num;
                        neighbour.HCost = GetDistance(neighbour, node2);
                        neighbour.Parent = node3;
                        if (!_heap.Contains(neighbour))
                            _heap.Add(neighbour);
                        else
                            _heap.UpdateItem(neighbour);
                    }
                }
            }
            return (List<Node>)null;
        }

        // ！ 修复：仅当节点本回合尚未被触碰时才重置，防止上一轮寻路遗留的成本污染本次结果
        private void Touch(Node node)
        {
            if (node.VisitStamp == _searchId) return;
            node.VisitStamp = _searchId;
            node.HeapIndex = -1;
            node.GCost = 0;
            node.HCost = 0;
            node.Parent = null;
        }

        private void FillNeighbours(Node node)
        {
            _neighbours.Clear();
            for (int index1 = -1; index1 <= 1; ++index1)
            {
                for (int index2 = -1; index2 <= 1; ++index2)
                {
                    if (index1 != 0 || index2 != 0)
                    {
                        int index3 = node.Location.X + index1;
                        int index4 = node.Location.Y + index2;
                        if (index3 >= 0 && index3 < Grid.GetLength(0) && index4 >= 0 && index4 < Grid.GetLength(1))
                            _neighbours.Add(Grid[index3, index4]);
                    }
                }
            }
        }

        public List<Node> RetracePath(Node startNode, Node endNode)
        {
            List<Node> nodeList = new List<Node>();
            for (Node node = endNode; node != startNode; node = node.Parent)
                nodeList.Add(node);
            nodeList.Add(startNode);
            nodeList.Reverse();
            return nodeList;
        }

        private int GetDistance(Node nodeA, Node nodeB)
        {
            int num1 = Math.Abs(nodeA.Location.X - nodeB.Location.X);
            int num2 = Math.Abs(nodeA.Location.Y - nodeB.Location.Y);
            if (num1 > num2)
                return 14 * num2 + 10 * (num1 - num2);
            return 14 * num1 + 10 * (num2 - num1);
        }

        private Node GetNode(Point location)
        {
            var x = location.X >= Map.Width ? Map.Width - 1 : location.X;
            var y = location.Y >= Map.Height ? Map.Height - 1 : location.Y;

            if (x < 0) x = 0;
            if (y < 0) y = 0;

            return Grid[x, y];
        }
    }
}
