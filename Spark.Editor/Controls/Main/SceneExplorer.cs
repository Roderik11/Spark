using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Spark.Windows;
using Spark;

namespace Spark.Editor
{
    public delegate void SelectionChanged();

    public partial class SceneExplorer : DockContent
    {
        public event SelectionChanged OnSelectionChanged;

        public SceneExplorer()
        {
            InitializeComponent();
           
            treeListView.CanExpandGetter = delegate(object x)
            {
                Entity entity = x as Entity;
                return entity.Transform.Count > 0;
            };

            treeListView.ChildrenGetter = delegate(object x)
            {
                List<Entity> result = new List<Entity>();

                foreach (Transform child in (x as Entity).Transform)
                    result.Add(child.Entity);

                return result;
            };

            treeListView.SmallImageList = EditorIcons.Images;
            olvColumn1.ImageGetter = delegate(object x)
            {
                Entity entity = (Entity)x;

                if (entity.IsPrototype)
                    return EditorIcons.ProtoInstance;
                
                return null;
            };

            treeListView.FormatRow += treeListView_FormatRow;
        }

        public void RefreshExplorer()
        {
            treeListView.DiscardAllState();
            treeListView.Roots = Entity.Entities;// Editor.Scene.GetEntities();
        }

        public void Select(List<Entity> entities)
        {
            foreach (Entity e in entities)
                EnsureVisible(e);

            try
            {
                treeListView.SelectObjects(entities);
            }
            catch { }
        }

        public void Select(Entity entity)
        {
            Select(entity, true);
        }

        public void Select(Entity entity, bool focus)
        {
            if (entity == null)
                treeListView.DeselectAll();
            else
            {
                if (focus) EnsureVisible(entity);
                treeListView.SelectObject(entity, focus);
            }
        }

        public void EnsureVisible(Entity entity)
        {
            Transform node = entity.Transform;
         
            while (node != null)
            {
                treeListView.Expand(node.Entity);
                node = node.Transform.Parent;
            }

            try
            {
                treeListView.EnsureModelVisible(entity);
            }
            catch { }
        }

        void treeListView_FormatRow(object sender, BrightIdeasSoftware.FormatRowEventArgs e)
        {
            Entity entity = e.Model as Entity;
            if (entity.Hidden)
            {
                e.Item.BackColor = System.Drawing.Color.IndianRed;
                e.Item.ForeColor = System.Drawing.Color.White;
            }
        }

        private void treeListView_SelectionChanged(object sender, EventArgs e)
        {
            Editor.SelectedObject = treeListView.SelectedObject;
            //Selector.Clear();
            //Selector.Select(treeListView.SelectedObjects.Cast<Entity>());

            //if (OnSelectionChanged != null)
            //    OnSelectionChanged();
        }

        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (toolStripTextBox1.Text.Trim().Length == 0)
            {
                treeListView.ModelFilter = null;
                return;
            }

            treeListView.ModelFilter = new BrightIdeasSoftware.ModelFilter(delegate(object x)
            {
                return ((Entity)x).Name.ToLowerInvariant().Contains(toolStripTextBox1.Text.ToLowerInvariant());
            });
        }

        private void treeListView_ModelCanDrop(object sender, BrightIdeasSoftware.ModelDropEventArgs e)
        {
            foreach (object o in e.SourceModels)
            {
                if (!(o is Entity))
                {
                    e.Effect = DragDropEffects.None;
                    e.InfoMessage = "Can only drop entities";
                    return;
                }
            }

            Entity target = e.TargetModel as Entity;

            if (target != null)
            {
                if (target.IsPrototype || target.IsChildOfPrototype)
                {
                    e.Effect = DragDropEffects.None;
                    e.InfoMessage = "Cannot drop to prototype (" + target.Prototype + ")";
                    return;
                }
                else
                {
                    foreach (object data in e.SourceModels)
                    {
                        Entity source = data as Entity;
                        if (target.Transform.IsChildOf(source.Transform))
                        {
                            e.Effect = DragDropEffects.None;
                            e.InfoMessage = "Can't drop entity in its own child";
                            return;
                        }
                    }
                }

                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.Move;
                e.InfoMessage = "Drop to root";
            }
        }

        private void treeListView_ModelDropped(object sender, BrightIdeasSoftware.ModelDropEventArgs e)
        {
            //Entity newparent = e.TargetModel == null ? Editor.ActiveScene.Root : e.TargetModel as Entity;
            
            //foreach (object data in e.SourceModels)
            //{
            //    Entity source = data as Entity;
            //    source.Parent = newparent;
            //}

            //RefreshExplorer();
        }

        private void treeListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //if (Selector.Selection.Count < 1)
            //    return;

            //BoundingBox box = Selector.AABB;

            //Editor.Camera.Entity.Transform.Position = box.Center;
            //Editor.Camera.Entity.Transform.Move(-box.Length, 0, 0);
        }

        private void btnToggleSelection_ButtonClick(object sender, EventArgs e)
        {
            KeyMapper.Invoke(KeyBindAction.ToggleSelection);
        }

        private void btnShowSelection_Click(object sender, EventArgs e)
        {
            KeyMapper.Invoke(KeyBindAction.ShowSelection);
        }

        private void btnHideSelection_Click(object sender, EventArgs e)
        {
            KeyMapper.Invoke(KeyBindAction.HideSelection);
        }
    }
}
