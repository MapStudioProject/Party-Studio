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

namespace PartyStudioPlugin
{
    public class MPSA : BoardLoader
    {
        static Dictionary<SpaceTypes, GLTexture> SpaceIcons = new Dictionary<SpaceTypes, GLTexture>();
        static Dictionary<SpaceTypes, Vector4> SpaceColors = new Dictionary<SpaceTypes, Vector4>();

        public List<BfresCameraAnim> OpeningCameras = new List<BfresCameraAnim>();

        public override Type SpaceType => typeof(SpaceNode);

        public BEA MapArchive;

        private string FileName;
        private ResFile SpacePosData;

        private ShaderPack ShaderPack;

        private bool GameShaders = false;

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
            SpaceIcons.Add(SpaceTypes.KOOPA, GLTexture2D.FromBitmap(Properties.Resources.Bowser));
            SpaceIcons.Add(SpaceTypes.MARK_STAR, GLTexture2D.FromBitmap(Properties.Resources.Star));
            SpaceIcons.Add(SpaceTypes.VS, GLTexture2D.FromBitmap(Properties.Resources.Duel));
            SpaceIcons.Add(SpaceTypes.CHANCE, GLTexture2D.FromBitmap(Properties.Resources.Miracle_Space));
            SpaceIcons.Add(SpaceTypes.EVENT, GLTexture2D.FromBitmap(Properties.Resources.Lucky));
            SpaceIcons.Add(SpaceTypes.SPOT_ITEM_SHOP, GLTexture2D.FromBitmap(Properties.Resources.Shop));
            SpaceIcons.Add(SpaceTypes.SPOT_ITEM_SHOP_MAP, GLTexture2D.FromBitmap(Properties.Resources.Shop));
            SpaceIcons.Add(SpaceTypes.MARK_PC, GLTexture2D.FromBitmap(Properties.Resources.PlayerStart));
            SpaceIcons.Add(SpaceTypes.BANK, GLTexture2D.FromBitmap(Properties.Resources.Bank));
            SpaceIcons.Add(SpaceTypes.START, GLTexture2D.FromBitmap(Properties.Resources.Start));
            SpaceIcons.Add(SpaceTypes.SPOT_TERESA, GLTexture2D.FromBitmap(Properties.Resources.Teresa));
            SpaceIcons.Add(SpaceTypes.SPOT_NOKONOKO, GLTexture2D.FromBitmap(Properties.Resources.Koopa));
            SpaceIcons.Add(SpaceTypes.SPOT_BRANCH_KEY, GLTexture2D.FromBitmap(Properties.Resources.Key));
            SpaceIcons.Add(SpaceTypes.SPOT_EVENT_1, GLTexture2D.FromBitmap(Properties.Resources.BoardEvent));
            SpaceIcons.Add(SpaceTypes.SPOT_EVENT_2, GLTexture2D.FromBitmap(Properties.Resources.BoardEvent));
            SpaceIcons.Add(SpaceTypes.SPOT_EVENT_3, GLTexture2D.FromBitmap(Properties.Resources.BoardEvent));
            SpaceIcons.Add(SpaceTypes.SPOT_EVENT_4, GLTexture2D.FromBitmap(Properties.Resources.BoardEvent));
            SpaceIcons.Add(SpaceTypes.SPOT_EVENT_5, GLTexture2D.FromBitmap(Properties.Resources.BoardEvent));
            SpaceIcons.Add(SpaceTypes.SPOT_EVENT_6, GLTexture2D.FromBitmap(Properties.Resources.BoardEvent));

            SpaceColors.Add(SpaceTypes.CUSTOM, new Vector4(0, 0, 0, 1));

            foreach (var icon in SpaceIcons)
                MapStudio.UI.IconManager.TryAddIcon(icon.Key.ToString(), icon.Value);

            //Space types for toolbox view
        }

        public MPSA()
        {
            //Load a list of all spaces used in the game.
            //This will be added to the tool menu for adding and selecting space types.
            foreach (SpaceTypes icon in Enum.GetValues(typeof(SpaceTypes)))
            {
                //Skip dupe types
                if (icon == SpaceTypes.HATENA_2 ||
                    icon == SpaceTypes.HATENA_3 ||
                    icon == SpaceTypes.SPOT_EVENT_2 ||
                    icon == SpaceTypes.SPOT_EVENT_3 ||
                    icon == SpaceTypes.SPOT_EVENT_4 ||
                    icon == SpaceTypes.SPOT_EVENT_5 ||
                    icon == SpaceTypes.SPOT_EVENT_6)
                    continue;

                SpaceTypeList.Add(icon.ToString());
            }
        }

        public override void LoadFile(MapEditorPlugin mapEditor, Stream data, string fileName)
        {
            LoadIcons();

            GLContext.PreviewScale = 10;

            MapArchive = new BEA();
            MapArchive.Load(data);

            //Load shaders if game shaders are used 
            if (GameShaders)
            {
                foreach (var file in MapArchive.FileLookup.Values)
                {
                    if (file.FileName.EndsWith("bnbshpk"))
                        ShaderPack = new ShaderPack(file.FileData);
                }
            }

            var folder = Path.GetDirectoryName(fileName);
            if (File.Exists($"{folder}\\hsbd00.nx.bea"))
                LoadBoardSpaceAssets($"{folder}\\hsbd00.nx.bea");

            FileName = Path.GetFileName(fileName);
            var id = FileName.Split('.').FirstOrDefault();
            var modelHook = $"scene/{id}/model/{id}_pos.fmdb";
            var csvParams = $"scene/{id}/data/{id}_map.nkn";
            var texPath = $"scene/{id}/model/textures/";
            var texBntxPath = $"scene/{id}/model/textures_{id}.bntx";
            var envFolder = $"scene/{id}/env";

            LoadEnvAssets(envFolder, id);

            if (MapArchive.FileLookup.ContainsKey(modelHook)) {
                var modelPosData = MapArchive.FileLookup[modelHook].FileData;
                var csvData = MapArchive.FileLookup[csvParams].FileData;

                LoadSpaceData(new ResFile(modelPosData), NKN.Decrypt(csvData));
            }

            List<BntxTexture> textures = new List<BntxTexture>();
            if (MapArchive.FileLookup.ContainsKey(texBntxPath))
                textures = LoadTextures(MapArchive.FileLookup[texBntxPath].FileData);

            Dictionary<string, GenericRenderer.TextureView> textureViews = new Dictionary<string, GenericRenderer.TextureView>();
            foreach (var tex in textures)
                textureViews.Add(tex.Name, new GenericRenderer.TextureView(tex));

            if (GameShaders)
            {
                BfresLoader.GlobalShaders.Clear();
                BfresLoader.GlobalShaders.AddRange(this.ShaderPack.Shaders.Values.ToList());
                BfresLoader.TargetShader = typeof(MPSARender);
            }

            foreach (var file in MapArchive.FileLookup)
            {
                if (!file.Key.StartsWith($"scene/{id}/model/{id}_bg") || !file.Key.EndsWith(".fmdb"))
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

        public override void SaveFile(MapEditorPlugin mapEditor, Stream data)
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

        private void LoadSpaceData(ResFile resFile, string csvParams)
        {
            SpacePosData = resFile;
            //Assign csv data
            ReadCsvBoardParams(csvParams);
            //Position board data via bone tree
            foreach (var model in resFile.Models.Values) {
                foreach (var bone in model.Skeleton.Bones.Values) {
                    //Only use pos### used for placement objects
                    if (!int.TryParse(bone.Name.Replace("pos", ""), out int id))
                        continue;

                    var space = FindSpaceFromBoneKey(bone.Name);
                    //If no space found, then it is used as a custom placement for NPC, star host, model, etc
                    if (space == null)
                    {
                        space = new SpaceNode(this, SpaceTypes.CUSTOM);
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

            //Prepare links
            foreach (SpaceNode space in Spaces)
            {
                if (!string.IsNullOrEmpty(space.LinkedSpace))
                {
                    var linked = (SpaceNode)Spaces.FirstOrDefault(x => ((SpaceNode)x).ID.ToString() == space.LinkedSpace);
                    linked.SourceLink = space;
                    space.DestLink = space;
                    space.UpdateHeader();
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
                    if (space.SpaceType == SpaceTypes.CUSTOM)
                        continue;

                    string line = string.Join(',', space.GetParams());
                    writer.WriteLine(line);
                    Console.WriteLine($"Saving {line}");
                }
                return NKN.Encrypt(writer.ToString());
            }
        }

        private void ReadCsvBoardParams(string csvParams)
        {
            foreach (var line in csvParams.Split('\n', '\r'))
            {
                if (line.StartsWith(","))
                    continue;

                Console.WriteLine(line);

                if (line.Split(",").Length > 4)
                    Spaces.Add(new SpaceNode(this, line));
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

        private List<BntxTexture> LoadTextures(Stream stream)
        {
            List<BntxTexture> textures = new List<BntxTexture>();
            var bntx = new BntxFile(stream);
            foreach (var tex in bntx.Textures)
                textures.Add(new BntxTexture(bntx, tex));
            return textures;
        }

        private SpaceNode FindSpaceFromBoneKey(string name)
        {
            if (!name.StartsWith("pos"))
                return null;

            var id = name.Replace("pos", "");
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
                        return SpaceTypes.EMPTY;    
                    
                    return (SpaceTypes)Enum.Parse(typeof(SpaceTypes), typeID); }
                set {

                    GLContext.ActiveContext.Scene.AddToUndo(new UndoableProperty(this, "TypeID", typeID, value));

                    if (value == SpaceTypes.EMPTY)
                        TypeID = "";
                    else
                        TypeID = value.ToString();
                }
            }

            public string TypeID
            {
                get {
                    if (string.IsNullOrEmpty(typeID))
                        return MapStudio.UI.TranslationSource.GetText("EMPTY");

                    return MapStudio.UI.TranslationSource.GetText(typeID); }
                set
                {
                    typeID = value;
                    UpdateHeader();
                    GLContext.ActiveContext.UpdateViewport = true;
                }
            }
            private string typeID;

            [BindGUI("Linked Space")]
            public string LinkedSpace
            {
                get { return Properties[0]; }
                set { Properties[0] = value; }
            }

            [BindGUI("Param 2")]
            public string Param2
            {
                get { return Properties[1]; }
                set { Properties[1] = value; }
            }

            [BindGUI("Param 3")]
            public string Param3
            {
                get { return Properties[2]; }
                set { Properties[2] = value; }
            }

            [BindGUI("Param 4")]
            public string Param4
            {
                get { return Properties[3]; }
                set { Properties[3] = value; }
            }

            [BindGUI("Param 5")]
            public string Param5
            {
                get { return Properties[4]; }
                set { Properties[4] = value; }
            }

            [BindGUI("Param 6")]
            public string Param6
            {
                get { return Properties[5]; }
                set { Properties[5] = value; }
            }

            [BindGUI("Param 7")]
            public string Param7
            {
                get { return Properties[6]; }
                set { Properties[6] = value; }
            }

            [BindGUI("Param 8")]
            public string Param8
            {
                get { return Properties[7]; }
                set { Properties[7] = value; }
            }

            [BindGUI("Param 9")]
            public string Param9
            {
                get { return Properties[8]; }
                set { Properties[8] = value; }
            }

            public SpaceNode SourceLink { get; set; }
            public SpaceNode DestLink { get; set; }

            public List<int> ChildrenIDs = new List<int>();
            public string[] Properties = new string[9];

            public override Space Copy()
            {
                var board = ((BoardPathRenderer)this.PathPoint.ParentPath).BoardLoader;
                var space = new SpaceNode(board, this.SpaceType);

                space.Properties = new string[Properties.Length];
                for (int i = 0; i < Properties.Length; i++)
                    space.Properties[i] = Properties[i];

                return space;
            }

            public void UpdateHeader() {
                //Update UI display
                this.Name = $"{ID.ToString().PadLeft(3, '0')}_{TypeID}";

                Icon = "";
                if (MapStudio.UI.IconManager.HasIcon(SpaceType.ToString()))
                    Icon = SpaceType.ToString();

                if (SourceLink != null)
                    SetupLinkType(SourceLink);
            }

            private void SetupLinkType(SpaceNode linkedSpace)
            {
                if (linkedSpace.SpaceType == SpaceTypes.MARK_STAR)
                    this.Name = $"{ID.ToString().PadLeft(3, '0')}_STAR_HOST";
                if (linkedSpace.SpaceType == SpaceTypes.SPOT_ITEM_SHOP)
                    this.Name = $"{ID.ToString().PadLeft(3, '0')}_SHOP_MODEL";
                if (linkedSpace.SpaceType == SpaceTypes.SPOT_KOOPA)
                    this.Name = $"{ID.ToString().PadLeft(3, '0')}_BOWSER_MODEL";
                if (linkedSpace.SpaceType == SpaceTypes.SPOT_TERESA)
                    this.Name = $"{ID.ToString().PadLeft(3, '0')}_BOO_MODEL";
                if (linkedSpace.SpaceType == SpaceTypes.SPOT_BRANCH_KEY)
                    this.Name = $"{ID.ToString().PadLeft(3, '0')}_KEYGATE_MODEL";
                if (linkedSpace.SpaceType == SpaceTypes.BANK)
                    this.Name = $"{ID.ToString().PadLeft(3, '0')}_BANK_MODEL";
            }

            public string[] GetParams()
            {
                string[] parameters = new string[Properties.Length + 6];
                //Space ID
                parameters[0] = ID.ToString();

                //Children IDs
                for (int i = 0; i < ChildrenIDs.Count; i++)
                    parameters[i + 1] = ChildrenIDs[i].ToString();

                //Type ID
                parameters[5] = typeID;
                //Properties
                for (int i = 0; i < Properties.Length; i++)
                    parameters[i + 6] = Properties[i];

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

                ImGuiNET.ImGui.Columns(2);
                for (int i = 0; i < Properties.Length; i++)
                {
                    ImGuiNET.ImGui.Text($"Property {i}");
                    ImGuiNET.ImGui.NextColumn();

                    string prop = Properties[i];
                    if (ImGuiNET.ImGui.InputText($"##prop{i}", ref prop, 0x100))
                        Properties[i] = prop;

                    ImGuiNET.ImGui.NextColumn();
                }
                ImGuiNET.ImGui.Columns(1);
            }

            private void ParseEntry(string line)
            {
                string[] values = line.Split(',');
                ID = int.Parse(values[0]);
                for (int i = 0; i < 4; i++)
                {
                    var index = values[1 + i];
                    if (index != string.Empty)
                        ChildrenIDs.Add(int.Parse(values[1 + i]));
                }
                TypeID = values[5];
                //Totals to 9 properties
                for (int i = 6; i < values.Length; i++)
                    Properties[i - 6] = values[i];

                UpdateHeader();
            }
        }

        public enum SpaceTypes
        {
            EMPTY,
            CUSTOM,
            START,
            PLUS,
            MINUS,
            ITEM,
            BANK,
            EVENT,
            CHANCE,
            VS,
            KOOPA,
            MARK_PC,
            MARK_STAR,
            HATENA_1,
            HATENA_2,
            HATENA_3,
            SPOT_BRANCH,
            SPOT_BRANCH_KEY,
            SPOT_NOKONOKO,
            SPOT_KOOPA,
            SPOT_ITEM_SHOP,
            SPOT_ITEM_SHOP_MAP,
            SPOT_EVENT_1,
            SPOT_EVENT_2,
            SPOT_EVENT_3,
            SPOT_EVENT_4,
            SPOT_EVENT_5,
            SPOT_EVENT_6,
            SPOT_TERESA,
        }
    }
}
