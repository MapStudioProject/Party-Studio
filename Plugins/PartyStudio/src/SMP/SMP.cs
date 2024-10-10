using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BfresLibrary;
using CafeLibrary.Rendering;
using GLFrameworkEngine;
using Syroot.NintenTools.NSW.Bntx;
using Toolbox.Core;
using OpenTK;
using MapStudio.UI;
using IONET.Collada.B_Rep.Curves;
using FirstPlugin.BoardEditor;

namespace PartyStudioPlugin
{
    public class SMP : BoardLoader
    {
        //Icons to represent each space type
        static Dictionary<SpaceTypes, GLTexture> SpaceIcons = new Dictionary<SpaceTypes, GLTexture>();
        //Color to use if no icon is present
        static Dictionary<SpaceTypes, Vector4> SpaceColors = new Dictionary<SpaceTypes, Vector4>();

        public List<BfresCameraAnim> OpeningCameras = new List<BfresCameraAnim>();

        public override Type SpaceType => typeof(SpaceNode);

        public BEA MapArchive;

        private string FileName;
        private ResFile SpacePosData;

        private ShaderPack ShaderPack;

        private bool GameShaders = true;

        static void LoadIcons()
        {
            if (SpaceIcons.Count > 0)
                return;


            SpaceIcons.Add(SpaceTypes.PLUS, GLTexture2D.FromBitmap(Properties.Resources.Blue));
            SpaceIcons.Add(SpaceTypes.MINUS, GLTexture2D.FromBitmap(Properties.Resources.Red));
            SpaceIcons.Add(SpaceTypes.HATENA_1, GLTexture2D.FromBitmap(Properties.Resources.Event));
            SpaceIcons.Add(SpaceTypes.HATENA_2, GLTexture2D.FromBitmap(Properties.Resources.Event));
            SpaceIcons.Add(SpaceTypes.HATENA_3, GLTexture2D.FromBitmap(Properties.Resources.Event));
            SpaceIcons.Add(SpaceTypes.ITEM, GLTexture2D.FromBitmap(Properties.Resources.Item));
            SpaceIcons.Add(SpaceTypes.MARK_STAR, GLTexture2D.FromBitmap(Properties.Resources.Star));
            SpaceIcons.Add(SpaceTypes.JOYCON, GLTexture2D.FromBitmap(Properties.Resources.Duel));
            SpaceIcons.Add(SpaceTypes.LUCKY, GLTexture2D.FromBitmap(Properties.Resources.Lucky));
            SpaceIcons.Add(SpaceTypes.LUCKY_2, GLTexture2D.FromBitmap(Properties.Resources.Lucky));
            SpaceIcons.Add(SpaceTypes.SHOP_A, GLTexture2D.FromBitmap(Properties.Resources.Shop));
            SpaceIcons.Add(SpaceTypes.SHOP_B, GLTexture2D.FromBitmap(Properties.Resources.Shop));
            SpaceIcons.Add(SpaceTypes.SHOP_C, GLTexture2D.FromBitmap(Properties.Resources.Shop));
            SpaceIcons.Add(SpaceTypes.MARK_PC, GLTexture2D.FromBitmap(Properties.Resources.PlayerStart));
            SpaceIcons.Add(SpaceTypes.START, GLTexture2D.FromBitmap(Properties.Resources.Start));

            foreach (var icon in SpaceIcons)
                MapStudio.UI.IconManager.TryAddIcon(icon.Key.ToString(), icon.Value);

            //Space types for toolbox view
        }

        public SMP()
        {
            //Load a list of all spaces used in the game.
            //This will be added to the tool menu for adding and selecting space types.
            foreach (SpaceTypes icon in Enum.GetValues(typeof(SpaceTypes)))
            {
                //Skip dupe types
                if (icon == SpaceTypes.HATENA_1 ||
                    icon == SpaceTypes.HATENA_2 ||
                    icon == SpaceTypes.HATENA_3 ||
                    icon == SpaceTypes.HATENA_4)
                    continue;

                SpaceTypeList.Add(icon.ToString());
            }
        }

        public override void LoadFile(FileEditor mapEditor, Stream data, string fileName)
        {
            LoadIcons();

            GLContext.PreviewScale = 10;

            MapArchive = new BEA();
            MapArchive.Load(data);

            var folder = Path.GetDirectoryName(fileName);
            if (File.Exists($"{folder}\\hsbd00.nx.bea"))
                LoadBoardSpaceAssets($"{folder}\\hsbd00.nx.bea");

            FileName = Path.GetFileName(fileName);
            var id = FileName.Split('.').FirstOrDefault();
            var modelHook = $"mainmode/{id}/model/{id}_hook_mass.fmdb";
            var texPath = $"mainmode/{id}/model/textures/";
            var csvParams = $"mainmode/{id}/csv/{id}_map.csv";
            var envFolder = $"mainmode/{id}/env";

            LoadEnvAssets(envFolder, id);

            if (MapArchive.FileLookup.ContainsKey(modelHook)) {
                var modelPosData = MapArchive.FileLookup[modelHook].FileData;
                var csvData = MapArchive.FileLookup[csvParams].FileData;
                LoadSpaceData(new ResFile(modelPosData), csvData);
            }

            var textureViews = GetTextures(); 

            foreach (var file in MapArchive.FileLookup)
            {
                if (!file.Key.StartsWith($"mainmode/{id}/model/{id}_bg") || !file.Key.EndsWith(".fmdb"))
                    continue;

                var mem = file.Value.FileData;
                var render = new BfresRender(mem, file.Key);
                render.IsVisibleCallback = () => ToolWindow.ShowModels;
                //No selection/picking for map models
                render.CanSelect = false;
                mapEditor.AddRender(render);

                render.Textures = textureViews;
            }
        }

        Dictionary<string, GenericRenderer.TextureView> GetTextures()
        {
            Dictionary<string, GenericRenderer.TextureView> textureViews = new Dictionary<string, GenericRenderer.TextureView>();

            foreach (var file in MapArchive.FileLookup.Values)
            {
                if (file.FileName.EndsWith("ftxb")) {
                    foreach (var tex in LoadTextures(file.FileData))
                    {
                        if (tex.Platform.OutputFormat == TexFormat.D32_FLOAT_S8X24_UINT)
                            continue;

                        textureViews.Add(tex.Name, new GenericRenderer.TextureView(tex));
                    }
                }
            }
            return textureViews;
        }

        static List<BntxTexture> LoadTextures(Stream stream)
        {
            List<BntxTexture> textures = new List<BntxTexture>();
            var bntx = new BntxFile(stream);
            foreach (var tex in bntx.Textures)
                textures.Add(new BntxTexture(bntx, tex));

            return textures;
        }

        private void LoadEnvAssets(string folder, string id)
        {
            for (int i = 0; i < 4; i++)
            {
                var camPath = $"{folder}/{id}_cam_op{i.ToString().PadLeft(2, '0')}.fsnb";
                if (!MapArchive.FileLookup.ContainsKey(camPath))
                    continue;

                var resFile = new ResFile(MapArchive.FileLookup[camPath].FileData);
                OpeningCameras.Add(new BfresCameraAnim(resFile.SceneAnims[0].CameraAnims[0]));
            }
        }

        private void LoadBoardSpaceAssets(string fileName)
        {
            return;

            var boardAssets = new BEA();
            boardAssets.Load(File.OpenRead(fileName));

            var textures = LoadTextures(boardAssets.FileLookup["scene/hsbd00/model/obj/textures_hsbd00.bntx"].FileData);
        }

        public override void SaveFile(FileEditor mapEditor, Stream data)
        {
            //Save csv params and hook data
            var id = FileName.Split('.').FirstOrDefault();
            var modelHook = $"scene/{id}/model/{id}_pos.fmdb";
            var csvParams = $"scene/{id}/data/{id}_map.nkn";

            if (MapArchive.FileLookup.ContainsKey(modelHook)) {
                SaveSpaceData(modelHook, csvParams);
            }

            //Save archive
            MapArchive.Save(data);
        }

        private void LoadSpaceData(ResFile resFile, Stream csvParams)
        {
            SpacePosData = resFile;
            //Assign csv data
            ReadCsvBoardParams(csvParams);
            //Position board data via bone tree
            foreach (var model in resFile.Models.Values) {
                foreach (var bone in model.Skeleton.Bones.Values) {
                    //Only use pos### used for placement objects
                    if (!int.TryParse(bone.Name.Replace("hook_", ""), out int id))
                        continue;

                    var space = FindSpaceFromBoneKey(bone.Name);
                    //If no space found, then it is used as a custom placement for NPC, star host, model, etc
                    if (space == null)
                    {
                        space = new SpaceNode(this, SpaceTypes.BRANCH);
                        space.ID = id;
                        space.UpdateHeader();
                        Spaces.Add(space);
                    }
                    space.Position = new OpenTK.Vector3(
                                     bone.Position.X, bone.Position.Y, bone.Position.Z) * GLContext.PreviewScale;
                    space.Rotation = new OpenTK.Vector3(
                                            bone.Rotation.X, bone.Rotation.Y, bone.Rotation.Z);
                    space.Scale = new OpenTK.Vector3(
                                           bone.Scale.X, bone.Scale.Y, bone.Scale.Z);
                }
            }
        }
        private void SaveSpaceData(string modelHookPath, string csvParamPath)
        {
            var modelPosData = MapArchive.FileLookup[modelHookPath];
            var csvData = MapArchive.FileLookup[csvParamPath];
            this.PathRender.UpdateChildren();

            //Update children
            foreach (SpaceNode space in Spaces)
            {
                //Update child based on renderer

                space.ChildrenIDs.Clear();
                foreach (SpaceNode child in space.Children)
                    space.ChildrenIDs.Add(child.ID);
            }

            var skeleton = SpacePosData.Models[0].Skeleton;

            //Remove unused spaces (pos##).
            //We could clear them all but we want to keep original parenting info and extra bone data
            List<string> bonesToRemove = new List<string>();
            foreach (var bone in skeleton.Bones)
            {
                //Only use pos### used for placement objects
                if (!int.TryParse(bone.Key.Replace("pos", ""), out int id))
                    continue;

                var space = Spaces.FirstOrDefault(x => $"pos{((SpaceNode)x).ID.ToString().PadLeft(3, '0')}" == bone.Key);
                if (space == null)
                    bonesToRemove.Add(bone.Key);
            }

            foreach (var bone in bonesToRemove)
                skeleton.Bones.RemoveKey(bone);

            //Save space placements
            foreach (SpaceNode space in Spaces)
            {
                //Insert bone data
                var name = $"pos{space.ID.ToString().PadLeft(3, '0')}";
                if (!skeleton.Bones.ContainsKey(name))
                    skeleton.Bones.Add(name, new Bone());

                skeleton.Bones[name].Name = name;
                skeleton.Bones[name].FlagsRotation = BoneFlagsRotation.EulerXYZ;
                skeleton.Bones[name].Position = new Syroot.Maths.Vector3F(
                    space.Position.X, space.Position.Y, space.Position.Z);
                skeleton.Bones[name].Rotation = new Syroot.Maths.Vector4F(
                              space.Rotation.X, space.Rotation.Y, space.Rotation.Z, 1.0f);
                skeleton.Bones[name].Scale = new Syroot.Maths.Vector3F(
                    space.Scale.X, space.Scale.Y, space.Scale.Z);
                skeleton.Bones[name].FlagsTransform = SetBoneFlags(space);
            }

            var mem = new MemoryStream();
            SpacePosData.Save(mem);
            modelPosData.SetFileData(mem.ToArray());
            //Save board params
            csvData.SetFileData(WriteCsvBoardParams());
        }

        private static BoneFlagsTransform SetBoneFlags(SpaceNode space)
        {
            BoneFlagsTransform flags = BoneFlagsTransform.None;
            if (space.Position == OpenTK.Vector3.Zero)
                flags |= BoneFlagsTransform.TranslateZero;
            if (space.Rotation == OpenTK.Vector3.Zero)
                flags |= BoneFlagsTransform.RotateZero;
            if (space.Scale == OpenTK.Vector3.One)
                flags |= BoneFlagsTransform.ScaleOne;
            return flags;
        }

        private byte[] WriteCsvBoardParams()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                writer.WriteLine(",,,,,,,,,,,,,,");
                foreach (SpaceNode space in Spaces) {
                   // if (space.SpaceType == SpaceTypes.CUSTOM)
                    //    continue;

                    string line = string.Join(',', space.GetParams());
                    writer.WriteLine(line);
                    Console.WriteLine($"Saving {line}");
                }
                return NKN.Encrypt(writer.ToString());
            }
        }

        private void ReadCsvBoardParams(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                //Skip first line
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    Console.WriteLine(line);

                    if (line.Split(",").Length > 4)
                        Spaces.Add(new SpaceNode(this, line));
                }
            }

            //Handle children
            foreach (SpaceNode space in Spaces) {
                foreach (var childID in space.ChildrenIDs) {
                    var childSpace = Spaces.FirstOrDefault(x => ((SpaceNode)x).ID == childID);
                    if (childSpace != null)
                        space.Children.Add(childSpace);
                }
            }
        }

        private SpaceNode FindSpaceFromBoneKey(string name)
        {
            if (!name.StartsWith("hook_"))
                return null;

            var id = name.Replace("hook_", "");
            foreach (SpaceNode space in this.Spaces)
            {
                if (space.ID.ToString().PadLeft(3, '0') == id)
                    return space;
            }
            return null;
        }

        public class SpaceNode : Space
        {
            public override float BaseScale => 1.5f;

            [BindGUI("ID")]
            public int ID
            {
                get { return id; }
                set
                {
                    id = value;
                    UpdateHeader();
                }
            }
            private int id;

            [BindGUI("Type")]
            public SpaceTypes SpaceType
            {
                get {
                    if (string.IsNullOrEmpty(typeID))
                        return SpaceTypes.BRANCH;    
                    
                    return (SpaceTypes)Enum.Parse(typeof(SpaceTypes), typeID); }
                set {

                    GLContext.ActiveContext.Scene.AddToUndo(new UndoableProperty(this, "TypeID", typeID, value));

                    if (value == SpaceTypes.BRANCH)
                        TypeID = "";
                    else
                        TypeID = value.ToString();
                }
            }

            public string TypeID
            {
                get {
                    return typeID;

                    if (typeID == null) return "";

                    if (string.IsNullOrEmpty(typeID))
                        return MapStudio.UI.TranslationSource.GetText("EMPTY");

                    return MapStudio.UI.TranslationSource.GetText(typeID);
                }
                set
                {
                    typeID = value;
                    UpdateHeader();
                    GLContext.ActiveContext.UpdateViewport = true;
                }
            }
            private string typeID;

            public string Attribute1 { get; set; }
            public string Attribute2 { get; set; }

            public List<int> ChildrenIDs = new List<int>();

            public override Space Copy()
            {
                var board = ((BoardPathRenderer)this.PathPoint.ParentPath).BoardLoader;
                var space = new SpaceNode(board, this.SpaceType);
                space.Attribute1 = this.Attribute1;
                space.Attribute2 = this.Attribute2;

                return space;
            }

            public void UpdateHeader() {
                //Update UI display
                this.Name = $"{ID.ToString().PadLeft(3, '0')}_{TypeID}";

                Icon = "";
                if (MapStudio.UI.IconManager.HasIcon(SpaceType.ToString()))
                    Icon = SpaceType.ToString();
            }

            public string[] GetParams()
            {
                string[] parameters = new string[8];
                //Space ID
                parameters[0] = ID.ToString();

                //Children IDs
                for (int i = 0; i < ChildrenIDs.Count; i++)
                    parameters[i + 1] = ChildrenIDs[i].ToString();

                //Type ID
                parameters[5] = typeID;
                //Properties
                parameters[6] = Attribute1;
                parameters[7] = Attribute2;

                return parameters;
            }

            public SpaceNode(BoardLoader boardLoader, string line) : base() {
                ParseEntry(line);
            }

            public SpaceNode(BoardLoader boardLoader, SpaceTypes type) : base() {
                Init(boardLoader, type.ToString());
            }

            public SpaceNode(BoardLoader boardLoader) : base() {
                string TYPE = "PLUS";
                if (!string.IsNullOrEmpty(BoardLoader.ActiveSpaceType))
                    TYPE = BoardLoader.ActiveSpaceType;

                Init(boardLoader, TYPE);
            }

            private void Init(BoardLoader boardLoader, string type)
            {
                int ID = 0;

                //Search through each ID and find next ID usable in the list
                //Some IDs tend to go higher depending on the type
                var ids = boardLoader.Spaces.Select(x => ((SpaceNode)x).ID).OrderBy(x => x).ToList();
                foreach (var id in ids)
                {
                    int spaceID = id + 1;
                    if (!ids.Contains(spaceID))
                    {
                        ID = spaceID;
                        break;
                    }
                }
                ParseEntry($"{ID},,,,,{type},,,,,,,,,");
            }

            public override void Render(GLContext context)
            {
                if (SpaceIcons.ContainsKey(this.SpaceType))
                    Material.DiffuseTextureID = SpaceIcons[this.SpaceType].ID;
                if (SpaceColors.ContainsKey(this.SpaceType))
                    Material.Color = SpaceColors[this.SpaceType];

                base.Render(context);
            }

            public override void DrawUI()
            {
                return;
            }

            private void ParseEntry(string line)
            {
                string[] values = line.Split(',');
                ID = int.Parse(values[0]);
                for (int i = 0; i < 4; i++)
                {
                    var index = values[1 + i];
                    if (index != string.Empty)
                        ChildrenIDs.Add(ushort.Parse(index));
                }
                TypeID = values[5];
                Attribute1 = values[6];
                Attribute2 = values[7];

                UpdateHeader();
            }
        }

        public enum SpaceTypes
        {
            PLUS,
            MINUS,
            LUCKY,
            LUCKY_2,
            DONT_ENTRY,
            HATENA_1,
            HATENA_2,
            HATENA_3,
            HATENA_4,
            START,
            MARK_PC,
            MARK_STAR,
            MARK_STAROBJ,
            SUPPORT,
            HAPPENING,
            ITEM,
            BATTAN,
            DOSSUN,
            JUGEMU_OBJ,
            JUGEMU,
            JOYCON,
            TREASURE_OBJ,
            SHOP_OBJ,
            SHOP_A,
            SHOP_B,
            SHOP_C,
            HARD_SHOP,
            ICECREAM_SHOP,
            CLAYPIPE,
            CLAYPIPE_RED,
            PRESENT_BOX,
            PRESENT_BOX_OBJ,
            TURNOUT_SWITCH,
            NPC_BOMBHEI,
            PATAPATA,
            PATAPATA_OBJ,
            SIGNBOARD_PATAPATA,
            SIGNBOARD_JUGEMU,
            SIGNBOARD_SHOP,
            BRANCH,
        }
    }
}
