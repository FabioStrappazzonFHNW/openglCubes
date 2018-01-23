using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTK;                  //add "OpenTK" as NuGet reference
using OpenTK.Graphics.OpenGL4; //add "OpenTK" as NuGet reference
using System.Numerics;
using System.Drawing;
using System.Drawing.Imaging;

namespace OpenGL
{
    public class Transparent
    {
        public static void Run()
        {
            using (var w = new GameWindow(720, 480, null, "ComGr", GameWindowFlags.Default, DisplayDevice.Default, 4, 0, OpenTK.Graphics.GraphicsContextFlags.ForwardCompatible))
            {
                int hProgram = 0;
                int hTexture = 0; ;

                int vaoTriangle = 0;
                int[] triangleIndices = null;
                int vboTriangleIndices = 0;

                byte[] imageData;

                double time = 0;

                w.Load += (o, ea) =>
                {
                    //set up opengl
                    GL.ClearColor(0.5f, 0.5f, 0.5f, 0);
                    GL.Enable(EnableCap.DepthTest);
                    GL.DepthMask(true);
                    GL.DepthRange(0, 50);
                    GL.Disable(EnableCap.CullFace);
                    GL.Enable(EnableCap.FramebufferSrgb);
                    GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                    //load, compile and link shaders
                    //see https://www.khronos.org/opengl/wiki/Vertex_Shader
                    var VertexShaderSource = @"
#version 400 core

in vec3 pos;
in vec3 col;
in vec2 uvVertex;
in vec3 normalVertex;

uniform mat4 model;
uniform mat4 projection;

out vec3 vertexColor; 
out vec2 uvFragment;
out vec3 absolutePos;
out vec3 normalFragment;

void main()
{
    uvFragment = uvVertex;
    vertexColor = col;

    normalFragment = (model * vec4(normalVertex,0)).xyz;

    absolutePos = (model * vec4(pos,1)).xyz;

    gl_Position = projection * vec4(pos,1);
}
                        ";
                    var hVertexShader = GL.CreateShader(ShaderType.VertexShader);
                    GL.ShaderSource(hVertexShader, VertexShaderSource);
                    GL.CompileShader(hVertexShader);
                    GL.GetShader(hVertexShader, ShaderParameter.CompileStatus, out int status);
                    if (status != 1)
                        throw new Exception(GL.GetShaderInfoLog(hVertexShader));

                    //see https://www.khronos.org/opengl/wiki/Fragment_Shader
                    var FragmentShaderSource = @"
#version 400 core

uniform sampler2D tex;
uniform bool alpha;

in vec3 absolutePos;
in vec3 vertexColor;
in vec2 uvFragment;
in vec3 lightPos;
in vec3 normalFragment;

out vec4 color;

void main()
{
    vec4 texColor = texture(tex, uvFragment);
    vec4 col = vec4(vertexColor, 1) * texColor;

    vec3 lightPos = vec3(2, 3, 0);
    vec3 toLight = normalize(lightPos - absolutePos);
    float diffuse = max(0, dot(toLight, normalFragment));

    vec3 eye = vec3(0, 0, 0);
    vec3 viewDir = normalize(absolutePos - eye);
    vec3 specularDir = normalFragment * (2 * dot(normalFragment, toLight)) - toLight;
    specularDir = normalize(specularDir);
    vec3 specular = vec3(0.8) * pow(max(0.0, -dot(specularDir, viewDir)), 50);

    color = (vec4(0.1, 0.1, 0.1, 1)*col + diffuse * col + vec4(specular, alpha)) * vec4(1, 1, 1, alpha?0.2:1);

}
                        ";
                    var hFragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                    GL.ShaderSource(hFragmentShader, FragmentShaderSource);
                    GL.CompileShader(hFragmentShader);
                    GL.GetShader(hFragmentShader, ShaderParameter.CompileStatus, out status);
                    if (status != 1)
                        throw new Exception(GL.GetShaderInfoLog(hFragmentShader));

                    //link shaders to a program
                    hProgram = GL.CreateProgram();
                    GL.AttachShader(hProgram, hFragmentShader);
                    GL.AttachShader(hProgram, hVertexShader);
                    GL.LinkProgram(hProgram);
                    GL.GetProgram(hProgram, GetProgramParameterName.LinkStatus, out status);
                    if (status != 1)
                        throw new Exception(GL.GetProgramInfoLog(hProgram));

                    //upload model vertices to a vbo
                    var triangleVertices = new float[]
                    {   //top
                        -1, -1, -1,
                        +1, -1, -1,
                        +1, +1, -1,
                        -1, +1, -1,
                        //bottom
                        -1, -1, +1,
                        +1, -1, +1,
                        +1, +1, +1,
                        -1, +1, +1,
                        //left
                        -1, -1, -1,
                        -1, +1, -1,
                        -1, +1, +1,
                        -1, -1, +1,
                        //right
                        +1, +1, -1,
                        +1, -1, -1,
                        +1, -1, +1,
                        +1, +1, +1,
                        //front
                        -1, +1, -1,
                        +1, +1, -1,
                        +1, +1, +1,
                        -1, +1, +1,
                        //back
                        +1, -1, -1,
                        -1, -1, -1,
                        -1, -1, +1,
                        +1, -1, +1,
                    };
                    var vboTriangleVertices = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboTriangleVertices);
                    GL.BufferData(BufferTarget.ArrayBuffer, triangleVertices.Length * sizeof(float), triangleVertices, BufferUsageHint.StaticDraw);

                    triangleIndices = new int[]
                    {
                        0,   1,  2,
                        0,   2,  3,
                        7,   6,  5,
                        7,   5,  4,
                        8,   9, 10,
                        8,  10, 11,
                        12, 13, 14,
                        12, 14, 15,
                        16, 17, 18,
                        16, 18, 19,
                        20, 21, 22,
                        20, 22, 23,
                    };


                    // upload model indices to a vbo
                    vboTriangleIndices = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboTriangleIndices);
                    GL.BufferData(BufferTarget.ElementArrayBuffer, triangleIndices.Length * sizeof(int), triangleIndices, BufferUsageHint.StaticDraw);


                    var colors = new float[]
                    {
                        0,1,1,
                        1,0,1,
                        1,1,0,
                        0,0,1,

                        0,1,0,
                        1,0,0,
                        0,0,0,
                        1,1,1,

                        0,1,1,
                        0,0,1,
                        1,1,1,
                        0,1,1,

                        1,1,0,
                        1,0,1,
                        1,0,0,
                        0,0,0,

                        0,0,1,
                        1,1,0,
                        0,0,0,
                        1,1,1,

                        1,0,1,
                        0,1,1,
                        0,1,0,
                        1,0,0

                    };


                    var vboColor = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboColor);
                    GL.BufferData(BufferTarget.ArrayBuffer, colors.Length * sizeof(float), colors, BufferUsageHint.StaticDraw);


                    var uvs = new float[]
                    {
                        0,1,
                        1,1,
                        1,0,
                        0,0,

                        0,1,
                        1,1,
                        1,0,
                        0,0,

                        0,1,
                        1,1,
                        1,0,
                        0,0,

                        0,1,
                        1,1,
                        1,0,
                        0,0,

                        0,1,
                        1,1,
                        1,0,
                        0,0,

                        0,1,
                        1,1,
                        1,0,
                        0,0,
                    };


                    var vboUvs = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboUvs);
                    GL.BufferData(BufferTarget.ArrayBuffer, uvs.Length * sizeof(float), uvs, BufferUsageHint.StaticDraw);


                    var normals = new float[]
                    {
                        0,0,-1,
                        0,0,-1,
                        0,0,-1,
                        0,0,-1,

                        0,0,1,
                        0,0,1,
                        0,0,1,
                        0,0,1,

                        -1,0,0,
                        -1,0,0,
                        -1,0,0,
                        -1,0,0,

                        1,0,0,
                        1,0,0,
                        1,0,0,
                        1,0,0,

                        0,1,0,
                        0,1,0,
                        0,1,0,
                        0,1,0,

                        0,-1,0,
                        0,-1,0,
                        0,-1,0,
                        0,-1,0,
                    };

                    var vboNorms = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboNorms);
                    GL.BufferData(BufferTarget.ArrayBuffer, normals.Length * sizeof(float), normals, BufferUsageHint.StaticDraw);


                    //set up a vao
                    vaoTriangle = GL.GenVertexArray();
                    GL.BindVertexArray(vaoTriangle);

                    GL.EnableVertexAttribArray(GL.GetAttribLocation(hProgram, "pos"));
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboTriangleVertices);
                    GL.VertexAttribPointer(GL.GetAttribLocation(hProgram, "pos"), 3, VertexAttribPointerType.Float, false, 0, 0);


                    GL.EnableVertexAttribArray(GL.GetAttribLocation(hProgram, "col"));
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboColor);
                    GL.VertexAttribPointer(GL.GetAttribLocation(hProgram, "col"), 3, VertexAttribPointerType.Float, false, 0, 0);


                    GL.EnableVertexAttribArray(GL.GetAttribLocation(hProgram, "uvVertex"));
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboUvs);
                    GL.VertexAttribPointer(GL.GetAttribLocation(hProgram, "uvVertex"), 2, VertexAttribPointerType.Float, false, 0, 0);


                    GL.EnableVertexAttribArray(GL.GetAttribLocation(hProgram, "normalVertex"));
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboNorms);
                    GL.VertexAttribPointer(GL.GetAttribLocation(hProgram, "normalVertex"), 3, VertexAttribPointerType.Float, false, 0, 0);

                    var image = new Bitmap("./stones.bmp");
                    var imageW = image.Width;
                    var h = image.Height;
                    imageData = new byte[imageW * h * 3];
                    BitmapData data = image.LockBits(new Rectangle(0, 0, imageW, h), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    unsafe
                    {
                        byte* p = (byte*)data.Scan0;
                        for (int i = 0; i < imageW * h * 3; i += 3)
                        {
                            imageData[i] = *(p++);//r
                            imageData[i + 1] = *(p++);//g
                            imageData[i + 2] = *(p++);//b

                        }

                        image.UnlockBits(data);
                    }

                    GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
                    GL.GenTextures(1, out hTexture);
                    GL.BindTexture(TextureTarget.Texture2D, hTexture);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Srgb8, image.Width, image.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgb, PixelType.UnsignedByte, imageData);
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                    {
                        //check for errors during all previous calls
                        var error = GL.GetError();
                        if (error != ErrorCode.NoError)
                            throw new Exception(error.ToString());
                    }
                };

                w.UpdateFrame += (o, fea) =>
                {
                    //perform logic

                    time += fea.Time;
                };

                w.RenderFrame += (o, fea) =>
                {
                    //clear screen and z-buffer
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    //switch to our shader
                    GL.UseProgram(hProgram);

                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, hTexture);
                    GL.Uniform1(GL.GetUniformLocation(hProgram, "tex"), 0);

                    var scale = Matrix4.CreateScale(2f);
                    var rotate = Matrix4x4.CreateFromYawPitchRoll((float)time * 0.5f, (float)time, (float)time).ToGlMatrix();

                    var translate = Matrix4.CreateTranslation(-1f, -1f, -7f);
                    var perspective = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 2, 1, 1, 100);

                    var allTransforms = rotate * scale * translate;

                    GL.UniformMatrix4(GL.GetUniformLocation(hProgram, "model"), false, ref allTransforms);
                    allTransforms = allTransforms * perspective;
                    GL.UniformMatrix4(GL.GetUniformLocation(hProgram, "projection"), false, ref allTransforms);
                    
                    

                    //render our model
                    GL.Disable(EnableCap.Blend);
                    GL.Uniform1(GL.GetUniformLocation(hProgram, "alpha"), 0);
                    GL.BindVertexArray(vaoTriangle);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboTriangleIndices);
                    GL.DrawElements(PrimitiveType.Triangles, triangleIndices.Length, DrawElementsType.UnsignedInt, 0);


                    rotate = Matrix4x4.CreateFromYawPitchRoll((float)time, (float)time * 0.5f, (float)time).ToGlMatrix();

                    translate = Matrix4.CreateTranslation(1f, 1f, -7f);

                    allTransforms = rotate * scale * translate;
                    
                    GL.UniformMatrix4(GL.GetUniformLocation(hProgram, "model"), false, ref allTransforms);
                    allTransforms = allTransforms * perspective;
                    GL.UniformMatrix4(GL.GetUniformLocation(hProgram, "projection"), false, ref allTransforms);
                    

                    //render our model
                    GL.DepthMask(false);
                    GL.Enable(EnableCap.Blend);
                    GL.Uniform1(GL.GetUniformLocation(hProgram, "alpha"), 1);
                    GL.BindVertexArray(vaoTriangle);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboTriangleIndices);
                    GL.DrawElements(PrimitiveType.Triangles, triangleIndices.Length, DrawElementsType.UnsignedInt, 0);


                    GL.DepthMask(true);


                    //display
                    w.SwapBuffers();

                    var error = GL.GetError();
                    if (error != ErrorCode.NoError)
                        throw new Exception(error.ToString());
                };

                w.Resize += (o, ea) =>
                {
                    GL.Viewport(w.ClientRectangle);
                };

                w.Run();
            }
        }
    }
}
