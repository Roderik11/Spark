using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using SharpDX;

namespace Spark
{
    public sealed class Transform : Component
    {
        private Transform _parent;
        private bool _isDirty = true;
        private Vector3 _position;
        private Quaternion _rotation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;
        private Matrix _matrix = Matrix.Identity;
        private Matrix _localTransform = Matrix.Identity;
        private Vector3 _worldPosition;
        private int _depth;
        private readonly List<Transform> Children = new List<Transform>();

        public event Action OnChanged;
        public int Depth => _depth;

        public int GetChildCount() => Children.Count;
         
        public Transform GetChild(int i)
        {
            if (Children.Count > i)
                return Children[i];

            return null;
        }

        [XmlIgnore]
        [Browsable(false)]
        public Transform Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                if (_parent == value) return;
                _parent?.Children.Remove(this);
                _parent = value;
                _parent?.Children.Add(this);
                _depth = _parent != null ? _parent.Depth + 1 : 0;
            }
        }

        [Browsable(false)]
        public Vector3 WorldPosition
        {
            get
            {
                if (IsDirty) return Matrix.TranslationVector;
                return _worldPosition;
            }
        }

        [Browsable(false)]
        public Vector3 WorldScale
        {
            get
            {
                if (_parent != null)
                    return _scale * _parent.WorldScale;

                return _scale;
            }
        }

        public bool IsDirty
        {
            get
            {
                if (_isDirty)
                    return true;

                if (_parent != null)
                    return _parent.IsDirty;

                return false;
            }
        }

        public Vector3 Up => Vector3.Normalize(Matrix.Up); 
        public Vector3 Forward => Vector3.Normalize(Matrix.Backward);
        public Vector3 Right => Vector3.Normalize(Matrix.Right);

        internal void SetDirty()
        {
            _isDirty = true;

            for (int i = 0; i < Children.Count; i++)
                Children[i].SetDirty();

            OnChanged?.Invoke();
        }

        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (_position == value) return;
                _position = value; SetDirty();
            }
        }

        public Quaternion Rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                if (_rotation == value) return;
                _rotation = value; SetDirty();
            }
        }

        public Vector3 Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                if (_scale == value) return;
                _scale = value; SetDirty();
            }
        }

        [XmlIgnore]
        public Matrix Matrix
        {
            get
            {
                if (IsDirty)
                {
                    _isDirty = false;
                    _localTransform = Matrix.Scaling(_scale) * Matrix.RotationQuaternion(_rotation) * Matrix.Translation(_position);

                    if (_parent != null)
                        _matrix = _localTransform * _parent.Matrix;
                    else
                        _matrix = _localTransform;

                    _worldPosition = _matrix.TranslationVector;
                }

                return _matrix;
            }
            set
            {
                Matrix m = value;

                if (_parent != null)
                    m *= Matrix.Invert(_parent.Matrix);

                m.Decompose(out _, out _rotation, out _position);
                
                SetDirty();
            }
        }

        public void Set(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public void RotateLocal(Vector3 axis, float angle)
        {
            Rotation = Quaternion.RotationAxis(axis, MathUtil.DegreesToRadians(angle)) * _rotation;
        }

        public void Rotate(Vector3 axis, float angle)
        {
            Rotation = _rotation * Quaternion.RotationAxis(axis, MathUtil.DegreesToRadians(angle));
        }

        public void Move(float forward, float up, float right)
        {
            Vector3 f = Forward;
            Vector3 u = Up;
            Vector3 s = Right;

            if (_parent != null)
            {
                Quaternion m = _parent.Rotation;
                m.Invert();

                if (forward != 0) f = Vector3.Transform(f, m);
                if (up != 0) u = Vector3.Transform(u, m);
                if (right != 0) s = Vector3.Transform(s, m);
            }

            if (forward != 0) _position += f * forward;
            if (up != 0) _position += u * up;
            if (right != 0) _position += s * right;

            SetDirty();
        }

        public bool IsChildOf(Transform other)
        {
            if (other.Children.Contains(this))
                return true;

            foreach (Transform child in other.Children)
            {
                if (IsChildOf(child))
                    return true;
            }

            return false;
        }

        public int Count => Children.Count;

        public System.Collections.IEnumerator GetEnumerator() => Children.GetEnumerator();

    }
}