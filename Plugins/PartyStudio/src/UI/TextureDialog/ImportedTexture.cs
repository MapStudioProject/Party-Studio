using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;
using MapStudio.UI;
using SixLabors.ImageSharp.Processing;
using Toolbox.Core.IO;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using GCNRenderLibrary;

namespace PartyStudio.GCN
{
    /// <summary>
    /// Represents a texture that is used for importing and encoding H3D texture data.
    /// </summary>
    public class GCNImportedTexture
    {
        /// <summary>
        /// The texture format of the imported texture.
        /// </summary>
        public Decode_Gamecube.TextureFormats Format
        {
            get { return _format; }
            set
            {
                if (_format != value) {
                    _format = value;
                    UpdateMipsIfRequired();
                    ReloadEncodingSize();
                }
            }
        }

        public Decode_Gamecube.PaletteFormats PaletteFormat = Decode_Gamecube.PaletteFormats.RGB565;

        private Decode_Gamecube.TextureFormats _format;

        /// <summary>
        /// The file path of the imported texture.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The name of the texture.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The time it took to encode the image.
        /// </summary>
        public string EncodingTime = "";

        /// <summary>
        /// The width of the image.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// The height of the image.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// A readable string of the total size of the encoded image.
        /// </summary>
        public string EncodingSize { get; private set; }

        private uint _mipCount;

        /// <summary>
        /// The mip count of the image.
        /// </summary>
        public uint MipCount
        {
            get { return _mipCount; }
            set
            {
                value = Math.Max(value, 1);
                value = Math.Min(value, MaxMipCount);
                _mipCount = value;
                //re encode all surfaces to adjust the mip count
                foreach (var surface in Surfaces)
                    surface.Encoded = false;
                ReloadEncodingSize();
            }
        }

        private uint MaxMipCount = 6;

        /// <summary>
        /// 
        /// </summary>
        public int ActiveArrayIndex = 0;

        /// <summary>
        /// Determines if the texture has been encoded in the "Format" or not.
        /// </summary>
        public bool Encoded
        {
            get { return Surfaces[ActiveArrayIndex].Encoded; }
            set
            {
                Surfaces[ActiveArrayIndex].Encoded = value;
            }
        }

        //Cache the encoded data so it can be applied when dialog is finished.
        //This prevents re encoding again saving time.
        public List<Surface> Surfaces = new List<Surface>();

        public GCNImportedTexture(string fileName) {
            FilePath = fileName;
            Name = Path.GetFileNameWithoutExtension(fileName);
            Format = Decode_Gamecube.TextureFormats.CMPR;

            Surfaces.Add(new Surface(fileName));
            Reload(0);
        }

        public GCNImportedTexture(string name, byte[] rgbaData, uint width, uint height, uint mipCount, Decode_Gamecube.TextureFormats format)
        {
            Name = name;
            Format = format;
            Width = (int)width;
            Height = (int)height;
            MipCount = mipCount;
            Surfaces.Add(new Surface(rgbaData, Width, Height));
        }

        /// <summary>
        /// Adds a surface to the imported texture.
        /// </summary>
        public void AddSurface(string filePath)
        {
            var surface = new Surface(filePath);
            surface.Reload(Width, Height);
            Surfaces.Add(surface);
        }

        /// <summary>
        /// Removes a surface from the imported texture.
        /// </summary>
        public void RemoveSurface(int index)
        {
            var surface = Surfaces[index];
            surface.Dispose();
            Surfaces.RemoveAt(index);
        }

        /// <summary>
        /// Reloads the surface from its loaded file path.
        /// </summary>
        public void Reload(int index) {
            var surface = Surfaces[index];
            surface.Reload();
            Width = surface.ImageFile.Width;
            Height = surface.ImageFile.Height;

            MaxMipCount = 1 + CalculateMipCount();
            MipCount = MaxMipCount;
        }

        /// <summary>
        /// Updates the max mip count amount and clamps the mip count to the max if required.
        /// </summary>
        public void UpdateMipsIfRequired()
        {
            MaxMipCount = 1 + CalculateMipCount();
            if (MipCount > MaxMipCount)
                MipCount = MaxMipCount;
        }

        /// <summary>
        /// Reloads the encoding size into a readable string.
        /// </summary>
        private void ReloadEncodingSize() {
            EncodingSize = STMath.GetFileSize(CalculateEncodingSize());
        }

        /// <summary>
        /// Disposes all surfaces in the imported texture.
        /// </summary>
        public void Dispose()
        {
            foreach (var surface in Surfaces)
                surface?.Dispose();
        }

        /// <summary>
        /// Decodes the current surface.
        /// </summary>
        public byte[] DecodeTexture(int index)
        {
            //Get the encoded data and turn back into raw rgba data for previewing purposes
            var surface = Surfaces[index];
            var encoded = surface.EncodedData; //Only need first mip level
            var palette = surface.PaletteData;

            byte[] GetPalette(ushort[] paletteData)
            {
                if (paletteData.Length == 0) return new byte[0];

                var mem = new MemoryStream();
                using (var wr = new BinaryWriter(mem))
                {
                    for (int i = 0; i < paletteData.Length; i++)
                    {
                        wr.Write((byte)(paletteData[i] >> 8));
                        wr.Write((byte)(paletteData[i] & 0xFF));
                    }
                }
                return mem.ToArray();
            }

            byte[] Buffer = gctex.Decode(encoded, (uint)Width, (uint)Height,
                     (uint)Format, GetPalette(palette),
                     (uint)PaletteFormat);

            return Buffer;
        }

        /// <summary>
        /// Calculates the encoding size of the current image width, height, format and mip count
        /// </summary>
        /// <returns></returns>
        public int CalculateEncodingSize() {
            return Decode_Gamecube.GetDataSizeWithMips(Format, (uint)Width, (uint)Height, MipCount);
        }

        /// <summary>
        /// Encodes the current surface.
        /// </summary>
        public void EncodeTexture(int index)
        {
            var surface = Surfaces[index];

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            //palette limit
            if (this.Format == Decode_Gamecube.TextureFormats.C8)
            {
                //reload source image to re-encode
                Reload(0);
                //256 bit color
               /* surface.ImageFile.Mutate(x => x.Quantize(new OctreeQuantizer(new QuantizerOptions()
                {
                    MaxColors = 256, 
                })));*/
            }
            if (this.Format == Decode_Gamecube.TextureFormats.C4)
            {
                //reload source image to re-encode
                Reload(0);
                //16 bit color
                surface.ImageFile.Mutate(x => x.Quantize(new OctreeQuantizer(new QuantizerOptions()
                {
                    MaxColors = 16,
                })));
            }

            var rgbaData = surface.ImageFile.GetSourceInBytes();
            BitmapExtension.ConvertBgraToRgba(rgbaData);

            var encoded = Decode_Gamecube.EncodeData(rgbaData,
                Format, PaletteFormat, Width, Height);

            surface.EncodedData = encoded.Item1;
            surface.PaletteData = encoded.Item2;

            if (surface.PaletteData.Length > 0)
            {
               // surface.PaletteRGBA = Decode_Gamecube.GetPaletteColors(PaletteFormat, surface.PaletteData);
            }

            Encoded = true;

            stopWatch.Stop();

            TimeSpan ts = stopWatch.Elapsed;
            EncodingTime = string.Format("{0:00}ms", ts.Milliseconds);
        }

        /// <summary>
        /// Calculates the total possible mip count of the current width, height and format.
        /// </summary>
        /// <returns></returns>
        private uint CalculateMipCount() 
        {
            int MipmapNum = 0;
            int num = Math.Max(Height, Width);

            uint width = (uint)Width;
            uint height = (uint)Height;

            return (uint)0;
        }
        
        /// <summary>
        /// Attempts to detect what format might be best suited based on the image contents.
        /// </summary>
        public Decode_Gamecube.TextureFormats TryDetectFormat(Decode_Gamecube.TextureFormats defaultFormat)
        {
            return defaultFormat;
        }

        public class Surface
        {
            /// <summary>
            /// Determines if the texture has been encoded in the "Format" or not.
            /// </summary>
            public bool Encoded { get; set; }

            /// <summary>
            /// The encoded mip map data of the surface.
            /// </summary>
            public byte[] EncodedData;

            /// <summary>
            /// 
            /// </summary>
            public ushort[] PaletteData = new ushort[0];

            /// <summary>
            /// The original file path of the image imported.
            /// </summary>
            public string SourceFilePath { get; set; }

            public byte[] PaletteRGBA;

            /// <summary>
            /// The raw image file data.
            /// </summary>
            public Image<Rgba32> ImageFile;

            public Surface(string fileName) {
                SourceFilePath = fileName;
            }

            public Surface(byte[] rgba, int width, int height) {
                ImageFile = Image.LoadPixelData<Rgba32>(rgba, width, height);
            }

            public void Reload() {
                if (ImageFile != null)
                    ImageFile.Dispose();

                if (SourceFilePath.EndsWith(".tiff") || SourceFilePath.EndsWith(".tif"))
                {
                    var bitmap = new System.Drawing.Bitmap(SourceFilePath);
                    var rgba = BitmapExtension.ImageToByte(bitmap);
                    ImageFile = Image.LoadPixelData<Rgba32>(rgba, bitmap.Width, bitmap.Height);
                    bitmap.Dispose();
                }
                else
                    ImageFile = Image.Load<Rgba32>(SourceFilePath);

                //Update width/height based on 3DS limitations
                int width = Math.Min(ImageFile.Width, 1024);
                int height = Math.Min(ImageFile.Height, 1024);

                if (ImageFile.Width != width || ImageFile.Height != height)
                    ImageSharpTextureHelper.Resize(ImageFile, width, height);
            }

            public void Reload(int width, int height) {
                ImageFile = Image.Load<Rgba32>(SourceFilePath);
                if (ImageFile.Width != width || ImageFile.Height != height) 
                    ImageSharpTextureHelper.Resize(ImageFile, width, height);
            }

            public List<byte[]> GenerateMipMaps(uint mipCount)
            {
                var mipmaps = ImageSharpTextureHelper.GenerateMipmaps(ImageFile, mipCount);

                List<byte[]> output = new List<byte[]>();
                for (int i = 0; i < mipCount; i++)
                {
                    output.Add(mipmaps[i].GetSourceInBytes());
                    //Dispose base images after if not the base image
                    if (i != 0)
                        mipmaps[i].Dispose();
                }
                return output;
            }

            public byte[] GetPaletteBytes()
            {
                var mem = new MemoryStream();
                using (var writer = new FileWriter(mem))
                {
                    writer.SetByteOrder(true);
                    writer.Write(this.PaletteData);
                }
                return mem.ToArray();
            }

            public void Dispose() {
                ImageFile?.Dispose();
            }
        }
    }
}
