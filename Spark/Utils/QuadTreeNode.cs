using System;
using SharpDX;

namespace Spark
{
    public delegate void NodeCallback(QuadTreeNode node);

    //public enum QuadLocation : int
    //{
    //    QL_NW = 0,
    //    QL_NE,
    //    QL_SW,
    //    QL_SE,
    //    QL_ROOT
    //};

    //public enum QuadNeighborDirection : int
    //{
    //    QND_NORTH = 0,
    //    QND_EAST = 1,
    //    QND_SOUTH = 2,
    //    QND_WEST = 3
    //};

    public class QuadTreeNode
    {
        private const int QL_NW = 0;
        private const int QL_NE = 1;
        private const int QL_SW = 2;
        private const int QL_SE = 3;

        private const int QND_NORTH = 0;
        private const int QND_EAST = 1;
        private const int QND_SOUTH = 2;
        private const int QND_WEST = 3;

        private static int[] QuadNeighborDirectionOpposite =
        {
            QND_SOUTH,
            QND_WEST,
            QND_NORTH,
            QND_EAST
        };

        private static int[,] QuadChildrenDirections =
        {
            {QL_NW, QL_NE}, // NORTH
            {QL_NE, QL_SE}, // EAST
            {QL_SW, QL_SE}, // SOUTH
            {QL_NW, QL_SW}  // WEST
        };

        private static int[,] QuadChildrenOppositeDirections =
        {
            {QL_SW, QL_SE}, // NORTH Opposite
            {QL_NW, QL_SW}, // EAST Opposite
            {QL_NW, QL_NE}, // SOUTH Opposite
            {QL_NE, QL_SE}
        };

        public static int MaxDepth = 7;

        public static event NodeCallback OnSplit;

        public static event NodeCallback OnMerged;

        public static NodeCallback OnDraw;
        public static float SplitDistance = 2f;

        public QuadTreeNode[] Children = new QuadTreeNode[4];
        public QuadTreeNode[] Neighbors = new QuadTreeNode[4];

        private int _depth;
        private int _location;

        public object Payload;

        public Vector3 Position;
        public Vector3 Extents;
        public BoundingBox Bounds;

        public int Depth { get { return _depth; } }
        public int Location { get { return _location; } }

        private QuadTreeNode GetChild(int index)
        {
            return Children[index];
        }

        private void SetNeighbor(QuadTreeNode node, int index)
        {
            Neighbors[index] = node;
        }

        private bool HasChildren()
        {
            for (int i = 0; i < 4; i++)
            {
                if (Children[i] != null) return true;
            }

            return false;
        }

        public void Update(Vector3 view)
        {
            if (HasChildren())
            {
                for (int i = 0; i < 4; i++)
                    Children[i].Update(view);
            }

            if (IsReadyToUnsplit(view))
            {
                UnsplitNode();
                return;
            }
            else if (IsReadyToSplit(view))
            {
                SplitNode();

                for (int i = 0; i < 4; i++)
                    Children[i].Update(view);
            }
        }

        private bool LODCheck(Vector3 view)
        {
            Vector3 extents = Extents; extents.Y = 0;
            float diagonal = extents.LengthSquared();
            Vector3 center = Bounds.Center; // Position;
            if (view.Y < 600)
            {
                view.Y = center.Y = 0;
            }
            //center.Y = 0;

            // I check here for LOD calculation splitting
            return Vector3.DistanceSquared(view, center) < diagonal * SplitDistance;
        }

        private bool IsReadyToSplit(Vector3 view)
        {
            if (HasChildren() || _depth == MaxDepth)
                return false;

            return LODCheck(view);
        }

        private bool IsReadyToUnsplit(Vector3 view)
        {
            if (HasChildren() == false)
                return false;

            QuadTreeNode neighbor;

            for (int i = 0; i < 4; i++)
            {
                neighbor = Neighbors[i];

                if (neighbor != null && neighbor.HasChildren() && neighbor._depth == _depth)
                {
                    if (
                        neighbor.GetChild(QuadChildrenOppositeDirections[i, 0]).HasChildren() ||
                        neighbor.GetChild(QuadChildrenOppositeDirections[i, 1]).HasChildren())
                        return false;
                }
            }

            return !LODCheck(view);
        }

        public QuadTreeNode(Vector3 position, Vector3 extents, int location, int depth)
        {
            _depth = depth;
            _location = location;
            Position = position;

            Extents = extents;
            Bounds = new BoundingBox(position - extents - new Vector3(0, 10000, 0), position + extents + new Vector3(0, 10000, 0));
        }

        public void Render(Action<QuadTreeNode> action)
        {
            if (HasChildren())
            {
                for (int i = 0; i < 4; i++)
                    Children[i].Render(action);
            }
            else
            {
                if (OnDraw != null) OnDraw(this);

                action.Invoke(this);
            }
        }

        public void Render(Action<QuadTreeNode, RenderPass> action, RenderPass flag)
        {
            if (HasChildren())
            {
                for (int i = 0; i < 4; i++)
                    Children[i].Render(action, flag);
            }
            else
            {
                action.Invoke(this, flag);
            }
        }

        public QuadTreeNode(Vector3 position, Vector3 extents)
        {
            _depth = 0;
            _location = 0;
            Position = position;
            Extents = extents;
            Bounds = new BoundingBox(position - extents, position + extents);
        }

        public QuadTreeNode GetNodeAt(Vector3 pos)
        {
            pos.Y = Bounds.Minimum.Y;

            if (Bounds.Contains(ref pos) != ContainmentType.Contains)
                return null;

            QuadTreeNode result = this;

            for (int i = 0; i < 4; i++)
            {
                if (Children[i] != null)
                {
                    result = Children[i].GetNodeAt(pos);
                    if (result != null) return result;
                }
            }

            return result;
        }

        public void Draw()
        {
            if (HasChildren())
            {
                for (int i = 0; i < 4; i++)
                    Children[i].Draw();
            }
            else
            {
                if (OnDraw != null) OnDraw(this);
                Graphics.Lines.Draw(Bounds, new Vector4(.5f, .5f, .5f, 1));

                if (_location == 0) // NW
                {
                    // find north neighbor
                    // find west neighbor
                    // east neighbor is mNeighbors[1]
                    // south neighbor is mNeighbors[2]
                }
                else if (_location == 1) // NE
                {
                    // find north neighbor
                    // find east neighbor
                    // west neighbor is mNeighbors[3]
                    // south neighbor is mNeighbors[2]
                }
                else if (_location == 2) // SE
                {
                    // find south neighbor
                    // find east neighbor
                    // west neighbor is mNeighbors[3]
                    // north neighbor is mNeighbors[0]
                }
                else if (_location == 3) // SW
                {
                    // find south neighbor
                    // find west neighbor
                    // east neighbor is mNeighbors[1]
                    // north neighbor is mNeighbors[0]
                }
            }
        }

        private void SplitNode()
        {
            Vector3 offset = Extents / 2.0f;

            //for (int i = 0; i < 4; i++)
            //    mChildren[i] = new QuadNode2(Position + new Vector3(-1 * offset.X, 0, -1 * offset.Z), Extents / 2, i, mDepth + 1);

            Children[0] = new QuadTreeNode(Position + new Vector3(-1 * offset.X, 0, -1 * offset.Z), Extents / 2, 0, _depth + 1);
            Children[1] = new QuadTreeNode(Position + new Vector3(+1 * offset.X, 0, -1 * offset.Z), Extents / 2, 1, _depth + 1);
            Children[2] = new QuadTreeNode(Position + new Vector3(-1 * offset.X, 0, +1 * offset.Z), Extents / 2, 2, _depth + 1);
            Children[3] = new QuadTreeNode(Position + new Vector3(+1 * offset.X, 0, +1 * offset.Z), Extents / 2, 3, _depth + 1);

            QuadTreeNode neighbor = null;

            for (int i = 0; i < 4; i++)
            {
                neighbor = Neighbors[i];

                // This isn't 100% needed, but it's a simple speed-up - just skips checking internal neighborhood stuff
                if (i != QuadNeighborDirectionOpposite[i] && neighbor != null)
                {
                    if (neighbor._depth + 1 == _depth && !neighbor.HasChildren())
                        neighbor.SplitNode();
                }
            }

            int reverseDirection;
            QuadTreeNode[] myChild = { null, null };
            QuadTreeNode[] neighborChild = { null, null };

            for (int i = 0; i < 4; i++)
            {
                neighbor = Neighbors[i];

                if (neighbor != null)
                {
                    reverseDirection = QuadNeighborDirectionOpposite[i];
                    myChild[0] = GetChild(QuadChildrenDirections[i, 0]);
                    myChild[1] = GetChild(QuadChildrenDirections[i, 1]);

                    if (neighbor.HasChildren() && (neighbor._depth == _depth))
                    {
                        neighborChild[0] = neighbor.GetChild(QuadChildrenOppositeDirections[i, 0]);
                        neighborChild[1] = neighbor.GetChild(QuadChildrenOppositeDirections[i, 1]);

                        myChild[0].SetNeighbor(neighborChild[0], i);
                        myChild[1].SetNeighbor(neighborChild[1], i);

                        neighborChild[0].SetNeighbor(myChild[0], reverseDirection);
                        neighborChild[1].SetNeighbor(myChild[1], reverseDirection);

                        neighbor.SetNeighbor(this, reverseDirection);
                    }
                    else if (!neighbor.HasChildren() && neighbor._depth == _depth)
                    {
                        myChild[0].SetNeighbor(neighbor, i);
                        myChild[1].SetNeighbor(neighbor, i);

                        neighbor.SetNeighbor(this, reverseDirection);
                    }
                    else if (neighbor._depth + 1 == _depth && neighbor.HasChildren())
                    {
                        neighborChild[0] = neighbor.GetChild(QuadChildrenOppositeDirections[i, 0]);
                        neighborChild[1] = neighbor.GetChild(QuadChildrenOppositeDirections[i, 1]);

                        if (_location == QuadChildrenDirections[i, 0])
                        {
                            myChild[0].SetNeighbor(neighborChild[0], i);
                            myChild[1].SetNeighbor(neighborChild[0], i);

                            neighborChild[0].SetNeighbor(this, reverseDirection);
                            SetNeighbor(neighborChild[0], i);
                        }
                        else if (_location == QuadChildrenDirections[i, 1])
                        {
                            myChild[0].SetNeighbor(neighborChild[1], i);
                            myChild[1].SetNeighbor(neighborChild[1], i);

                            neighborChild[1].SetNeighbor(this, reverseDirection);
                            SetNeighbor(neighborChild[1], i);
                        }
                    }
                }
            }

            Children[QL_NW].SetNeighbor(Children[QL_NE], QND_EAST);
            Children[QL_NE].SetNeighbor(Children[QL_NW], QND_WEST);

            Children[QL_NW].SetNeighbor(Children[QL_SW], QND_SOUTH);
            Children[QL_SW].SetNeighbor(Children[QL_NW], QND_NORTH);

            Children[QL_SW].SetNeighbor(Children[QL_SE], QND_EAST);
            Children[QL_SE].SetNeighbor(Children[QL_SW], QND_WEST);

            Children[QL_SE].SetNeighbor(Children[QL_NE], QND_NORTH);
            Children[QL_NE].SetNeighbor(Children[QL_SE], QND_SOUTH);

            OnSplit?.Invoke(this);
        }

        private void UnsplitNode()
        {
            QuadTreeNode neighbor = null;
            int reverseDirection;

            for (int i = 0; i < 4; i++)
            {
                neighbor = Neighbors[i];

                if (neighbor != null)
                {
                    reverseDirection = QuadNeighborDirectionOpposite[i];

                    if (neighbor.HasChildren() && neighbor._depth == _depth)
                    {
                        neighbor.GetChild(QuadChildrenOppositeDirections[i, 0]).SetNeighbor(this, reverseDirection);
                        neighbor.GetChild(QuadChildrenOppositeDirections[i, 1]).SetNeighbor(this, reverseDirection);
                        neighbor.SetNeighbor(this, reverseDirection);
                    }
                    else if (neighbor.HasChildren() == false && neighbor._depth == _depth)
                    {
                        neighbor.SetNeighbor(this, reverseDirection);
                    }
                }
            }

            for (int i = 0; i < 4; i++)
                Children[i] = null;

            OnMerged?.Invoke(this);
        }
    }
}