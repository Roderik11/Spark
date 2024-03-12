using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SharpDX;

namespace Spark
{
    public struct IcoQueryResult
    {
        public List<ISpatial> Included;
        public List<ISpatial> Intersecting;
        public List<IDraw> AlwaysVisible;

        public static IcoQueryResult Create()
        {
            return new IcoQueryResult
            {
                Included = new List<ISpatial>(),
                Intersecting = new List<ISpatial>()
            };
        }

        public void Draw(BoundingFrustum frustum, Func<IComponent, bool> action, bool parallel = true)
        {
            int limit = 512;
            
            AlwaysVisible?.For(e =>
                {
                    if (!e.Enabled) return;
                    if (!action(e)) return;
                    e.Draw();
                }, AlwaysVisible.Count > 128);

            Included.For(e =>
            {
                if (!e.Enabled) return;
                if (!action(e)) return;
                if (!(e is IDraw draw)) return;
                draw.Draw();
            }, Included.Count > limit || parallel);

            Intersecting.For(e =>
            {
                if (!e.Enabled) return;
                if (!action(e)) return;
                if (!(e is IDraw draw)) return;
                if (frustum.Contains(e.BoundingBox) == ContainmentType.Disjoint) return;
                draw.Draw();
            }, Intersecting.Count > limit || parallel);



        }

        public void Clear()
        {
            if(Included.Count > 0) Included.Clear();
            if(Intersecting.Count > 0) Intersecting.Clear();
        }
    }

    public struct IcoFrustumQueryPro2
    {
        private BoundingFrustum Frustum;
        private readonly Icoseptree[] Trees;
        private readonly List<Icoseptree> nodes;
        public bool Split { get; set; }

        public IcoFrustumQueryPro2(BoundingFrustum frustum, params Icoseptree[] trees)
        {
            Split = true;
            Trees = trees;
            Frustum = frustum;
            nodes = new List<Icoseptree>();
        }

        public void Execute(ref IcoQueryResult result)
        {
            result.Clear();
            TestChildren(Trees.ToList(), ref result);
        }

        private void TestChildren(List<Icoseptree> parents, ref IcoQueryResult result)
        {
            if(nodes.Count > 0) nodes.Clear();

            foreach(var parent in parents)
            {
                foreach (var pair in parent.Children)
                {
                    pair.Value.GetExtents(Split);
                    nodes.Add(pair.Value);
                }
            }

            var frustum = Frustum;

            nodes.For((node) =>
            {
                frustum.Contains(ref node.Bounds, out node.Containment);
            }, nodes.Count > 128);

            parents.Clear();

            foreach(var node in nodes)
            {
                switch (node.Containment)
                {
                    case ContainmentType.Contains:
                        Include(node, ref result);
                        break;
                    case ContainmentType.Intersects:
                        if(node.Objects.Count > 0) result.Intersecting.AddRange(node.Objects);
                        parents.Add(node);
                        break;
                }
            }

            if(parents.Count > 0)
                TestChildren(parents, ref result);
        }

        private void Include(Icoseptree node, ref IcoQueryResult result)
        {
            if (node.Objects.Count > 0) result.Included.AddRange(node.Objects);
            foreach (var pair in node.Children)
                Include(pair.Value, ref result);
        }
    }


    public struct IcoFrustumQueryPro
    {
        private BoundingFrustum Frustum;
        private readonly Icoseptree[] Trees;
        public bool Split { get; set; }

        public IcoFrustumQueryPro(BoundingFrustum frustum, params Icoseptree[] trees)
        {
            Split = true;
            Trees = trees;
            Frustum = frustum;
        }

        public void Execute(ref IcoQueryResult result)
        {
            result.Clear();

            foreach (Icoseptree root in Trees)
                Recursion(root, ref result);
        }

        private void Recursion(Icoseptree node, ref IcoQueryResult result)
        {
            node.GetExtents(Split);
            Frustum.Contains(ref node.Bounds, out node.Containment);

            switch (node.Containment)
            {
                case ContainmentType.Disjoint: return;
                case ContainmentType.Contains:
                    Include(node, ref result);
                    break;
                case ContainmentType.Intersects:
                    result.Intersecting.AddRange(node.Objects);
                    foreach (var pair in node.Children)
                        Recursion(pair.Value, ref result);
                    break;
            }
        }

        private void Include(Icoseptree node, ref IcoQueryResult result)
        {
            result.Included.AddRange(node.Objects);
            foreach (var pair in node.Children)
                Include(pair.Value, ref result);
        }
    }

    public struct IcoFrustumQuery
    {
        private BoundingFrustum Frustum;
        private readonly Icoseptree[] Trees;
        public bool Split { get; set; }

        public IcoFrustumQuery(BoundingFrustum frustum, params Icoseptree[] trees)
        {
            Split = true;
            Trees = trees;
            Frustum = frustum;
        }

        public List<ISpatial> Execute(List<ISpatial> result = null)
        {
            if(result == null)
                result = new List<ISpatial>();

            foreach (Icoseptree root in Trees)
                Recursion(root, result);

            return result;
        }

        private void Recursion(Icoseptree node, List<ISpatial> result)
        {
            BoundingBox bounds = node.GetExtents(Split);
            Frustum.Contains(ref bounds, out var type);

            switch (type)
            {
                case ContainmentType.Disjoint: return;
                case ContainmentType.Contains:
                    Include(node, result);
                    break;
                case ContainmentType.Intersects:

                    // check individual objects
                    //foreach (ISpatial e in node.Objects)
                    //{
                    //    if (!e.Enabled) continue;

                    //    if (Frustum.Contains(e.BoundingBox) != ContainmentType.Disjoint)
                    //        result.Add(e);
                    //}

                    foreach (ISpatial e in node.Objects)
                    {
                        if (!e.Enabled) continue;
                        result.Add(e);
                    }

                    foreach (var pair in node.Children)
                        Recursion(pair.Value, result);
                    break;
            }
        }

        private void Include(Icoseptree node, List<ISpatial> result)
        {
            foreach (ISpatial e in node.Objects)
            {
                if (!e.Enabled) continue;
                result.Add(e);
            }

            foreach (var pair in node.Children)
                Include(pair.Value, result);
        }
    }

    public class IcoRayQuery
    {
        private Ray ray;
        private readonly Icoseptree[] Trees;
        private Predicate<ISpatial> CriteriaCheck;

        public bool Split { get; set; }

        public IcoRayQuery(Ray ray, params Icoseptree[] trees)
        {
            Split = true;
            Trees = trees;
            this.ray = ray;
        }

        struct ContactPoint
        {
            public ISpatial entity;
            public Vector3 point;
        }

        public List<ISpatial> Execute(Predicate<ISpatial> predicate)
        {
            List<ISpatial> result = new List<ISpatial>();
            List<ContactPoint> contacts = new List<ContactPoint>();

            CriteriaCheck = predicate;

            foreach (Icoseptree root in Trees)
                Recursion(root, contacts);

            contacts.Sort((a, b) => Vector3.Distance(ray.Position, a.point).CompareTo(Vector3.Distance(ray.Position, b.point)));

            foreach(var contact in contacts)
                result.Add(contact.entity);

            return result;
        }

        private void Recursion(Icoseptree node, List<ContactPoint> result)
        {
            BoundingBox bounds = node.GetExtents(Split);
            bool intersects = ray.Intersects(ref bounds);

            if (!intersects)
                return;

            // check individual objects
            foreach (ISpatial e in node.Objects)
            {
                if (!e.Enabled) continue;

                bool valid = CriteriaCheck == null ? true : CriteriaCheck(e);
                if (!valid) continue;

                var bb = e.BoundingBox;
                if(ray.Intersects(ref bb, out Vector3 hit))
                {
                    result.Add(new ContactPoint { entity = e, point = hit });
                }
            }

            foreach (var pair in node.Children)
                Recursion(pair.Value, result);
        }
    }

    public class IcoBoxQuery
    {
        private BoundingBox BoundingBox;
        private Icoseptree[] Trees;
        private Predicate<ISpatial> CriteriaCheck;

        public bool Split { get; set; }

        public IcoBoxQuery(BoundingBox box, params Icoseptree[] trees)
        {
            Split = true;
            Trees = trees;
            BoundingBox = box;
        }

        public List<ISpatial> Execute(Predicate<ISpatial> predicate)
        {
            List<ISpatial> result = new List<ISpatial>();

            CriteriaCheck = predicate;

            foreach (Icoseptree root in Trees)
                Recursion(root, result);

            return result;
        }

        private void Recursion(Icoseptree node, List<ISpatial> result)
        {
            BoundingBox bounds = node.GetExtents(Split);
            ContainmentType type = BoundingBox.Contains(ref bounds);

            BoundingBox.Intersects(bounds);

            if (type == ContainmentType.Disjoint)
                return;

            // the frustum contains this AABB node completely
            // include everything inside this node and every child node
            if (type == ContainmentType.Contains)
            {
                Include(node, result);
            }
            else if (type == ContainmentType.Intersects)
            {
                // check individual objects
                foreach (ISpatial e in node.Objects)
                {
                    if (!e.Enabled) continue;

                    bool valid = CriteriaCheck == null ? true : CriteriaCheck(e);
                    var otherBounds = e.BoundingBox;

                    if (valid && BoundingBox.Contains(ref otherBounds) != ContainmentType.Disjoint)
                        result.Add(e);
                }

                foreach (var pair in node.Children)
                    Recursion(pair.Value, result);
            }
        }

        private void Include(Icoseptree node, List<ISpatial> result)
        {
            foreach (ISpatial e in node.Objects)
            {
                if (!e.Enabled) continue;

                bool valid = CriteriaCheck == null ? true : CriteriaCheck(e);

                if (valid)
                    result.Add(e);
            }

            foreach (var pair in node.Children)
                Include(pair.Value, result);
        }
    }

    public struct IcoBoxVisitor
    {
        private BoundingBox BoundingBox;
        private Icoseptree[] Trees;

        public bool Split { get; set; }

        public IcoBoxVisitor(BoundingBox box, params Icoseptree[] trees)
        {
            Split = true;
            Trees = trees;
            BoundingBox = box;
        }

        public void Execute(Action<ISpatial> action)
        {
            foreach (Icoseptree root in Trees)
                Recursion(root, action);
        }

        private void Recursion(Icoseptree node, Action<ISpatial> action)
        {
            BoundingBox bounds = node.GetExtents(Split);
            ContainmentType type = BoundingBox.Contains(bounds);

            switch (type)
            {
                case ContainmentType.Disjoint:
                    return;
                case ContainmentType.Contains:
                    Include(node, action);
                    break;
                case ContainmentType.Intersects:

                    // check individual objects
                    foreach (ISpatial e in node.Objects)
                    {
                        if (!e.Enabled) continue;

                        if (BoundingBox.Contains(e.BoundingBox) != ContainmentType.Disjoint)
                            action(e);
                    }

                    foreach (var pair in node.Children)
                        Recursion(pair.Value, action);

                    break;
            }
        }

        private void Include(Icoseptree node, Action<ISpatial> action)
        {
            foreach (ISpatial e in node.Objects)
            {
                if (!e.Enabled) continue;
                action(e);
            }

            foreach (var pair in node.Children)
                Include(pair.Value, action);
        }
    }

}