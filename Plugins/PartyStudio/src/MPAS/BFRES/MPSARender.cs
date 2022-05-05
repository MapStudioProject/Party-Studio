using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Toolbox.Core;
using GLFrameworkEngine;
using System.IO;
using CafeLibrary.Rendering;
using BfresLibrary;

namespace PartyStudio
{
    public class MPSARender : BfshaRenderer
    {
        public MPSARender(BfresRender render, BfresModelRender model) : base(render, model)
        {

        }

        public override void LoadMesh(BfresMeshRender mesh)
        {
          
        }

        public override void ReloadRenderState(Material mat, BfresMeshRender meshRender)
        {
            var type = BfresMatGLConverter.GetRenderInfo(mat, "state_type");
            if (type == 1)
            {
                Material.BlendState.BlendColor = true;
                meshRender.Pass = Pass.TRANSPARENT;
            }
        }

        public override void ReloadProgram(BfresMeshRender mesh)
        {
            ProgramPasses.Clear();

            var matRender = mesh.MaterialAsset as BfresMaterialRender;

            //Find index via option choices
            Dictionary<string, string> options = new Dictionary<string, string>();
            foreach (var op in matRender.Material.ShaderOptions)
                options.Add(op.Key, op.Value);

            foreach (var op in ShaderModel.DynamiOptions.Values)
                options.Add(op.Name, op.defaultChoice);

            options["bezel_skinning_count_option"] = mesh.VertexSkinCount.ToString();

            int programIndex = ShaderModel.GetProgramIndex(options);
            if (programIndex == -1)
            {
                StudioLogger.WriteError($"Failed to find shader program {mesh.Name}!");
                return;
            }

            this.ProgramPasses.Add(ShaderModel.GetShaderProgram(programIndex));
        }

        static GLTextureCube DiffuseCubemapTexture;
        static GLTextureCube SpecularCubemapTexture;
        static GLTextureCube EmptyCubemapTexture;

        static GLTexture2D WhiteTexture;
        static GLTexture2D SpecularMaskTexture;
        static GLTexture2D CurvatureTexture;
        static GLTexture2DArray ShadowTexture;

        static void InitTextures()
        {
            //Cube maps
            DiffuseCubemapTexture = GLTextureCube.FromDDS(new DDS($"Resources\\hsbd02_00_cha_irr.dds"));

            DiffuseCubemapTexture.Bind();
            DiffuseCubemapTexture.MagFilter = TextureMagFilter.Linear;
            DiffuseCubemapTexture.MinFilter = TextureMinFilter.Linear;
            DiffuseCubemapTexture.UpdateParameters();

            GL.TexParameter(DiffuseCubemapTexture.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(DiffuseCubemapTexture.Target, TextureParameterName.TextureMaxLevel, 0);
            DiffuseCubemapTexture.Unbind();

            SpecularCubemapTexture = GLTextureCube.FromDDS(new DDS($"Resources\\radiance.dds"));

            SpecularCubemapTexture.Bind();
            SpecularCubemapTexture.MagFilter = TextureMagFilter.Linear;
            SpecularCubemapTexture.MinFilter = TextureMinFilter.LinearMipmapLinear;
            SpecularCubemapTexture.UpdateParameters();

            GL.TexParameter(SpecularCubemapTexture.Target, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(SpecularCubemapTexture.Target, TextureParameterName.TextureMaxLevel, 7);
            SpecularCubemapTexture.Unbind();

            var whiteTexture = GLTexture2D.CreateWhiteTexture(1, 1);

            SpecularMaskTexture = whiteTexture;
            CurvatureTexture = whiteTexture;
            ShadowTexture = GLTexture2DArray.CreateConstantColorTexture(1, 1, 1, 255, 255, 255, 255);
            WhiteTexture = whiteTexture;
            EmptyCubemapTexture = GLTextureCube.CreateEmptyCubemap(4);
        }

        #region debug

        private void GeneratePrefilterMap(GLTextureCube input)
        {
            //Generate a prefilter map for using in the tool
            var ouput = GLTextureCube.CreateEmptyCubemap(128, PixelInternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte, 7);
            AGraphicsLibrary.CubemapPrefilterRT.CreateCubemap(GLContext.ActiveContext, input, ouput, 0);
            ouput.SaveDDS($"Resources\\radiance.dds");
        }

        #endregion

        public override void Render(GLContext control, BfresModelRender modelRender, GLTransform transform, ShaderProgram shader, GenericPickableMesh mesh)
        {
            if (DiffuseCubemapTexture == null)
                InitTextures();

            base.Render(control, modelRender, transform, shader, mesh);
        }

        public override void LoadUniformBlock(GLContext control, ShaderProgram shader, int index,
      BfresModelRender parentModel, GLTransform transform, UniformBlock block, string name, GenericPickableMesh mesh)
        {
            var bfresMaterial = (BfresMaterialRender)mesh.MaterialAsset;
            var bfresMesh = (BfresMeshRender)mesh;
            var meshBone = parentModel.ModelData.Skeleton.Bones[bfresMesh.BoneIndex];
            var bfshaBlock = ShaderModel.UniformBlocks[index];

            switch (name)
            {
                case "Shape":
                    SetShapeBlock(bfresMesh, meshBone.Transform, block);
                    break;
                case "Skeleton":
                    SetBoneMatrixBlock(parentModel.ModelData.Skeleton, transform.TransformMatrix, bfresMesh.VertexSkinCount > 1, block, bfshaBlock.Size / 48);
                    break;
                case "View":
                    SetViewportUniforms(control.Camera, block);
                    break;
                case "Material":
                    SetMaterialBlock(block);
                    break;
                case "EnvironmentParamBuffer":
                    SetEnvUniforms(block);
                    break;
                case "ModelParamBuffer":
                    SetModelBlock(block);
                    break;
                case "ShadowView":
                    break;
                case "RenderTarget":
                    SetRenderTargetBlock(block);
                    break;
            }
        }

        public void SetRenderTargetBlock(UniformBlock block)
        {
            block.Buffer.Clear();
            block.Add(new Vector4(1152.00f, 648.00f, 0.00087f, 0.00154f));
            block.Add(new Vector4(1152.00f, 648.00f, 0.00087f, 0.00155f));
            block.Add(new Vector4(1.00893E-43f, 5.74532E-44f, 4.13663E-42f, 0.00f));
        }

        public void SetModelBlock(UniformBlock block)
        {
            Matrix4 transform = Matrix4.Identity;

            block.Buffer.Clear();

            //Fill the buffer by program offsets
            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.SeekBegin(0);
                writer.Write(Properties.Resources.MPASModel);

              //  writer.SeekBegin(18 * 16);
               // writer.Write(new Vector4(0, 0, 1, 1));
            }

            block.Buffer.Clear();
            block.Buffer.AddRange(mem.ToArray());
        }

        private void SetEnvUniforms(UniformBlock block)
        {
            block.Buffer.Clear();

            //Fill the buffer by program offsets
            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.SeekBegin(0);
                writer.Write(Properties.Resources.MPSAEnv);
                writer.SeekBegin(0);

               /* writer.Write(new Vector4(-4, 4, 0, 0));
                writer.Write(new Vector4(0.89f, 0.89f, 0.61338f, 0.00f));
                writer.Write(new Vector4(-0.01745f, 0.86603f, 0.4997f, 0));
                writer.Write(new Vector4(0, 11.532f, 23.831f, 0));

                writer.SeekBegin(33 * 16);
                writer.Write(new Vector4(0.18f, 0.83f, 1.00f, 0));
                //Near and far fog
                writer.Write(new Vector4(0.01351f, 10000, 1.00f, 1.00f));*/
            }

            block.Buffer.Clear();
            block.Buffer.AddRange(mem.ToArray());
        }

        public override void SetShapeBlock(BfresMeshRender mesh, Matrix4 transform, UniformBlock block)
        {
            int numSkinning = (int)mesh.VertexSkinCount;

            block.Buffer.Clear();
            block.Add(transform.Column0);
            block.Add(transform.Column1);
            block.Add(transform.Column2);
            block.AddInt(numSkinning);
        }

        public override void LoadBindlessTextures(GLContext control, ShaderProgram shader, ref int id)
        {
            base.LoadBindlessTextures(control, shader, ref id);

            shader.SetTexture(ShadowTexture, "fp_tex_cb8_110", id++);
            shader.SetTexture(ShadowTexture, "fp_tex_cb8_112", id++);

            shader.SetTexture(SpecularCubemapTexture, "fp_tex_cb8_132", id++);

            shader.SetTexture(EmptyCubemapTexture, "fp_tex_cb9_78", id++);
            shader.SetTexture(EmptyCubemapTexture, "fp_tex_cb9_7A", id++);
            shader.SetTexture(EmptyCubemapTexture, "fp_tex_cb9_7C", id++);
            shader.SetTexture(EmptyCubemapTexture, "fp_tex_cb9_7E", id++);
            shader.SetTexture(SpecularCubemapTexture, "fp_tex_cb9_8E", id++);
            shader.SetTexture(DiffuseCubemapTexture, "fp_tex_cb9_90", id++);
        }

        private void SetViewportUniforms(Camera camera, UniformBlock block)
        {
            Matrix4 mdlMat = ParentRenderer.Transform.TransformMatrix;
            var viewMatrix = mdlMat * camera.ViewMatrix;
            var projMatrix = camera.ProjectionMatrix;
            var viewInverted = viewMatrix.Inverted();
            var viewProjMatrix = viewMatrix * projMatrix;
            Vector4[] cView = new Vector4[3]
            {
                viewMatrix.Column0,
                viewMatrix.Column1,
                viewMatrix.Column2,
            };
            Vector4[] cViewProj = new Vector4[4]
            {
                viewProjMatrix.Column0,
                viewProjMatrix.Column1,
                viewProjMatrix.Column2,
                viewProjMatrix.Column3,
            };
            Vector4[] cProj = new Vector4[4]
            {
                projMatrix.Column0,
                projMatrix.Column1,
                projMatrix.Column2,
                projMatrix.Column3,
            };
            Vector4[] cViewInv = new Vector4[3]
             {
                viewInverted.Column0,
                viewInverted.Column1,
                viewInverted.Column2,
             };

            //Fill the buffer by program offsets
            var mem = new System.IO.MemoryStream();
            using (var writer = new Toolbox.Core.IO.FileWriter(mem))
            {
                writer.SeekBegin(0);
                writer.Write(cProj[0]);
                writer.Write(cProj[1]);
                writer.Write(cProj[2]);
                writer.Write(cProj[3]);

                writer.Write(cView[0]);
                writer.Write(cView[1]);
                writer.Write(cView[2]);

                writer.Write(cViewProj[0]);
                writer.Write(cViewProj[1]);
                writer.Write(cViewProj[2]);
                writer.Write(cViewProj[3]);

                writer.Write(cViewInv[0]);
                writer.Write(cViewInv[1]);
                writer.Write(cViewInv[2]);

                //   writer.SeekBegin(22 * 16);
                //   writer.Write(new Vector4(1));

                writer.SeekBegin(26 * 16);
                writer.Write(camera.InverseRotationMatrix.Row2);
               // writer.Write(new Vector4(0, -0.62f, -0.76f, 0));
            }

            block.Buffer.Clear();
            block.Buffer.AddRange(mem.ToArray());
        }

        public override GLTexture GetExternalTexture(GLContext control, string sampler)
        {
            switch (sampler)
            {
                case "radianceIbl": return DiffuseCubemapTexture;
                case "irradianceIbl": return SpecularCubemapTexture;
                case "utilitySampler0": return WhiteTexture;
                case "utilitySampler1": return WhiteTexture;
                case "utilitySampler2": return WhiteTexture;
                case "utilitySampler3": return WhiteTexture;
                case "utilitySampler4": return ShadowTexture;
                case "utilitySamplerCube0": return SpecularCubemapTexture;
            }
            return base.GetExternalTexture(control, sampler);
        }
    }
}
