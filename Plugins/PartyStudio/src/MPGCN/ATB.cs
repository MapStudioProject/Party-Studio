using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using MapStudio.UI;
using Toolbox.Core.ViewModels;
using UIFramework;
using PartyStudio.GCN;
using System.IO;

namespace MPLibrary.GCN
{
    public class ATB : FileEditor, IFileFormat, IDisposable
    {
        public bool CanSave { get; set; } = true;

        public string[] Description { get; set; } = new string[] { "Mario Party GCN Sprite" };
        public string[] Extension { get; set; } = new string[] { "*.anm" };

        public File_Info FileInfo { get; set; }

        public bool Identify(File_Info fileInfo, System.IO.Stream stream)
        {
            return Utils.GetExtension(fileInfo.FileName) == ".anm";
        }

        AtbFile AtbFile;

        public void Load(System.IO.Stream stream) {
            AtbFile = new AtbFile(stream);

            int id = 0;
            foreach (var tex in AtbFile.Textures)
            {
                var t = new EditableTexture(tex);
                var n = new TextureWrapper(tex)
                {
                    Header = $"Texture{id++}",
                    Tag = t,
                };
                n.Icon = n.ID;
                Root.AddChild(n);
            }
            foreach (var pat in AtbFile.Patterns)
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(pat, Newtonsoft.Json.Formatting.Indented);

                var n = new NodeBase($"Animation Pattern");
                n.TagUI.UIDrawer += delegate
                {
                    ImGuiNET.ImGui.TextWrapped(json);
                };
                Root.AddChild(n);
            }
            foreach (var bank in AtbFile.Banks)
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(bank, Newtonsoft.Json.Formatting.Indented);

                var n = new NodeBase($"Animation Bank");
                n.TagUI.UIDrawer += delegate
                {
                    ImGuiNET.ImGui.TextWrapped(json);
                };
                Root.AddChild(n);
            }
        }

        public void Save(System.IO.Stream stream) {
            AtbFile.Save(stream);
        }

        public void Dispose()
        {
            foreach (var node in Root.Children)
            {
                var tex = node.Tag as STGenericTexture;
                if (tex != null)
                    tex.RenderableTex?.Dispose();
            }
        }

        /// <summary>
        /// Prepares the dock layouts to be used for the file format.
        /// </summary>
        public override List<DockWindow> PrepareDocks()
        {
            List<DockWindow> windows = new List<DockWindow>();
            windows.Add(Workspace.Outliner);
            windows.Add(Workspace.PropertyWindow);
           // windows.Add(Workspace.ConsoleWindow);
            //windows.Add(Workspace.ViewportWindow);
            return windows;
        }


        public class TextureWrapper : NodeBase
        {
            public AtbTextureInfo AtbTexture;

            public TextureWrapper()
            {
                Header = "";

                ContextMenus.Add(new MenuItemModel("Export", ExportTextureDialog));
                ContextMenus.Add(new MenuItemModel("Replace", ReplaceTextureDialog));
            }

            public TextureWrapper(AtbTextureInfo texture) : this()
            {
                AtbTexture = texture;
                Tag = new EditableTexture(texture);
            }

            public void ExportTexture(string filePath)
            {
                var tex = this.Tag as STGenericTexture;
                tex.Export(filePath, new TextureExportSettings());
            }

            private void ExportTextureDialog()
            {
                //Multi select export
                var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();
                if (selected.Count > 1)
                {
                    ImguiFolderDialog dlg = new ImguiFolderDialog();
                    //Todo configurable formats for folder dialog
                    if (dlg.ShowDialog())
                    {
                        foreach (var sel in selected)
                            ExportTexture(Path.Combine(dlg.SelectedPath, $"{sel.Header}.png"));
                    }
                }
                else
                {
                    ImguiFileDialog dlg = new ImguiFileDialog();
                    dlg.SaveDialog = true;
                    dlg.FileName = $"{this.Header}.png";
                    foreach (var ext in TextureDialog.SupportedExtensions)
                        dlg.AddFilter(ext, ext);

                    if (dlg.ShowDialog())
                        ExportTexture(dlg.FilePath);
                }
            }

            private void ReplaceTextureDialog()
            {
                ImguiFileDialog dlg = new ImguiFileDialog();
                dlg.SaveDialog = false;
                foreach (var ext in TextureDialog.SupportedExtensions)
                    dlg.AddFilter(ext, ext);

                if (dlg.ShowDialog())
                    ReplaceTexture(dlg.FilePath);
            }

            public void ReplaceTexture(string filePath)
            {
                var dlg = new GCNTextureDialog();
                var tex = dlg.AddTexture(filePath);
                tex.Format = EditableTexture.FormatList[AtbTexture.Format];
                tex.PaletteFormat = Decode_Gamecube.PaletteFormats.RGB5A3;

                DialogHandler.Show(dlg.Name, 850, 350, dlg.Render, (o) =>
                {
                    if (o != true)
                        return;

                    dlg.Textures[0].Name = this.Header;
                    ImportTexture(dlg.Textures[0]);
                });
            }

            public void ImportTexture(GCNImportedTexture tex)
            {
                if (AtbTexture == null)
                {
                    AtbTexture = new AtbTextureInfo();
                }

                var atbTexFormat = EditableTexture.GetFormatId(tex.Format);

                byte bpp = (byte)(tex.Format == Decode_Gamecube.TextureFormats.C4 ? 4 : 8);
                AtbTexture.ImageData = tex.Surfaces[0].EncodedData;
                AtbTexture.PaletteData = tex.Surfaces[0].GetPaletteBytes();
                AtbTexture.Format = (byte)atbTexFormat;
                AtbTexture.Width = (ushort)tex.Width;
                AtbTexture.Height = (ushort)tex.Height;
                AtbTexture.Bpp = bpp;

                Tag = new EditableTexture(AtbTexture);

                if (IconManager.HasIcon(this.Icon))
                    IconManager.RemoveTextureIcon(this.Icon);
            }
        }

        class EditableTexture : STGenericTexture
        {
            private AtbTextureInfo Texture;

            public EditableTexture(AtbTextureInfo texture)
            {
                Texture = texture;

                Width = texture.Width;
                Height = texture.Height;
                MipCount = 1;
                var gcFormat = FormatList[texture.Format & 0xF];
                var gcnPalette = Decode_Gamecube.PaletteFormats.RGB5A3;
                this.Platform = new Toolbox.Core.Imaging.GamecubeSwizzle()
                {
                    PaletteData = GetPalette(),
                    Format = gcFormat,
                    PaletteFormat = gcnPalette,
                };
                Parameters.DontSwapRG = true;
            }

            public override byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0)
            {
                return Texture.ImageData.ToArray();
            }

            public override void SetImageData(List<byte[]> imageData, uint width, uint height, int arrayLevel = 0)
            {
                throw new NotImplementedException();
            }

            public static int GetFormatId(Decode_Gamecube.TextureFormats gcformat)
            {
                return FormatList.FirstOrDefault(x => x.Value == gcformat).Key;
            }

            public static int GetPaletteFormatId(Decode_Gamecube.PaletteFormats gcformat)
            {
                return PaletteFormatList.FirstOrDefault(x => x.Value == gcformat).Key;
            }

            public static Dictionary<int, Decode_Gamecube.TextureFormats> FormatList = new Dictionary<int, Decode_Gamecube.TextureFormats>()
            {
            { 0x00, Decode_Gamecube.TextureFormats.RGBA32 },
            { 0x01, Decode_Gamecube.TextureFormats.RGB5A3 },
            { 0x02, Decode_Gamecube.TextureFormats.RGB5A3 },
            { 0x03, Decode_Gamecube.TextureFormats.C8 },
            { 0x04, Decode_Gamecube.TextureFormats.C4 },
            { 0x05, Decode_Gamecube.TextureFormats.IA8 },
            { 0x06, Decode_Gamecube.TextureFormats.IA4 },
            { 0x07, Decode_Gamecube.TextureFormats.I8 },
            { 0x08, Decode_Gamecube.TextureFormats.I4 },
            { 0x09, Decode_Gamecube.TextureFormats.IA8 },
            { 0x0A, Decode_Gamecube.TextureFormats.CMPR },
           };

            private static Dictionary<int, Decode_Gamecube.PaletteFormats> PaletteFormatList = new Dictionary<int, Decode_Gamecube.PaletteFormats>()
            {
                { 0x09, Decode_Gamecube.PaletteFormats.RGB565 }, //C4 if BPP == 4
               { 0x0A, Decode_Gamecube.PaletteFormats.RGB5A3 }, //C4 if BPP == 4
               { 0x0B, Decode_Gamecube.PaletteFormats.IA8 }, //C4 if BPP == 4
            };

            /// <summary>
            /// Gets the palette data in ushorts.
            /// </summary>
            public ushort[] GetPalette()
            {
                if (Texture.PaletteData == null || Texture.PaletteData.Length == 0) return new ushort[0];

                ushort[] palettes = new ushort[Texture.PaletteData.Length / 2];
                using (var reader = new Toolbox.Core.IO.FileReader(new System.IO.MemoryStream(Texture.PaletteData.ToArray())))
                {
                    reader.SetByteOrder(true);
                    for (int i = 0; i < palettes.Length; i++)
                        palettes[i] = reader.ReadUInt16();
                }
                return palettes;
            }
        }
    }
}
