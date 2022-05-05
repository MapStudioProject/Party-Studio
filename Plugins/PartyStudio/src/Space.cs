using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using GLFrameworkEngine;

namespace PartyStudio
{
    public class Space : MapStudio.UI.IPropertyUI
    {
        public string Name { get; set; } = "00";

        private string _icon;
        public string Icon
        {
            get { return _icon; }
            set
            {
                if (_icon != value) {
                    _icon = value;
                    if (PathPoint != null)
                        PathPoint.UINode.Icon = _icon;
                }
            }
        }

        public virtual float BaseScale { get; } = 1.0f;

        public List<Space> Children = new List<Space>();

        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; } = Vector3.One;

        public BoardPathPoint PathPoint;

        public StandardMaterial Material = new StandardMaterial();
        static PlaneRenderer SpaceDrawer;

        public Space() {
            Material.Color = new Vector4(1, 1, 1, 1.0f);
        }


        public Type GetTypeUI() => typeof(Space);

        public void OnLoadUI(object uiInstance)
        {

        }

        public void OnRenderUI(object uiInstance)
        {
            DrawUI();
        }

        public virtual void DrawUI()
        {

        }

        public virtual Space Copy()
        {
            var space = new Space();
            space.Position = this.Position;
            space.Scale = this.Scale;
            space.Rotation = this.Rotation;
            space.Icon = this.Icon;
            space.Name = this.Name;
            return space;
        }

        public virtual void Render(GLContext context)
        {
            if (SpaceDrawer == null)
                SpaceDrawer = new PlaneRenderer(5);

            var rot = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(90));
            var offset = Matrix4.CreateTranslation(0, 0.8f, 0);
            var baseScale = Matrix4.CreateScale(BaseScale);

            PathPoint.RaySphereSize = 8.1f;

            Material.DisplaySelection = PathPoint.IsHovered || PathPoint.IsSelected;
            Material.ModelMatrix = (baseScale * rot * offset) * PathPoint.Transform.TransformMatrix;

            GLMaterialBlendState.Translucent.RenderBlendState();

            Material.Render(context);
            SpaceDrawer.DrawWithSelection(context, PathPoint.IsSelected);

            GLMaterialBlendState.Opaque.RenderBlendState();
        }
    }
}
