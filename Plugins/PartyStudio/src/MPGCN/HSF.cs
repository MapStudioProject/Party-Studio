using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using MPLibrary.GCN;
using MapStudio.UI;
using Toolbox.Core.ViewModels;
using ImGuiNET;
using GCNRenderLibrary.Rendering;
using GLFrameworkEngine;
using Toolbox.Core.Animations;
using UIFramework;
using MPLibrary.GCWii.HSF;
using System.IO;
using PartyStudioPlugin;

namespace PartyStudio.GCN
{
    public class HSF : FileEditor, IFileFormat, IDisposable, IContextMenu
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "Mario Party GCN Resource" };
        public string[] Extension { get; set; } = new string[] { "*.hsf" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            using (var reader = new FileReader(stream, true)) {
                return reader.CheckSignature(4, "HSFV");
            }
        }

        static List<HSF> openedFiles = new List<HSF>();

        public HsfFile Header;

        public HsfRender Render;

        LightObject LightObject = new LightObject();

        NodeBase nodeFolder = new NodeBase("Nodes");
        NodeBase animFolder = new NodeBase("Animations");

        List<ObjectNodeWrapper> MeshList = new List<ObjectNodeWrapper>();

        public List<AnimationWrapper> Animations = new List<AnimationWrapper>();

        public static HSF LoadFile(Stream stream)
        {
            HSF hsf = new HSF();
            hsf.Load(stream);
            return hsf;
        }

        public void Load(System.IO.Stream stream)
        {
            Header = new HsfFile(stream);
            Render = new HsfRender(Header);
            AddRender(Render);
            ReloadHSF();

            openedFiles.Add(this);
        }

        public void Dispose()
        {
            openedFiles.Remove(this);
            Render?.Dispose();
        }

        public void ReloadRender()
        {
            Render.Reload(Header);
        }

        public void ReloadHSF()
        {
            foreach (var node in this.Header.ObjectNodes)
            {
                if (node.Data.Type != ObjectType.Camera && node.Data.Type != ObjectType.Light)
                {
                    Console.WriteLine($"ParentIndex {node.Data.ParentIndex} {node.Data.ChildrenCount}");
                }
            }

            Root.Children.Clear();

            Root.AddChild(new FogWrapper(Header, Render));

            NodeBase matFolder = new NodeBase("Materials");
            Root.AddChild(matFolder);

            NodeBase texFolder = new TextureFolder(Header, Render);
            Root.AddChild(texFolder);

            nodeFolder.Children.Clear();
            animFolder.Children.Clear();

            Root.AddChild(nodeFolder);
            Root.AddChild(animFolder);

            foreach (var motion in Header.MotionData.Animations)
            {
                AnimationWrapper anim = new AnimationWrapper(Header, Render, motion);

                NodeBase animNode = new MotionWrapper(anim, motion);
                animNode.Tag = anim;
                animNode.ContextMenus.AddRange(anim.GetMenuItems());
                animNode.Icon = IconManager.SKELEAL_ANIM_ICON.ToString();
                animFolder.AddChild(animNode);

                this.Animations.Add(anim);
            }


            ObjectNodeWrapper[] nodes = new ObjectNodeWrapper[Header.ObjectNodes.Count];
            for (int i = 0; i < Header.ObjectNodes.Count; i++)
            {
                string name = Header.ObjectNodes[i].Name;
                var objNode = Header.ObjectNodes[i];

                nodes[i] = new ObjectNodeWrapper(objNode, name, Render);
                if (objNode.Data.ParentIndex == -1)
                    nodeFolder.AddChild(nodes[i]);

                if (!objNode.HasHierachy())
                    nodeFolder.AddChild(nodes[i]);

                nodes[i].OnSelected += delegate
                {
                    //Material animations
                    var anim = animFolder.Children.FirstOrDefault();
                    if (anim != null)
                    {
                        var a = anim.Tag as AnimationWrapper;
                        Workspace.GraphWindow.AddAnimation(a);
                        a.ReloadTree(objNode, objNode.AnimationData);
                    }
                };
            }

            STSkeleton skeleton = new STSkeleton();
            for (int i = 0; i < Header.ObjectNodes.Count; i++)
            {
                string name = Header.ObjectNodes[i].Name;
                var objNode = Header.ObjectNodes[i].Data;

                STBone bone = new STBone(skeleton, name);
                if (objNode.ParentIndex >= -1 && objNode.ParentIndex < Header.ObjectNodes.Count)
                {
                    bone.Position = new Vector3(
                        objNode.BaseTransform.Translate.X,
                        objNode.BaseTransform.Translate.Y,
                        objNode.BaseTransform.Translate.Z);
                    bone.EulerRotationDegrees = new Vector3(
                        objNode.BaseTransform.Rotate.X,
                        objNode.BaseTransform.Rotate.Y,
                        objNode.BaseTransform.Rotate.Z);
                    bone.Scale = new Vector3(
                        objNode.BaseTransform.Scale.X,
                        objNode.BaseTransform.Scale.Y,
                        objNode.BaseTransform.Scale.Z);
                }

                skeleton.Bones.Add(bone);
            }

            skeleton.Reset();
            skeleton.Update();

            for (int i = 0; i < Header.ObjectNodes.Count; i++)
            {
                var objNode = Header.ObjectNodes[i].Data;
                if (objNode.ParentIndex > -1 && objNode.ParentIndex < Header.ObjectNodes.Count)
                    skeleton.Bones[i].ParentIndex = objNode.ParentIndex;
            }

            Render.SkeletonRenderer = new SkeletonRenderer(skeleton);

            Render.OnMeshSelected = null;
            Render.OnMeshSelected += (node, scene) =>
            {
                //deselect all materials
                foreach (MaterialWrapper mat in matFolder.Children)
                    mat.IsSelected = false;

                //Select material that is linked to the mesh
                foreach (MaterialWrapper mat in matFolder.Children)
                {
                    if (mat.ObjectData == node.Data)
                    {
                        mat.DisplayMaterialUI?.Invoke();
                        mat.IsSelected = true;
                    }
                }
            };

            MeshList.Clear();
            for (int i = 0; i < Header.ObjectNodes.Count; i++)
            {
                string name = Header.ObjectNodes[i].Name;
                var objNode = Header.ObjectNodes[i].Data;

                NodeBase n = nodes[i];
                n.ContextMenus.Add(new MenuItemModel("Expand All", () =>
                {
                    void Expand(NodeBase nb)
                    {
                        nb.IsExpanded = true;
                        foreach (var child in nb.Children)
                            Expand(child);
                    }
                    Expand(n);
                }));
                n.ContextMenus.Add(new MenuItemModel("Collapse All", () =>
                {
                    void Expand(NodeBase nb)
                    {
                        nb.IsExpanded = false;
                        foreach (var child in nb.Children)
                            Expand(child);
                    }
                    Expand(n);
                }));

                if (objNode.ParentIndex > -1 && objNode.ParentIndex < Header.ObjectNodes.Count)
                    nodes[objNode.ParentIndex].AddChild(nodes[i]);

                switch (objNode.Type)
                {
                    case ObjectType.Light:
                        n.Icon = IconManager.LIGHT_ICON.ToString();
                        break;
                    case ObjectType.Camera:
                        n.Icon = IconManager.CAMERA_ICON.ToString();
                        break;
                    case ObjectType.Effect:
                        n.Icon = IconManager.PARTICLE_ICON.ToString();
                        break;
                    case ObjectType.Joint:
                        n.Icon = IconManager.BONE_ICON.ToString();
                        break;
                    case ObjectType.Root:
                        break;
                }

                if (objNode.Type == ObjectType.Mesh)
                {
                    var mesh = Header.Meshes.FirstOrDefault(x => x.ObjectData == objNode);
                    n.Icon = IconManager.MESH_ICON.ToString();

                    n.OnSelected += delegate
                    {
                        foreach (var mesh in mesh.GXMeshes.Values)
                            mesh.SceneNode.IsSelected = n.IsSelected;
                    };

                    MeshList.Add(n as ObjectNodeWrapper);

                    var materials = mesh.Primitives.Select(x => x.MaterialIndex).Distinct();

                    foreach (var matID in materials)
                    {
                        var mat = Header.Materials[matID];

                        MaterialWrapper matNode = new MaterialWrapper(Header, Render, mat, objNode, mesh.GXMeshes[matID]);
                        matNode.Icon = IconManager.MATERIAL_OPAQUE_ICON.ToString();
                        matNode.Header = name;
                        matNode.Icon = $"{name}{matID}_mat";
                        matNode.ReloadIcon();

                        matNode.OnSelected += delegate
                        {
                            //Select visual for render
                           // mesh.GXMeshes[matID].SceneNode.IsSelected = matNode.IsSelected;
                            matNode.DisplayMaterialUI?.Invoke();
                        };

                        mesh.OnSelected += delegate
                        {
                            matNode.IsSelected = true;
                            matNode.DisplayMaterialUI?.Invoke();
                        };

                        matNode.DisplayMaterialUI = () =>
                        {
                            //Material animations
                            var anim = animFolder.Children.FirstOrDefault();
                            if (anim != null)
                            {
                                var a = anim.Tag as AnimationWrapper;
                                Workspace.GraphWindow.AddAnimation(a);
                                a.ReloadTree(mat, mat.AnimationData);
                            }
                        };
                        matNode.DisplayTextureAnimationUI = (i) =>
                        {
                            var anim = animFolder.Children.FirstOrDefault();
                            if (anim != null && mat.TextureAttributes.Count > i)
                            {
                                var tex = mat.TextureAttributes[i];

                                var a = anim.Tag as AnimationWrapper;
                                Workspace.GraphWindow.AddAnimation(a);
                                a.ReloadTree(tex, tex.AnimationData);
                            }
                        };
                        matFolder.AddChild(matNode);

                        nodes[i].MaterialWrapper = matNode;
                    }
                }
            }

            foreach (var mat in Header.Materials)
            {

            }

            if (Header.MotionData.Animations.Count == 0)
            {
                var createNode = new TreeNode("Create Animation")
                {
                    Icon = IconManager.ADD_ICON.ToString(),
                };
                createNode.OnDoubleClick += delegate
                {
                    CreateAnimation();

                    UIManager.ActionExecBeforeUIDraw += delegate
                    {
                        Workspace.GraphWindow.ClearNodes();
                    };
                };
                Workspace.GraphWindow.ClearNodes();
                Workspace.GraphWindow.AddNode(createNode);
            }
        }

        private void CreateAnimation()
        {
            var motion = new HSFMotionAnimation()
            {
                Name = "opt_evalScene",
                FrameCount = 400,
            };
            Header.MotionData.Animations.Add(motion);

            AnimationWrapper anim = new AnimationWrapper(Header, Render, motion);

            NodeBase animNode = new MotionWrapper(anim, motion);
            animNode.Tag = anim;
            animNode.ContextMenus.AddRange(anim.GetMenuItems());
            animNode.Icon = IconManager.SKELEAL_ANIM_ICON.ToString();
            animFolder.AddChild(animNode);
        }

        public void Save(System.IO.Stream stream)
        {
            Header.Save(stream);
        }

        /// <summary>
        /// Prepares the dock layouts to be used for the file format.
        /// </summary>
        public override List<DockWindow> PrepareDocks()
        {
            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(Workspace.PropertyWindow);
            windows.Add(Workspace.ConsoleWindow);
            windows.Add(Workspace.ViewportWindow);
            windows.Add(Workspace.TimelineWindow);
            windows.Add(Workspace.GraphWindow);
            return windows;
        }

        public MenuItemModel[] GetContextMenuItems()
        {
            List<MenuItemModel> items = new List<MenuItemModel>();
            items.Add(new MenuItemModel("Export Model", Export));
            items.Add(new MenuItemModel("Replace Model", Replace));

            return items.ToArray();
        }

        private void Export()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.SaveDialog = true;
            dlg.AddFilter(".dae", "Dae");
            dlg.AddFilter(".json", "Json");
            dlg.AddFilter(".gltf", "gltf");
            dlg.FileName = this.Header + ".dae";

            if (dlg.ShowDialog())
            {
                if (dlg.FilePath.EndsWith(".json"))
                    HSFJsonExporter.Export(Header, dlg.FilePath);
                else
                    HSFModelImporter.Export(Header, dlg.FilePath);
            }
        }

        private void Replace()
        {
            ImguiFileDialog dlg = new ImguiFileDialog();
            dlg.AddFilter(".fbx", "Fbx");
            dlg.AddFilter(".dae", "Dae");
            dlg.AddFilter(".obj", "Obj");
            dlg.AddFilter(".gltf", "gltf");
            dlg.AddFilter(".glb", "glb");

            if (dlg.ShowDialog())
            {
                var imported_hsf = HSFModelImporter.Import(dlg.FilePath, Header, new HSFModelImporter.ImportSettings()
                {

                });

                this.Header.ObjectNodes.Clear();
                this.Header.ObjectNodes.AddRange(imported_hsf.ObjectNodes);

                this.Header.Meshes.Clear();
                this.Header.Meshes.AddRange(imported_hsf.Meshes);

                this.Header.Materials.Clear();
                this.Header.Materials.AddRange(imported_hsf.Materials);

                this.Header.Textures.Clear();
                this.Header.Textures.AddRange(imported_hsf.Textures);

                this.Header.SkeletonData = imported_hsf.SkeletonData;
                this.Header.MatrixData = imported_hsf.MatrixData;

                this.Render.Reload(this.Header);

                ReloadHSF();
                CanSave = true;
            }
        }

        public class FogWrapper : NodeBase
        {
            HsfFile HsfFile;
            HsfRender Render;

            public FogWrapper(HsfFile hsf, HsfRender render)
            {
                HsfFile = hsf;
                Render = render;
                Header = "Fog Data";
                Tag = hsf.FogData;
                TagUI.UIDrawer += delegate
                {
                    FogUI.Render(HsfFile, Render);
                };
            }
        }

        private class MotionWrapper : NodeBase
        {
            HSFMotionAnimation Anim;

            private string Josn = "";

            public MotionWrapper(AnimationWrapper wrapper, HSFMotionAnimation anim)
            {
                Anim = anim;
                Header = anim.Name;

                Tag = anim;

                AnimationPropertyUI drawer = new AnimationPropertyUI();

                TagUI.UIDrawer += delegate
                {
                    drawer.Render(anim, wrapper.Json);

                  //  ImGui.TextUnformatted(wrapper.Json);
                };
            }
        }

        public class MaterialWrapper : NodeBase, IPropertyUI
        {
            public HsfRender Render;

            Material Material;
            HsfFile HsfFile;
            GXMesh Mesh;
            public HSFObjectData ObjectData;

            public Action<int> DisplayTextureAnimationUI;
            public Action DisplayMaterialUI;

            public bool HasLightmap
            {
                get { return (ObjectData.RenderFlags & HsfGlobals.HIGHLIGHT_ENABLE) != 0; }
                set
                {
                    if (value)
                        ObjectData.RenderFlags |= HsfGlobals.HIGHLIGHT_ENABLE;
                    else
                        ObjectData.RenderFlags &= ~HsfGlobals.HIGHLIGHT_ENABLE;
                }
            }

            public Type GetTypeUI() => typeof(MaterialUI);

            public void OnLoadUI(object uiInstance)
            {
                ((MaterialUI)uiInstance).Init(this, Material);
            }

            public void OnRenderUI(object uiInstance)
            {
                ((MaterialUI)uiInstance).Render();
            }

            public void ReloadDrawList() => this.Render.ReloadDrawList();

            public MaterialWrapper(HsfFile hsfFile, HsfRender render, Material mat, HSFObjectData objNode, GXMesh mesh)
            {
                HsfFile = hsfFile;
                Render = render;
                Material = mat;
                Header = mat.Name;
                Tag = Material;
                ObjectData = objNode;
                Mesh = mesh;

                ContextMenus.Add(new MenuItemModel("Export", Export));
                ContextMenus.Add(new MenuItemModel("Replace", Replace));
            }

            private void Export()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.FileName = this.Header + ".json";
                dlg.AddFilter(".json", "Json");
                if (dlg.ShowDialog())
                {
                    System.IO.File.WriteAllText(dlg.FilePath, HSFJsonMaterialConverter.Export(Material));
                }
            }

            private void Replace()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.AddFilter(".json", "Json");
                if (dlg.ShowDialog())
                {
                    foreach (MaterialWrapper child in Parent.Children.Where(x => x.IsSelected))
                    {
                        HSFJsonMaterialConverter.Import(child.Material, dlg.FilePath);
                    }
                }
            }

            public List<GXMesh> GetMeshes()
            {
                List<GXMesh> meshes = new List<GXMesh>();
                meshes.Add(Mesh);
                return meshes;
            }

            public void ReloadIcon()
            {
                if (IconManager.HasIcon(Icon))
                    IconManager.RemoveTextureIcon(Icon);

                IconManager.AddIcon(Icon, RenderIcon(21).ID);
            }

            static UVSphereRender MaterialSphere;

            public GLTexture RenderIcon(int size)
            {
                var context = new GLContext();
                context.Camera = new GLFrameworkEngine.Camera();

                var frameBuffer = new Framebuffer(FramebufferTarget.Framebuffer, size, size);
                frameBuffer.Bind();

                GL.Viewport(0, 0, size, size);
                GL.ClearColor(0, 0, 0, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                //Create a simple mvp matrix to render the material data
                Matrix4 modelMatrix = Matrix4.CreateTranslation(0, 0, -12);
                Matrix4 viewMatrix = Matrix4.Identity;
                Matrix4 mtxProj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), 1.0f, 1.0f, 1000f);
                Matrix4 viewProj = mtxProj * viewMatrix;

                var mat = new StandardMaterial();
                mat.HalfLambertShading = true;
                mat.DirectionalLighting = false;
                mat.CameraMatrix = viewProj;
                mat.ModelMatrix = modelMatrix;
                mat.IsSRGB = false;

                var textureMaps = Material.TextureAttributes.Select(x => x.AttributeData).ToList();
                if (textureMaps.Count > 0)
                {
                    var tex = Render.TextureCache[textureMaps[0].TextureIndex];
                    mat.DiffuseTextureID = tex.ID;
                }

                if (MaterialSphere == null)
                    MaterialSphere = new UVSphereRender(8);

                mat.Render(context);
                MaterialSphere.Draw(context);

                var thumbnail = frameBuffer.ReadImagePixels(true);

                //Disable shader and textures
                GL.UseProgram(0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.Disable(EnableCap.FramebufferSrgb);

                frameBuffer.Dispose();

                return GLTexture2D.FromBitmap(thumbnail);
            }
        }

        public class ObjectNodeWrapper : NodeBase, IPropertyUI
        {
            public HSFObjectData ObjectData;
            public HSFObject Node;

            public MaterialWrapper MaterialWrapper;

            HsfRender HsfRender;

            public override string Header { get => Node.Name; set => Node.Name = value; }

            public Action DisplayAnimationUI;

            public Type GetTypeUI() => typeof(ObjectNodeUI);

            public void OnLoadUI(object uiInstance)
            {
                ((ObjectNodeUI)uiInstance).Init(this, Node);
            }

            public void OnRenderUI(object uiInstance)
            {
                ((ObjectNodeUI)uiInstance).Render();
            }

            public ObjectNodeWrapper(HSFObject objData, string name, HsfRender render)
            {
                HsfRender = render;
                ObjectData = objData.Data;
                Tag = objData;
                Node = objData;
            }

            public void UpdateRender()
            {
                Node.UpdateMatrix();
                GLContext.ActiveContext.UpdateViewport = true;
            }
        }

        public class AnimationWrapper : STAnimation, IEditableAnimation
        {
            private HsfFile HsfFile;
            private HSFMotionAnimation Anim;
            private HsfRender Render;

            /// <summary>
            /// The tree node loaded in the animation timeline.
            /// </summary>
            public TreeNode Root { get; set; }

            public string Json = "";

            public AnimationWrapper(HsfFile hsf, HsfRender render, HSFMotionAnimation anim)
            {
                Render = render;
                HsfFile = hsf;
                Anim = anim;
                FrameCount = Anim.FrameCount;
                Name = Anim.Name;
                Json = HSFJsonAnimationConverter.Export(anim);

                Root = new TreeNode(this.Name);
            }

            public MenuItemModel[] GetMenuItems()
            {
                List<MenuItemModel> items = new List<MenuItemModel>();
                items.Add(new MenuItemModel("Export", Export));
                items.Add(new MenuItemModel("Replace", Replace));
                return items.ToArray();
            }

            private void Export()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = true;
                dlg.AddFilter("json", "Json");
                dlg.AddFilter(".dae", "dae");
                dlg.AddFilter(".gltf", "gltf");
                dlg.AddFilter(".glb", "glb");
                dlg.AddFilter(".anim", "anim");

                dlg.FileName = $"{this.Name}.json";
                if (dlg.ShowDialog())
                {
                    if (dlg.FilePath.EndsWith("json"))
                    {
                        string json = HSFJsonAnimationConverter.Export(Anim);
                        System.IO.File.WriteAllText(dlg.FilePath, json);
                    }
                    else
                    {
                        if (TryGetTargetHsfModel(out HsfFile hsfFile))
                            HSFAnimationExporter.Export(hsfFile, Anim, dlg.FilePath);
                    }
                }
            }

            private void Replace()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.AddFilter("json", "Json");
                dlg.AddFilter(".dae", "dae");
                dlg.AddFilter(".gltf", "gltf");
                dlg.AddFilter(".glb", "glb");
                dlg.AddFilter(".anim", "anim");

                dlg.FileName = $"{this.Name}.json";
                if (dlg.ShowDialog())
                {
                    if (dlg.FilePath.ToLower().EndsWith("gltf") || 
                        dlg.FilePath.ToLower().EndsWith("glb") ||
                        dlg.FilePath.ToLower().EndsWith("dae") ||
                        dlg.FilePath.ToLower().EndsWith("anim"))
                    {
                        if (TryGetTargetHsfModel(out HsfFile hsfFile))
                            HSFAnimationImporter.Import(hsfFile, Anim, dlg.FilePath); ;
                    }
                    else
                    {
                        Anim = HSFJsonAnimationConverter.Import(System.IO.File.ReadAllText(dlg.FilePath));
                    }
                    Json = HSFJsonAnimationConverter.Export(Anim);
                }
            }

            private bool TryGetTargetHsfModel(out HsfFile hsfFile)
            {
                if (this.HsfFile.ObjectNodes.Count > 0)
                {
                    hsfFile = this.HsfFile;
                    return true;
                }

                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.AddFilter("hsf", "hsf");
                dlg.FileName = "Select HSF that has the target boneset";

                if (dlg.ShowDialog())
                {
                    hsfFile = new HsfFile(dlg.FilePath);
                    if (hsfFile.ObjectNodes.Count == 0)
                    {
                        TinyFileDialog.MessageBoxErrorOk($"HSF file has no bones!");
                        return false;
                    }

                    return true;
                }

                hsfFile = null;
                return false;
            }

            public void ReloadTree(HSFObject node, AnimationNode group)
            {
                Root.Children.Clear();

                if (group.TrackList.Count == 0)
                {
                    var createNode = new TreeNode("Create Node Animation")
                    {
                        Icon = IconManager.ADD_ICON.ToString(),
                    };
                    createNode.OnDoubleClick += delegate
                    {
                        node.CreateAnimation();
                        this.Anim.AnimGroups.Add(node.AnimationData);

                        MapStudio.UI.UIManager.ActionExecBeforeUIDraw += delegate
                        {
                            ReloadTree(node, node.AnimationData);
                        };
                    };
                    Root.AddChild(createNode);
                    return;
                }

                foreach (var track in group.TrackList)
                    CreateTrack(group, track.TrackEffect);
            }

            public void ReloadTree(TextureAttribute att, AnimationNode group)
            {
                Root.Children.Clear();

                if (group.TrackList.Count == 0)
                {
                    var createNode = new TreeNode("Create Texture Animation")
                    {
                        Icon = IconManager.ADD_ICON.ToString(),
                    };
                    createNode.OnDoubleClick += delegate
                    {
                        att.CreateAnimation();
                        this.Anim.AnimGroups.Add(att.AnimationData);

                        MapStudio.UI.UIManager.ActionExecBeforeUIDraw += delegate
                        {
                            ReloadTree(att, att.AnimationData);
                        };
                    };
                    Root.AddChild(createNode);
                    return;
                }

                foreach (var track in group.TrackList)
                    CreateTrack(group, track.TrackEffect);
            }

            public void ReloadTree(Material mat, AnimationNode group)
            {
                Root.Children.Clear();

                if (group.TrackList.Count == 0)
                {
                    var createNode = new TreeNode("Create Material Animation")
                    {
                        Icon = IconManager.ADD_ICON.ToString(),
                    };
                    createNode.OnDoubleClick += delegate
                    {
                        mat.CreateAnimation();
                        this.Anim.AnimGroups.Add(mat.AnimationData);

                        MapStudio.UI.UIManager.ActionExecBeforeUIDraw += delegate
                        {
                            ReloadTree(mat, mat.AnimationData);
                        };
                    };
                    Root.AddChild(createNode);
                    return;
                }

                CreateColorNode(group, "Material Color", TrackEffect.MaterialColorR);
                CreateColorNode(group, "Ambient Color", TrackEffect.AmbientColorR);
                CreateColorNode(group, "Shadow Color", TrackEffect.ShadowColorR);
                CreateTrack(group, "Lightmap Scale", TrackEffect.HiliteScale);
                CreateTrack(group, "Inverted Transparency", TrackEffect.Transparency);
                CreateTrack(group, "Reflection Intensity", TrackEffect.ReflectionIntensity);
                CreateTrack(group, "MatUnknown2", TrackEffect.MatUnknown2);
                CreateTrack(group, "MatUnknown3", TrackEffect.MatUnknown3);
                CreateTrack(group, "MatUnknown4", TrackEffect.MatUnknown4);
                CreateTrack(group, "MatUnknown5", TrackEffect.MatUnknown5);
            }

            private void CreateTrack(AnimationNode group, TrackEffect trackEffect)
            {
                CreateTrack(group, trackEffect.ToString(), trackEffect);
            }

            private void CreateTrack(AnimationNode group, string name, TrackEffect trackEffect)
            {
                var track = group.FindByEffect(trackEffect);

                TreeNode trackNode = new AnimationTree.TrackNode(this, track);
                trackNode.Header = name.ToString();
                Root.AddChild(trackNode);
            }

            private void CreateColorNode(AnimationNode group, string name, TrackEffect trackEffect)
            {
                ColorGroup ambientTrack = new ColorGroup();
                ambientTrack.R = group.FindByEffect(trackEffect);
                ambientTrack.G = group.FindByEffect(trackEffect + 1);
                ambientTrack.B = group.FindByEffect(trackEffect + 2);

                ambientTrack.Name = name;
                ambientTrack.R.Name = "R";
                ambientTrack.G.Name = "G";
                ambientTrack.B.Name = "B";

                var colorNodeUI = new AnimationTree.ColorGroupNode(this, ambientTrack, group);
                colorNodeUI.IsExpanded = true;
                Root.AddChild(colorNodeUI);

                //We want to add individual tracks for editing certain values
                colorNodeUI.AddChild(new AnimationTree.TrackNode(this, ambientTrack.R));
                colorNodeUI.AddChild(new AnimationTree.TrackNode(this, ambientTrack.G));
                colorNodeUI.AddChild(new AnimationTree.TrackNode(this, ambientTrack.B));
            }

            class ColorGroup : STAnimGroup
            {
                //A list of tracks per channel
                public STAnimationTrack R = new STAnimationTrack("R");
                public STAnimationTrack G = new STAnimationTrack("G");
                public STAnimationTrack B = new STAnimationTrack("B");

                //Get all the tracks used by this group
                public override List<STAnimationTrack> GetTracks()
                {
                    return new List<STAnimationTrack>() { R, G, B };
                }
            }

            public override void Reset()
            {
                foreach (HSF hsf in openedFiles)
                {
                    foreach (var obj in hsf.Header.ObjectNodes)
                    {
                        obj.ResetAnimation();
                        foreach (var mesh in obj.Meshes)
                            mesh.Material.ResetAnimation();
                    }
                }
            }

            public override void NextFrame()
            {
                foreach (var mat in HsfFile.Materials)
                {
                    AnimateMaterialGroup(mat, mat.AnimationData);
                    foreach (var att in mat.TextureAttributes)
                        AnimateAttributeGroup(mat, att, att.AnimationData);
                }

                foreach (AnimationNode node in Anim.AnimGroups)
                {
                    if (node.IsBone)
                        AnimateNodeGroup(node);
                }
            }

            private void AnimateNodeGroup(AnimationNode node)
            {
                foreach (HSF hsf in openedFiles)
                {
                    var obj = hsf.Header.ObjectNodes.FirstOrDefault(x => x.Name == node.Name);
                    if (obj == null) continue;

                    var objNode = hsf.Header.ObjectNodes.FirstOrDefault(x => x.Data == obj.Data);
                    if (objNode == null)
                        continue;

                    var index = hsf.Header.ObjectNodes.IndexOf(objNode);

                    var pos = objNode.Data.BaseTransform.Translate;
                    var rot = objNode.Data.BaseTransform.Rotate;
                    var sca = objNode.Data.BaseTransform.Scale;

                    float posX = pos.X; float posY = pos.Y; float posZ = pos.Z;
                    float rotX = rot.X; float rotY = rot.Y; float rotZ = rot.Z;
                    float scaX = sca.X; float scaY = sca.Y; float scaZ = sca.Z;

                    bool is_visible = true;

                    foreach (AnimTrack track in node.GetTracks())
                    {
                        float v = track.GetFrameValue(this.Frame);

                        switch (track.TrackEffect)
                        {
                            case TrackEffect.TranslateX: posX = v; break;
                            case TrackEffect.TranslateY: posY = v; break;
                            case TrackEffect.TranslateZ: posZ = v; break;
                            case TrackEffect.RotationX: rotX = v; break;
                            case TrackEffect.RotationY: rotY = v; break;
                            case TrackEffect.RotationZ: rotZ = v; break;
                            case TrackEffect.ScaleX: scaX = v; break;
                            case TrackEffect.ScaleY: scaY = v; break;
                            case TrackEffect.ScaleZ: scaZ = v; break;
                            case TrackEffect.Visible: is_visible = v != 0; break;
                        }
                    }

                    var translation = Matrix4.CreateTranslation(posX, posY, posZ);
                    var rotationX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotX));
                    var rotationY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotY));
                    var rotationZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotZ));
                    var scale = Matrix4.CreateScale(scaX, scaY, scaZ);
                    hsf.Header.ObjectNodes[index].AnimatedLocalMatrix = scale * (rotationX * rotationY * rotationZ) * translation;
                    hsf.Header.ObjectNodes[index].IsAnimated = true;

                    hsf.Header.ObjectNodes[index].IsVisible = is_visible;


                    // objNode.Data.CurrentTransform.Rotate = new Vector3XYZ(rotX, rotY, rotZ);
                    // objNode.Data.CurrentTransform.Translate = new Vector3XYZ(posX, posY, posZ);
                    // objNode.Data.CurrentTransform.Scale = new Vector3XYZ(scaX, scaY, scaZ);
                }
            }

            private void AnimateAttributeGroup(Material mat, TextureAttribute att, AnimationNode node)
            {
                if (node.TrackList.Count == 0)
                    return;

                int index = (int)node.ValueIndex;
                int id = mat.TextureAttributes.IndexOf(att);

                foreach (AnimTrack track in node.GetTracks())
                {
                    if (track.ConstantUnk == 0 && track.KeyFrames.Count <= 1)
                        continue;

                    float v = track.GetFrameValue(this.Frame);

                    switch ((AttributeTrackEffect)track.TrackEffect)
                    {
                        case AttributeTrackEffect.TranslateX:
                            mat.TextureMatrices[id].SetAnimation(GXTextureMatrix.Track.TransU, v);
                            break;
                        case AttributeTrackEffect.TranslateY:
                            mat.TextureMatrices[id].SetAnimation(GXTextureMatrix.Track.TransV, 1 - v);
                            break;
                        case AttributeTrackEffect.ScaleX:
                            mat.TextureMatrices[id].SetAnimation(GXTextureMatrix.Track.ScaleU, v);
                            break;
                        case AttributeTrackEffect.ScaleY:
                            mat.TextureMatrices[id].SetAnimation(GXTextureMatrix.Track.ScaleV, v);
                            break;
                        case AttributeTrackEffect.RotationX: break;
                        case AttributeTrackEffect.RotationY: break;
                        case AttributeTrackEffect.RotationZ:
                            mat.TextureMatrices[id].SetAnimation(GXTextureMatrix.Track.Rotate, v);
                            break;
                        case AttributeTrackEffect.TextureIndex: 
                            mat.Textures[id].TextureIndex = (int)v;
                            mat.Textures[id].Texture = HsfFile.Textures[(int)v].Name;
                            break;
                        case AttributeTrackEffect.CombinerBlending: 
                            break;
                    }
                }
                mat.ReloadTevStages();
            }

            private void AnimateMaterialGroup(Material material, AnimationNode node)
            {
                foreach (AnimTrack track in node.GetTracks())
                {
                    if (track.ConstantUnk == 0 && track.KeyFrames.Count < 1)
                        continue;

                    float v = track.GetFrameValue(this.Frame);

                    switch (track.TrackEffect)
                    {
                        case TrackEffect.AmbientColorR: material.AmbientColor[0].R = AsByte(v); break;
                        case TrackEffect.AmbientColorG: material.AmbientColor[0].G = AsByte(v); break;
                        case TrackEffect.AmbientColorB: material.AmbientColor[0].B = AsByte(v); break;
                        case TrackEffect.MaterialColorR: material.MaterialColor[0].R = AsByte(v); break;
                        case TrackEffect.MaterialColorG: material.MaterialColor[0].G = AsByte(v); break;
                        case TrackEffect.MaterialColorB: material.MaterialColor[0].B = AsByte(v); break;
                        case TrackEffect.Transparency: 
                            material.ReloadTransparency(this.Frame);
                            break;
                        case TrackEffect.HiliteScale:
                            material.UpdateLightmapMatrix();
                            break;
                    }
                }
            }

            private byte AsByte(float value)
            {
                return (byte)(Math.Clamp(value, 0.0f, 1.0f) * 255);
            }
        }
    }
}
