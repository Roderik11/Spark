using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;


namespace Spark
{
    public interface ISpatial : IComponent
    {
        //void UpdateVisibility();
        void UpdateBounds();
        BoundingBox BoundingBox { get; }
        BoundingSphere BoundingSphere { get; }
        Icoseptree SpatialNode { get; set; }
    }

    /// <summary>
    /// Class Icoseptree.
    /// Each object can either be in front of, behind, or overlapping each of the 3 bounding planes, 
    /// so there are three to the third power, or 27, different classifications. 
    /// </summary>
    public class Icoseptree
    {
        private Vector3 Min;
        private Vector3 Max;
        private Vector3 SplitPoint;
        private bool IsSplit;
        private bool AreExtendsCorrected;
        private int SplitValue = 16;
        private readonly Icoseptree Parent;

        public BoundingBox Bounds;
        public ContainmentType Containment;

        public HashSet<ISpatial> Objects = new HashSet<ISpatial>();
        public Dictionary<int, Icoseptree> Children = new Dictionary<int, Icoseptree>();

        public Icoseptree() { }

        public Icoseptree(Icoseptree parent)
        {
            Parent = parent;
        }

  
        public BoundingBox GetExtents(bool doSplit)
        {
            if (!AreExtendsCorrected)
                UpdateExtents();

            if (doSplit && !IsSplit && Objects.Count >= SplitValue)
                DoSplit();

            Bounds.Minimum = Min;
            Bounds.Maximum = Max;

            return Bounds;
        }

        public bool UpdateObject(ISpatial spatial, Vector3 oldp, float oldr)
        {
            float r = spatial.BoundingSphere.Radius;
            Vector3 rp = new Vector3(r, r, r);
            Vector3 minp = spatial.BoundingSphere.Center - rp;
            Vector3 maxp = spatial.BoundingSphere.Center + rp;
            Icoseptree node = this;

            while (node.Parent != null && !node.Contains(minp, maxp))
                node = node.Parent;

            // Find its final reinsertion point
            node = node.FindNode(spatial);
            if (node != this)
            {
                // Moved to a new node
                node.InsertNode(spatial);
                node.GrowExtents(minp, maxp);

                if (!RemoveObject(spatial, oldp, oldr))
                    return false;

                spatial.SpatialNode = node;
            }
            else
            {
                // Just update the extents of this node
                GrowExtents(minp, maxp);
                ShrinkExtents(oldp, oldr);
            }

            return true;
        }

        public Icoseptree AddObject(ISpatial spatial)
        {
            if (spatial == null) return null;

            var bounds = spatial.BoundingSphere;
            Vector3 center = bounds.Center;
            float radius = bounds.Radius;

            Icoseptree node = FindNode(ref bounds);
            node.InsertNode(spatial);

            Vector3 extents = new Vector3(radius, radius, radius);
            node.GrowExtents(center - extents, center + extents);
            return node;
        }

        public bool RemoveObject(ISpatial spatial)
        {
            return RemoveObject(spatial, spatial.BoundingSphere.Center, spatial.BoundingSphere.Radius);
        }

        public void Draw()
        {
            BoundingBox bounds = GetExtents(false);

            // Game.Screen2D.Draw_Box3D(bounds.Min, bounds.Max, -1);

            if (Objects.Count > 0)
                Graphics.Lines.Draw(bounds, new Vector4(.3f, .3f, .1f, 1));
            else if(Parent == null)
                Graphics.Lines.Draw(bounds, new Vector4(1, 1, 1, 1));

            foreach (var pair in Children)
                pair.Value.Draw();
        }

        private void UpdateExtents()
        {
            bool isSet = false;
            Vector3 newsplit = Vector3.Zero;
            int count = 0;
            Vector3 pos;
            Vector3 lo;
            Vector3 hi;
            float r;

            foreach (ISpatial node in Objects)
            {
                pos = node.BoundingSphere.Center;
                r = node.BoundingSphere.Radius;

                lo = new Vector3(pos.X - r, pos.Y - r, pos.Z - r);
                hi = new Vector3(pos.X + r, pos.Y + r, pos.Z + r);

                if (!isSet)
                {
                    Min = lo;
                    Max = hi;
                    isSet = true;
                }
                else
                {
                    Min = Vector3.Min(Min, lo);
                    Max = Vector3.Max(Max, hi);
                }

                newsplit += pos;
                count++;
            }

            foreach (var pair in Children)
            {
                var child = pair.Value;
                child.GetExtents(false);
                lo = child.Bounds.Minimum;
                hi = child.Bounds.Maximum;

                if (!isSet)
                {
                    Min = lo;
                    Max = hi;
                    isSet = true;
                }
                else
                {
                    Min = Vector3.Min(Min, lo);
                    Max = Vector3.Max(Max, hi);
                }

                newsplit += child.SplitPoint * SplitValue;
                count += SplitValue;
            }

            SplitPoint = newsplit / count;
            AreExtendsCorrected = true;
        }

        private void DoSplit()
        {
            if (IsSplit) return;

            // Find the split point
            SplitPoint = Vector3.Zero;
            float ttl = 0;
            foreach (ISpatial node in Objects)
            {
                Vector3 pos = node.BoundingSphere.Center;
                float r = node.BoundingSphere.Radius;
                float rw = 1 / (float)(r + 1E-12);

                SplitPoint += (pos + new Vector3(r, r, r)) * rw;
                ttl += rw;
            }

            SplitPoint /= ttl;
            IsSplit = true;

            bool safe = false, first = true;
            int lc = 0;

            List<ISpatial> here = new List<ISpatial>();
            foreach (ISpatial node in Objects)
            {
                var bounds = node.BoundingSphere;
                int c = WhichChild(ref bounds);
                // Make sure at least one object doesn’t go into the
                // same child as everyone else
                if (!safe)
                {
                    if (c < 0)
                        safe = true;
                    else
                    {
                        if (first)
                        {
                            lc = c;
                            first = false;
                        }
                        else
                            safe = (c != lc);
                    }
                }

                if (c < 0)
                {
                    here.Add(node);
                }
                else
                {
                    GetChild(c).InsertNode(node);
                }
            }

            Objects.Clear();
            foreach (ISpatial e in here)
                InsertNode(e);

            if (!safe)
            {
                // Oops, all objects went into the same child node!
                // Take them back.
                Icoseptree cn = GetChild(lc);

                foreach (ISpatial e in cn.Objects)
                    InsertNode(e);
                cn.Objects.Clear();

                //The last removal marked this node as unsplit, so
                //we’ll just have to do this all over again next time
                //this AABB is queried, unless we simply keep this
                //node split
                IsSplit = true;
            }

            foreach (var pair in Children)
                pair.Value.UpdateExtents();
        }

        private int WhichChild(ref BoundingSphere bounds)
        {
            if (!IsSplit) return -1;

            //#ifdef NONLEAFY
            //if (m_extentsCorrect)
            //{
            //    // If this object’s bounding volume is at least 1/8
            //    // the last known tree bounding volume, just keep it
            //    // here
            //    float sv = radius * 4 * (float)Math.PI / 3;
            //    float bv = (m_max.X - m_min.X) * (m_max.Y - m_min.Y) * (m_max.Z - m_min.Z);
            //    if (sv > bv / 8) return -1;
            //}
            //#endif

            ref var pos = ref bounds.Center;
            ref var radius = ref bounds.Radius;

            return
                (pos.X + radius < SplitPoint.X ? 1 : (pos.X - radius > SplitPoint.X ? 2 : 0)) +
                (pos.Y + radius < SplitPoint.Y ? 3 : (pos.Y - radius > SplitPoint.Y ? 6 : 0)) +
                (pos.Z + radius < SplitPoint.Z ? 9 : (pos.Z - radius > SplitPoint.Z ? 18 : 0));

            //int c = 0;

            //if (pos.X + radius < SplitPoint.X)
            //    c += 1;
            //else if (pos.X - radius > SplitPoint.X)
            //    c += 2;

            //if (pos.Y + radius < SplitPoint.Y)
            //    c += 3;
            //else if (pos.Y - radius > SplitPoint.Y)
            //    c += 6;

            //if (pos.Z + radius < SplitPoint.Z)
            //    c += 9;
            //else if (pos.Z - radius > SplitPoint.Z)
            //    c += 18;

            //return c;
        }

        private void InsertNode(ISpatial entity)
        {
            if (!Objects.Contains(entity))
                Objects.Add(entity);

            entity.SpatialNode = this;
        }

        private void GrowExtents(Vector3 lo, Vector3 hi)
        {
            Icoseptree pp = this;
            while (pp != null)
            {
                if (pp.AreExtendsCorrected)
                {
                    pp.Min = Vector3.Min(pp.Min, lo);
                    pp.Max = Vector3.Max(pp.Max, hi);
                }

                pp = pp.Parent;
            }
        }

        private void ShrinkExtents(Vector3 c, float r)
        {
            Icoseptree pp = this;
            while (pp != null)
            {
                if (c.X - r <= pp.Min.X
                || c.Y - r <= pp.Min.Y
                || c.Z - r <= pp.Min.Z
                || c.X + r >= pp.Max.X
                || c.Y + r >= pp.Max.Y
                || c.Z + r >= pp.Max.Z)
                {
                    pp.AreExtendsCorrected = false;
                }

                pp = pp.Parent;
            }
        }

        private bool Contains(Vector3 minp, Vector3 maxp)
        {
            return (minp.X >= Min.X
            && minp.Y >= Min.Y
            && minp.Z >= Min.Z
            && maxp.X <= Max.X
            && maxp.Y <= Max.Y
            && maxp.Z <= Max.Z);
        }

        private Icoseptree GetChild(int key)
        {
            if (key < 0) return this;

            if(!Children.TryGetValue(key, out var child))
            {
                child = new Icoseptree(this);
                Children.Add(key, child);
            }

            return child;
        }

        private Icoseptree FindNode(ISpatial spatial)
        {
            if(spatial == null) return null;
            var bounds = spatial.BoundingSphere;
            return FindNode(ref bounds);
        }

        private Icoseptree FindNode(ref BoundingSphere bounds)
        {
            Icoseptree node = this;
            int c = WhichChild(ref bounds);
            while (c >= 0)
            {
                node = node.GetChild(c);
                c = node.WhichChild(ref bounds);
            }
            return node;
        }

        private bool RemoveObject(ISpatial entity, Vector3 oldp, float oldr)
        {
            if (!Objects.Contains(entity))
            {
                if (Parent != null)
                    return Parent.RemoveObject(entity);

                return false;
            }

            ShrinkExtents(oldp, oldr);
            Objects.Remove(entity);
            entity.SpatialNode = null;

            // THIS MUST COME LAST
            TestSuicide();
            return true;
        }

        private void TestSuicide()
        {
            if (Parent == null) return;

            if (Objects.Count == 0 && Children.Count == 0)
                Parent.RemoveChild(this);
        }

        private void RemoveChild(Icoseptree node)
        {
            int found = -1;
            foreach (var pair in Children)
            {
                if (pair.Value == node)
                {
                    found = pair.Key;
                    break;
                }
            }

            if (found > -1)
                Children.Remove(found);

            TestSuicide();
        }
    }
}