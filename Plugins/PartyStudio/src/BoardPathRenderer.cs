using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK;
using MapStudio.UI;

namespace PartyStudio
{
    public class BoardPathRenderer : RenderablePath
    {
        public Type SpaceType;

        public override string Name => TranslationSource.GetText("SPACES");

        public BoardLoader BoardLoader;

        public override bool EditMode => true;

        public BoardPathRenderer(BoardLoader boardLoader) {
            //Board loader for getting space data
            BoardLoader = boardLoader;
            boardLoader.PathRender = this;

            //Directly editable
            EditMode = true;
            IsActive = true;
            //Don't connect on hover as points can be close to each other,
            ConnectHoveredPoints = false;

            //Configure display
            PointColor = new Vector4(1, 0, 0, 1);
            ArrowColor = new Vector4(1, 0.7f, 0, 1);
            LineColor = new Vector4(1, 1, 0, 1);
            IsArrowCentered = true;
            LineWidth = 2;
            PointSize = 0.3f;

            SegmentDrawPointLength = 20;

            //The space type to create.
            SpaceType = boardLoader.SpaceType;
            //Add all the spaces from the current board
            foreach (var space in boardLoader.Spaces)
            {
                var point = new BoardPathPoint(this, space, space.Position);
                point.Transform.RotationEuler = space.Rotation;
                point.UpdateMatrices();
                point.UINode.Tag = space;
                if (!string.IsNullOrEmpty(space.Icon))
                    point.UINode.Icon = space.Icon;

                space.PathPoint = point;

                this.AddPoint(point);
            }
            //Add children from the renderable points
            for (int i = 0; i < boardLoader.Spaces.Count; i++) {
                foreach (var child in boardLoader.Spaces[i].Children) {
                    var cindex = boardLoader.Spaces.IndexOf(child);
                    this.PathPoints[i].AddChild(this.PathPoints[cindex]);
                }
            }
            //Callbacks for adding/removing. Update the space list
            this.PointAddedCallback += (o, e) =>
            {
                this.BoardLoader.Spaces.Add(((BoardPathPoint)o).SpaceData);
            };
            this.PointRemovedCallback += (o, e) =>
            {
                this.BoardLoader.Spaces.Remove(((BoardPathPoint)o).SpaceData);
            };
        }

        public override void OnMouseDown(MouseEventInfo mouseInfo)
        {
            base.OnMouseDown(mouseInfo);
        }

        public override void OnKeyDown(KeyEventInfo keyInfo)
        {
            base.OnKeyDown(keyInfo);
            if (keyInfo.IsKeyDown(InputSettings.INPUT.Scene.Create))
            {
                var selected = GetSelectedPoints().FirstOrDefault();

                GLContext.ActiveContext.Scene.BeginUndoCollection();
                var pt = (BoardPathPoint)AddSinglePoint();
                if (selected != null && !KeyEventInfo.State.KeyCtrl)
                    selected.AddChild(pt);
                GLContext.ActiveContext.Scene.EndUndoCollection();
            }
            if (keyInfo.IsKeyDown(InputSettings.INPUT.Scene.Dupe))
                this.DuplicateSelected();
        }

        public override RenderablePathPoint CreatePoint(Vector3 position) {
            var spaceInstance = (Space)Activator.CreateInstance(SpaceType, BoardLoader);
            return new BoardPathPoint(this, spaceInstance, position);
        }
    }

    public class BoardPathPoint : RenderablePathPoint
    {
        public override string Name => SpaceData.Name;

        public Space SpaceData;

        public BoardPathPoint(BoardPathRenderer path, Space space, Vector3 pos) : base(path, pos) {
            space.PathPoint = this;
            SpaceData = space;
            UINode.Tag = space;
            if (!string.IsNullOrEmpty(space.Icon))
                UINode.Icon = space.Icon;   
        }

        public override RenderablePathPoint Duplicate()
        {
            return new BoardPathPoint((BoardPathRenderer)ParentPath, SpaceData.Copy(), Transform.Position);
        }

        public override void Render(GLContext context, Pass pass)
        {
            if (pass != Pass.TRANSPARENT)
                return;

            SpaceData.Render(context);
        }
    }
}
