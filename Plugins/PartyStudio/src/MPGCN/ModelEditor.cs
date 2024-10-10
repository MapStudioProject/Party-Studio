using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MapStudio.UI;
using MPLibrary.GCN;
using Toolbox.Core.ViewModels;
using GLFrameworkEngine;

namespace PartyStudio.GCN
{
    internal class ModelEditor
    {
        public List<ModelNode> MapModels = new List<ModelNode>();

        public void Add(HSF hsf, FileDefinition fileDefinition, int fileIndex)
        {
            string name = $"File{fileIndex}";
            if (fileDefinition.FileList.ContainsKey(fileIndex))
            {
                name = fileDefinition.FileList[fileIndex].Name;
                hsf.Render.IsVisible = fileDefinition.FileList[fileIndex].Display;
            }

            hsf.CanSave = false;

            var m = new ModelNode()
            {
                Header = name,
                Hsf = hsf,
                IsChecked = hsf.Render.IsVisible,
            };
            m.ContextMenus.AddRange(hsf.GetContextMenuItems());

            foreach (var child in hsf.Root.Children)
                m.AddChild(child);

            MapModels.Add(m);
        }

        public class ModelNode : NodeBase
        {
            public HSF Hsf;

            public ModelNode() : base()
            {
                this.IsChecked = true;
                this.HasCheckBox = true;
                this.Icon = IconManager.MODEL_ICON.ToString();
                this.OnChecked += delegate
                {
                    Hsf.Render.IsVisible = this.IsChecked;
                    GLContext.ActiveContext.UpdateViewport = true;
                };
                this.ContextMenus.Add(new MenuItemModel("Clear Models", ClearModels));
                this.ContextMenus.Add(new MenuItemModel("Generate Collision", GenerateCollision));
            }

            public void GenerateCollision()
            {
                var col = GLContext.ActiveContext.CollisionCaster;
                col.Clear();

                var scene = HSFModelImporter.ToScene(Hsf.Header);

                foreach (var mesh in scene.Models[0].Meshes)
                {
                    var transform = OpenTK.Matrix4.Identity;

                    foreach (var p in mesh.Polygons)
                    {
                        for (int v = 0; v < p.Indicies.Count; v += 3)
                        {
                            var v1 = OpenTK.Vector3.TransformPosition(ToVec3(mesh.Vertices[p.Indicies[v + 0]].Position), transform);
                            var v2 = OpenTK.Vector3.TransformPosition(ToVec3(mesh.Vertices[p.Indicies[v + 1]].Position), transform);
                            var v3 = OpenTK.Vector3.TransformPosition(ToVec3(mesh.Vertices[p.Indicies[v + 2]].Position), transform);

                            col.AddTri(v1, v2, v3);
                        }
                    }
                }
                col.UpdateCache();
            }

            private OpenTK.Vector3 ToVec3(System.Numerics.Vector3 v) {
                return new OpenTK.Vector3(v.X, v.Y, v.Z);
            }

            private void ClearModels()
            {
                var selected = this.Parent.Children.Where(x => x.IsSelected);
                foreach (ModelNode node in selected)
                    node.ClearModelData();
            }

            public void ClearModelData()
            {
                //Remove all nodes that are mesh types
                var meshes = Hsf.Header.ObjectNodes.Where(x => x.Data.Type == ObjectType.Mesh).ToList();
                foreach (var mesh in meshes)
                {
                    if (mesh.Data.Type == ObjectType.Mesh)
                    {
                         var msh = Hsf.Header.Meshes.FirstOrDefault(X => X.ObjectData == mesh.Data);

                        msh.Positions.Clear();
                        msh.Positions.Add(new System.Numerics.Vector3());

                        foreach (var prim in msh.Primitives)
                        {
                            for (int i = 0; i < prim.Vertices.Length; i++)
                                prim.Vertices[i].PositionIndex = 0;
                        }
                    }
                }

                Hsf.CanSave = true;
                Hsf.ReloadHSF();
                Hsf.ReloadRender();

                this.Children.Clear();
                foreach (var child in Hsf.Root.Children)
                    AddChild(child);

                GLContext.ActiveContext.UpdateViewport = true;
            }
        }
    }
}
