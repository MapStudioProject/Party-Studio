using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLFrameworkEngine
{
    public class CollisionFloorVisual : RenderMesh<VertexPositionTexCoord>, IDrawable
    {
        /// <summary>
        /// Toggles to display a solid floor or not.
        /// </summary>
        public static bool Display = true;

        public static float Height = 0f;

        /// <summary>
        /// The texture to display on the solid floor.
        /// </summary>
        public GLTexture Texture = null;

        private StandardMaterial Material = new StandardMaterial();

        //scale of the floor
        private const float SCALE = 200;

        public CollisionFloorVisual() : base(Vertices, PrimitiveType.TriangleStrip)
        {
        }

        static VertexPositionTexCoord[] Vertices => new VertexPositionTexCoord[]
        {
               new VertexPositionTexCoord(new Vector3(1.0f, Height, 1.0f) * SCALE, new Vector2(0, 1)),
               new VertexPositionTexCoord(new Vector3(1.0f, Height,-1.0f) * SCALE, new Vector2(0, 0)),
               new VertexPositionTexCoord(new Vector3(-1.0f,Height, 1.0f) * SCALE, new Vector2(1, 1)),
               new VertexPositionTexCoord(new Vector3(-1.0f,Height,-1.0f) * SCALE, new Vector2(1, 0)),
        };

        public bool IsVisible { get; set; } = true;

        public void SetImage(string filePath)
        {
            if (System.IO.File.Exists(filePath))
                Texture = GLTexture2D.FromBitmap(filePath);
        }

        public void Update()
        {
            this.UpdateVertexData(Vertices);
        }

        public void DrawModel(GLContext control, Pass pass)
        {
            if (pass != Pass.OPAQUE || !Display)
                return;

            if (Texture != null)
                Material.DiffuseTextureID = Texture.ID;

            Material.Render(control);
            this.Draw(control);

            GL.UseProgram(0);
        }
    }
}