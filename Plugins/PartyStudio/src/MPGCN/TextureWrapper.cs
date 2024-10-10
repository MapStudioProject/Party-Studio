using MPLibrary.GCN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using Toolbox.Core;
using MapStudio.UI;
using System.IO;
using Toolbox.Core.IO;

namespace PartyStudio.GCN
{
    public class TextureFolder : NodeBase
    {
        private HsfRender HsfRender;
        private HsfFile HsfFile;

        public override string Header => "Textures";

        public TextureFolder(HsfFile hsf, HsfRender render)
        {
            HsfFile = hsf;
            HsfRender = render;

            ContextMenus.Add(new MenuItemModel("Import", ImportTextures));
            ContextMenus.Add(new MenuItemModel("Export All", ExportTextures));
            ContextMenus.Add(new MenuItemModel(""));
            ContextMenus.Add(new MenuItemModel("Clear", Clear));

            foreach (var tex in hsf.Textures)
               AddChild(new TextureWrapper(tex));
        }

        private void ExportTextures()
        {
            ImguiFolderDialog dlg = new ImguiFolderDialog();
            if (dlg.ShowDialog())
            {
                foreach (TextureWrapper tex in this.Children)
                    tex.ExportTexture(Path.Combine(dlg.SelectedPath, $"{tex.Header}.png"));
            }
        }

        private void Clear()
        {
            int result = TinyFileDialog.MessageBoxInfoYesNo("Are you sure you want to clear all textures? This cannot be undone!");
            if (result != 1)
                return;

            var children = this.Children.ToList();
            foreach (TextureWrapper tex in children)
                RemoveTexture(tex);
        }

        private void ImportTextures()
        {
            //Dialog for importing textures. 
            ImguiFileDialog fileDialog = new ImguiFileDialog();
            fileDialog.MultiSelect = true;
            foreach (var ext in TextureDialog.SupportedExtensions)
                fileDialog.AddFilter(ext, ext);

            if (fileDialog.ShowDialog())
                ImportTexture(fileDialog.FilePaths);
        }

        public void ImportTexture(string filePath) => ImportTexture(new string[] { filePath });

        public void ImportTexture(string[] filePaths)
        {
            var dlg = new GCNTextureDialog();
            foreach (var filePath in filePaths)
                dlg.AddTexture(filePath);

            if (dlg.Textures.Count == 0)
                return;

            DialogHandler.Show(dlg.Name, 850, 350, dlg.Render, (o) =>
            {
                if (o != true)
                    return;

                ProcessLoading.Instance.IsLoading = true;
                foreach (var tex in dlg.Textures)
                {
                    var surfaces = tex.Surfaces;
                    AddTexture(tex.FilePath, tex);
                }
                ProcessLoading.Instance.IsLoading = false;
            });
        }

        public void ImportTextureDirect(string filePath)
        {
            var dlg = new GCNTextureDialog();
            dlg.AddTexture(filePath);

            dlg.Textures[0].EncodeTexture(0);

            var surfaces = dlg.Textures[0].Surfaces;
            AddTexture(dlg.Textures[0].FilePath, dlg.Textures[0]);
        }

        private void AddTexture(string filePath, GCNImportedTexture tex)
        {
            //Check for duped 
            var duped = this.Children.FirstOrDefault(x => x.Header == tex.Name);
            if (duped != null)
                ((TextureWrapper)duped).ImportTexture(tex);
            else
            {
                TextureWrapper ctex = new TextureWrapper();
                ctex.ImportTexture(tex);
                this.AddTexture(ctex.HSFTexture);
            }
        }

        public void RemoveTexture(TextureWrapper tex)
        {
            RemoveTexture(tex.HSFTexture);
            this.Children.Remove(tex);
        }

        private void AddTexture(HSFTexture tex)
        {
            HsfFile.Textures.Add(tex);
            HsfRender.AddTexture(tex);

            this.AddChild(new TextureWrapper(tex));
        }

        private void RemoveTexture(HSFTexture tex)
        {
            int index = HsfFile.Textures.IndexOf(tex);
            HsfFile.Textures.RemoveAt(index);
            HsfRender.TextureCache.RemoveAt(index);
        }
    }

    public class TextureWrapper : NodeBase
    {
        public HSFTexture HSFTexture;

        public TextureWrapper()
        {
            Header = "";

            ContextMenus.Add(new MenuItemModel("Export", ExportTextureDialog));
            ContextMenus.Add(new MenuItemModel("Replace", ReplaceTextureDialog));
            ContextMenus.Add(new MenuItemModel(""));
            ContextMenus.Add(new MenuItemModel("Rename", () => { ActivateRename = true; }));
            ContextMenus.Add(new MenuItemModel(""));
            ContextMenus.Add(new MenuItemModel("Delete", RemoveBatch));

            OnHeaderRenamed += delegate
            {
                Rename(this.Header);
            };
        }

        public TextureWrapper(HSFTexture texture) : this()
        {
            HSFTexture = texture;
            Tag = new EditableTexture(texture);
            Header = texture.Name;
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

        private void Rename(string name)
        {
            //Set bcres and h3d render instances
            this.HSFTexture.Name = name;
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
            tex.Format = HSFTexture.GcnFormat;

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
            if (HSFTexture == null)
            {
                HSFTexture = new HSFTexture();
                HSFTexture.Name = tex.Name;
            }

            var hsfFormat = HSFTexture.GetFormatId(tex.Format);
            byte bpp = (byte)(tex.Format == Decode_Gamecube.TextureFormats.C4 ? 4 : 8);
            HSFTexture.ImageData = tex.Surfaces[0].EncodedData;
            HSFTexture.SetPalette(tex.Surfaces[0].PaletteData);

            HSFTexture.GcnFormat = tex.Format;
            HSFTexture.GcnPaletteFormat = tex.PaletteFormat;

            HSFTexture.TextureInfo = new TextureInfo()
            {
                Width = (ushort)tex.Width,
                Height = (ushort)tex.Height,
                Format = (byte)hsfFormat,
                Bpp = bpp,
                MaxLOD = tex.MipCount,
                PaletteEntries = (ushort)(HSFTexture.PaletteData.Length),
                TextureTint = HSFTexture.TextureInfo.TextureTint,
            };


            if (HSFTexture.RenderTexture != null)
            {
                HSFTexture.RenderTexture.Reload(
                tex.Name, (uint)tex.Width, (uint)tex.Height, (uint)HSFTexture.GcnFormat, (uint)tex.PaletteFormat, 1,
                HSFTexture.ImageData, HSFTexture.PaletteData);
            }
            else
            {
                HSFTexture.RenderTexture = new GCNRenderLibrary.Rendering.GLGXTexture(
                   tex.Name, (uint)tex.Width, (uint)tex.Height, (uint)HSFTexture.GcnFormat, (uint)tex.PaletteFormat, 1,
                   HSFTexture.ImageData, HSFTexture.PaletteData);
            }

            Tag = new EditableTexture(HSFTexture);

            if (IconManager.HasIcon(this.Icon))
                IconManager.RemoveTextureIcon(this.Icon);
        }

        public void RemoveBatch()
        {
            var selected = this.Parent.Children.Where(x => x.IsSelected).ToList();

            string msg = $"Are you sure you want to delete the ({selected.Count}) selected textures? This cannot be undone!";
            if (selected.Count == 1)
                msg = $"Are you sure you want to delete {Header}? This cannot be undone!";

            int result = TinyFileDialog.MessageBoxInfoYesNo(msg);
            if (result != 1)
                return;

            var folder = ((TextureFolder)this.Parent);

            foreach (TextureWrapper tex in selected)
                folder.RemoveTexture(tex);
        }
    }

    class EditableTexture : STGenericTexture
    {
        private HSFTexture Texture;

        public EditableTexture(HSFTexture texture)
        {
            Texture = texture;
            RenderableTex = (IRenderableTexture)texture.RenderTexture;
            Width = texture.TextureInfo.Width;
            Height = texture.TextureInfo.Height;
            MipCount = texture.TextureInfo.MaxLOD;
            this.Platform = new Toolbox.Core.Imaging.GamecubeSwizzle()
            {
                Format = Texture.GcnFormat,
                PaletteFormat = texture.GcnPaletteFormat,
                PaletteData = texture.GetPalette(),
            };
            Parameters.DontSwapRG = true;
        }

        public override byte[] GetImageData(int ArrayLevel = 0, int MipLevel = 0, int DepthLevel = 0)
        {
            return Texture.ImageData.ToArray();
        }

        public override void SetImageData(List<byte[]> imageData, uint width, uint height, int arrayLevel = 0)
        {
            var swizzle = this.Platform as Toolbox.Core.Imaging.GamecubeSwizzle;
            this.Width = width;
            this.Height = height;
            byte bpp = (byte)(Texture.GcnFormat == Decode_Gamecube.TextureFormats.C4 ? 4 : 8);

            var encoded = Decode_Gamecube.EncodeData(imageData[0],
               swizzle.Format, swizzle.PaletteFormat, (int)Width, (int)Height);

            Texture.ImageData = encoded.Item1;
            Texture.SetPalette(encoded.Item2);

            var renderTex = new GCNRenderLibrary.Rendering.GLGXTexture(
                Texture.Name, (uint)this.Width, (uint)this.Height, (uint)swizzle.Format, 1, Texture.ImageData);

            if (Texture.RenderTexture != null)
                Texture.RenderTexture.Dispose();

            Texture.RenderTexture = renderTex;
        }
    }
}
