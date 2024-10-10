using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace PartyStudio.GCN
{
    /// <summary>
    /// The texture map displayed in the UV viewer.
    /// </summary>
    public class TextureSamplerMap
    {
        /// <summary>
        /// The ID of the texture map to render as.
        /// </summary>
        public int ID = -1;

        /// <summary>
        /// The wrap mode to configure how the texture wraps outside the UV border on the horizontal axis.
        /// </summary>
        public TextureWrapMode WrapU = TextureWrapMode.Repeat;

        /// <summary>
        /// The wrap mode to configure how the texture wraps outside the UV border on the vertical axis.
        /// </summary>
        public TextureWrapMode WrapV = TextureWrapMode.Repeat;

        /// <summary>
        /// The mag filter.
        /// </summary>
        public TextureMagFilter MagFilter = TextureMagFilter.Linear;

        /// <summary>
        /// The min filter.
        /// </summary>
        public TextureMinFilter MinFilter = TextureMinFilter.Linear;

        /// <summary>
        /// The texture width used to configure texture aspect ratio.
        /// </summary>
        public int Width;

        /// <summary>
        /// The texture height used to configure texture aspect ratio.
        /// </summary>
        public int Height;

        public float LODBias;
        public int MinLOD;

        public Matrix4 Transform = Matrix4.Identity;
    }
}
