using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spark.Editor
{
    public static class Selector
    {
        private static Entity _selectedEntity;
        private static object _selectedObject;

        public static object SelectedObject
        {
            get { return _selectedObject; }
            set
            {
                _selectedObject = value;
                _selectedEntity = value as Entity;

                Selection.Clear();
                if (_selectedEntity != null)
                    Selection.Add(_selectedEntity);

                MessageDispatcher.Send(Msg.SelectionChanged, _selectedObject);
            }
        }

        public static Entity SelectedEntity
        {
            get { return _selectedEntity; }
            set { SelectedObject = value; }
        }

        public static void SelectOrDeselect(Entity entity)
        {
            if(Selection.Count == 0)
            {
                SelectedEntity = entity;
                return;
            }

            if(Selection.Contains(entity))
                Selection.Remove(entity);
            else
                Selection.Add(entity);
      
            MessageDispatcher.Send(Msg.SelectionChanged, _selectedObject);
        }

        public static List<Entity> Selection = new List<Entity>();

        static Selector()
        {
        }

        public static void Draw()
        {
            foreach (Jitter.Dynamics.RigidBody obj in Physics.World.RigidBodies)
            {
                if (!obj.EnableDebugDraw) continue;
                obj.DebugDraw(JitterDebugDraw.Instance);
            }

            foreach(var entity in Selection)
            {
                foreach (var comp in entity.GetComponents())
                {
                    if(comp is IDrawDebug drawDebug)
                        drawDebug.DrawDebug();
                }
            }
        }
    }
}
