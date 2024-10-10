using GLFrameworkEngine;
using MapStudio.UI;
using MPLibrary.GCN;
using PartyStudioPlugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using MPLibrary;
using PartyStudioPlugin.Properties;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace PartyStudio.GCN
{
    internal class MPGCN : BoardLoader
    {
        public GameVersion Version = GameVersion.MP4;

        public ModelEditor ModelEditor = new ModelEditor();

        static Dictionary<string, GLTexture> SpaceIcons = new Dictionary<string, GLTexture>();
        static Dictionary<string, Vector4> SpaceColors = new Dictionary<string, Vector4>();

        Board BoardParams = new Board();

        FileDefinition FileDefinition = new FileDefinition();

        public override Type SpaceType
        {
            get
            {
                if (Version == GameVersion.MP4)
                    return typeof(MP4.MP4Space);

                return typeof(SpaceNode);
            }
        }

        private MPBIN MapArchive;

        public MPGCN()
        {
            if (SpaceIcons.Count > 0)
                return;

            SpaceIcons.Add("Blue", GLTexture2D.FromBitmap(Resources.Blue));
            SpaceIcons.Add("Red", GLTexture2D.FromBitmap(Resources.Red));
            SpaceIcons.Add("Event", GLTexture2D.FromBitmap(Resources.Event));
            SpaceIcons.Add("Star", GLTexture2D.FromBitmap(Resources.Star));
            SpaceIcons.Add("Bowser", GLTexture2D.FromBitmap(Resources.Bowser));
            SpaceIcons.Add("Spring", GLTexture2D.FromBitmap(Resources.Spring));
            SpaceIcons.Add("Item", GLTexture2D.FromBitmap(Resources.Item));
            SpaceIcons.Add("Mic", GLTexture2D.FromBitmap(Resources.Mic));
            SpaceIcons.Add("Miracle", GLTexture2D.FromBitmap(Resources.Miracle_Space));
            SpaceIcons.Add("VS", GLTexture2D.FromBitmap(Resources.Duel));
            SpaceIcons.Add("Shop", GLTexture2D.FromBitmap(Resources.Shop));
            SpaceIcons.Add("Shop1", GLTexture2D.FromBitmap(Resources.Shop));
            SpaceIcons.Add("Shop2", GLTexture2D.FromBitmap(Resources.Shop));
            SpaceIcons.Add("Shop3", GLTexture2D.FromBitmap(Resources.Shop));
            SpaceIcons.Add("Boo", GLTexture2D.FromBitmap(Resources.Teresa));
            SpaceIcons.Add("Lottery", GLTexture2D.FromBitmap(Resources.Bank));
            SpaceIcons.Add("Start", GLTexture2D.FromBitmap(Resources.Start));
            SpaceIcons.Add("PlayerSpot", GLTexture2D.FromBitmap(Resources.PlayerStart));
            SpaceIcons.Add("Orb", GLTexture2D.FromBitmap(Resources.Orbs));
            SpaceIcons.Add("DK_Bowser", GLTexture2D.FromBitmap(Resources.DK));


            foreach (var icon in SpaceIcons)
                MapStudio.UI.IconManager.TryAddIcon(icon.Key.ToString(), icon.Value);
        }

        public override void LoadFile(FileEditor mapEditor, Stream data, string fileName)
        {
            Version = GameVersion.MP4;

            MapArchive = new MPBIN(fileName);

            string fileInfoPath = Path.Combine(Runtime.ExecutableDir, "Lib", "MP4", "W01.txt");
            FileDefinition = new FileDefinition(fileInfoPath);

            LoadModelAssets(mapEditor);
            LoadSpaces();
        }

        BoardObjectList ObjectList;

        private void LoadModelAssets(FileEditor mapEditor)
        {
            int fileID = 0;
            foreach (var file in MapArchive.files)
            {
                string ext = Utils.GetExtension(file.FileName);
                Console.WriteLine($"reading file {file.FileName}");
                if (ext == ".hsf")
                {
                    try
                    {
                        var hsf = file.OpenFile() as HSF;
                        file.FileFormat = hsf;
                        if (hsf.Header.Meshes.Count > 0)
                        {
                            hsf.Root.Header = file.FileName;

                            hsf.Render.CanSelect = false;
                            mapEditor.AddRender(hsf.Render);

                            ModelEditor.Add(hsf, FileDefinition, fileID);
                            mapEditor.Root.AddChild(hsf.Root);
                        }
                    }
                    catch
                    {

                    }
                }
                fileID++;
            }
        }

        private void LoadSpaces()
        {
            //Space file is always the first non model file
            var spaceFile = MapArchive.files.FirstOrDefault(x => !x.FileName.Contains(".hsf") && !x.FileName.Contains(".atb"));
            BoardParams = new Board(spaceFile.FileData, Version);
            //Load spaces
            foreach (var space in BoardParams.Spaces)
            {
                switch (Version)
                {
                    case GameVersion.MP4:
                        Spaces.Add(new MP4.MP4Space(space));
                        break;
                    case GameVersion.MP6:
                        Spaces.Add(new MP6.MP6Space(space));
                        break;
                    default:
                        Spaces.Add(new SpaceNode(space));
                        break;
                }
            }
            //Setup children
            for (int i = 0; i < Spaces.Count; i++)
            {
                foreach (var id in BoardParams.Spaces[i].ChildrenIndices)
                    Spaces[i].Children.Add(Spaces[id]);
            }

            switch (Version)
            {
                case GameVersion.MP4:
                    this.SpaceTypeList.AddRange(MP4.GetSupportedSpaces());
                    break;
                case GameVersion.MP5:
                 //   this.SpaceTypeList.AddRange(MP5.GetSupportedSpaces());
                    break;
                case GameVersion.MP6:
                    this.SpaceTypeList.AddRange(MP6.GetSupportedSpaces());
                    break;
              /*  case GameVersion.MP7:
                    this.SpaceTypeList.AddRange(MP7.GetSupportedSpaces());
                    break;
                case GameVersion.MP8:
                    this.SpaceTypeList.AddRange(MP8.GetSupportedSpaces());
                    break;*/
            }
        }

        public override void SaveFile(FileEditor mapEditor, Stream data)
        {
            //Setup board params
            BoardParams.Spaces.Clear();
            for (int i = 0; i < Spaces.Count; i++)
            {
                var space = (Spaces[i] as SpaceNode).MPSpace;

                space.Position = Spaces[i].PathPoint.Transform.Position;
                space.EulerRotation = Spaces[i].PathPoint.Transform.RotationEulerDegrees;
                space.Scale = Spaces[i].PathPoint.Transform.Scale;
                
                space.ChildrenIndices.Clear();
                foreach (var child in Spaces[i].PathPoint.Children)
                {
                    int index = this.PathRender.PathPoints.IndexOf(child);
                    if (index != -1)
                        space.ChildrenIndices.Add((ushort)index);
                }

                BoardParams.Spaces.Add(space);
            }

            var mem = new MemoryStream();
            BoardParams.Save(mem, Version);

            MapArchive.files[0].FileData = new MemoryStream(mem.ToArray());
            MapArchive.Save(data);
        }

        public class BoardObjectList
        {
            HSF Hsf;

            List<EditableObject> Objects = new List<EditableObject>();

            public void Load(FileEditor mapEditor, HSF hsf)
            {
                Hsf = hsf;

                foreach (var bone in hsf.Header.ObjectNodes)
                {
                    var worldMatrix = bone.CalculateWorldMatrix();

                    var editable = new TransformableObject(mapEditor.Root);
                    editable.UINode.Header = bone.Name;
                    editable.Transform.Position = worldMatrix.ExtractTranslation();
                    editable.Transform.Rotation = worldMatrix.ExtractRotation();
                    editable.Transform.Scale = worldMatrix.ExtractScale();
                    editable.Transform.UpdateMatrix(true);
                    Objects.Add(editable);

                    mapEditor.AddRender(editable);
                }
            }

            public void Save(MPBIN bin, int index)
            {
                for (int i = 0; i < Hsf.Header.ObjectNodes.Count; i++)
                {
                    var objData = Hsf.Header.ObjectNodes[i].Data;

                    //World to local space
                    var parentWorldSpace = Matrix4.Identity;
                    if (Hsf.Header.ObjectNodes[i].Parent != null)
                        parentWorldSpace = Hsf.Header.ObjectNodes[i].Parent.CalculateWorldMatrix().Inverted();

                    var ob = Objects[i].Transform.TransformMatrix * parentWorldSpace;
                    var pos = ob.ExtractTranslation();
                    var rot = STMath.ToEulerAngles(ob.ExtractRotation()) * STMath.Rad2Deg;
                    var sca = ob.ExtractScale();

                    objData.BaseTransform.Translate = new Vector3XYZ(pos.X, pos.Y, pos.Z);
                    objData.BaseTransform.Rotate = new Vector3XYZ(rot.X, rot.Y, rot.Z);
                    objData.BaseTransform.Scale = new Vector3XYZ(sca.X, sca.Y, sca.Z);
                }

                var mem = new MemoryStream();
                Hsf.Save(mem);
                bin.files[index].FileData = new MemoryStream(mem.ToArray());
            }
        }

        public class SpaceNode : Space
        {
            public MPSpace MPSpace = new MPSpace();

            public SpaceNode() { }

            public SpaceNode(MPSpace space)
            {
                MPSpace = space;
                this.Position = space.Position;
                this.Rotation = space.EulerRotation * STMath.Deg2Rad;
                this.Scale = space.Scale;
            }


            public SpaceNode(BoardLoader loader) 
            {
     
            }

            public override void Render(GLContext context)
            {
                Material.DiffuseTextureID = -1;
                Material.Color = new Vector4(1);

                if (SpaceIcons.ContainsKey(this.Name))
                    Material.DiffuseTextureID = SpaceIcons[this.Name].ID;
                if (SpaceColors.ContainsKey(this.Name))
                    Material.Color = SpaceColors[this.Name];

                if (SpaceDrawer == null)
                    SpaceDrawer = new PlaneRenderer(60);

                var rot = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(90));
                var offset = Matrix4.CreateTranslation(0, 5, 0);
                var baseScale = Matrix4.CreateScale(BaseScale);

                PathPoint.RaySphereSize = 82.1f * this.Scale.X;

                Material.DisplaySelection = PathPoint.IsHovered || PathPoint.IsSelected;
                Material.ModelMatrix = (baseScale * rot * offset) * PathPoint.Transform.TransformMatrix;

                GLMaterialBlendState.Translucent.RenderBlendState();

                GL.Disable(EnableCap.CullFace);

                Material.Render(context);
                SpaceDrawer.DrawWithSelection(context, PathPoint.IsSelected);

                GL.Enable(EnableCap.CullFace);

                GLMaterialBlendState.Opaque.RenderBlendState();
            }
        }
    }
}
