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
    public class HeightMap
    {
        public static void Run()
        {
            using (var w = new GameWindow(720, 480, null, "ComGr", GameWindowFlags.Default, DisplayDevice.Default, 4, 0, OpenTK.Graphics.GraphicsContextFlags.ForwardCompatible))
            {
                int hProgram = 0;
                int hTexture = 0;
                int hHeight = 0;

                int vaoTriangle = 0;
                int[] triangleIndices = null;
                int vboTriangleIndices = 0;

                byte[] imageData;
                byte[] heightData;

                double time = 0;
                int gridSize = 100;

                w.Load += (o, ea) =>
                {
                    //set up opengl
                    GL.ClearColor(0.5f, 0.5f, 0.5f, 0);
                    GL.ClearDepth(1);
                    GL.Enable(EnableCap.DepthTest);
                    GL.DepthFunc(DepthFunction.Less);
                    //GL.Enable(EnableCap.CullFace);

                    //load, compile and link shaders
                    //see https://www.khronos.org/opengl/wiki/Vertex_Shader
                    var VertexShaderSource = @"
#version 400 core

in vec3 pos;
in vec2 uvVertex;

uniform mat4 model;
uniform mat4 projection;
uniform sampler2D height;

out vec2 uvFragment;
out vec3 absolutePos;
out vec3 normalFragment;

float getHeight(in vec2 pos)
{
    float h = texture(height, pos).x;
    return log(h*30+1) * 8;
}

vec3 getNormal(in vec2 pos)
{
    float offset = 1.0/float(textureSize(height,0).x);
    vec2 xOff = vec2(offset, 0);
    vec2 zOff = vec2(0, offset);

    vec3 x = vec3(offset*2, getHeight(pos+xOff) - getHeight(pos-xOff), 0);
    vec3 z = vec3(0,        getHeight(pos+zOff) - getHeight(pos-zOff), offset*2);

    return normalize(cross(z, x));
}

void main()
{
    uvFragment = uvVertex;

    absolutePos = (model * vec4(pos, 1)).xyz;
    
    vec3 n = getNormal(uvVertex);
    normalFragment = normalize((model * vec4(n, 0)).xyz);

    vec3 p = pos;
    p.y = getHeight(uvVertex);

    gl_Position = projection * vec4(p,1);
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

in vec3 absolutePos;
in vec2 uvFragment;
in vec3 lightPos;
in vec3 normalFragment;

out vec4 color;

void main()
{
    vec4 col = texture(tex, uvFragment);

    vec3 lightPos = vec3(2, 7, 0);
    vec3 toLight = normalize(lightPos - absolutePos);
    float diffuse = max(0, dot(toLight, normalFragment));

    vec3 eye = vec3(0, 0, 0);
    vec3 viewDir = normalize(absolutePos - eye);
    vec3 specularDir = normalFragment * (2 * dot(normalFragment, toLight)) - toLight;
    specularDir = normalize(specularDir);
    vec3 specular = vec3(0.8) * pow(max(0.0, -dot(specularDir, viewDir)), 5);

    color = vec4(0.1)*col + diffuse * col + vec4(specular, 1);

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


                    
                    var triangleVertices = new float[(gridSize + 1) * (gridSize + 1) * 3];
                    var uvs = new float[(gridSize + 1) * (gridSize + 1) * 2];
                    triangleIndices = new int[gridSize * gridSize * 2 * 3];

                    for (int i = 0; i < gridSize + 1; i++)
                    {
                        for (int j = 0; j < gridSize +1; j++)
                        {
                            triangleVertices[((gridSize + 1) * i + j) * 3 + 0] = i;
                            triangleVertices[((gridSize + 1) * i + j) * 3 + 1] = 0;
                            triangleVertices[((gridSize + 1) * i + j) * 3 + 2] = j;

                            uvs[((gridSize + 1) * i + j) * 2 + 0] = i/(float) gridSize;
                            uvs[((gridSize + 1) * i + j) * 2 + 1] = j / (float)gridSize;
                        }
                    }

                    for (int i = 0; i < gridSize; i++)
                    {
                        for (int j = 0; j < gridSize; j++)
                        {
                            triangleIndices[(gridSize * i + j) * 6 + 0] = ((gridSize + 1) * i + j) + (gridSize + 1) * 0 + 0;
                            triangleIndices[(gridSize * i + j) * 6 + 1] = ((gridSize + 1) * i + j) + (gridSize + 1) * 0 + 1;
                            triangleIndices[(gridSize * i + j) * 6 + 2] = ((gridSize + 1) * i + j) + (gridSize + 1) * 1 + 0;
                            
                            triangleIndices[(gridSize * i + j) * 6 + 3] = ((gridSize + 1) * i + j) + (gridSize + 1) * 1 + 0;
                            triangleIndices[(gridSize * i + j) * 6 + 4] = ((gridSize + 1) * i + j) + (gridSize + 1) * 0 + 1;
                            triangleIndices[(gridSize * i + j) * 6 + 5] = ((gridSize + 1) * i + j) + (gridSize + 1) * 1 + 1;
                        }
                    }


                    //upload model vertices to a vbo
                    var vboTriangleVertices = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboTriangleVertices);
                    GL.BufferData(BufferTarget.ArrayBuffer, triangleVertices.Length * sizeof(float), triangleVertices, BufferUsageHint.StaticDraw);


                    // upload model indices to a vbo
                    vboTriangleIndices = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboTriangleIndices);
                    GL.BufferData(BufferTarget.ElementArrayBuffer, triangleIndices.Length * sizeof(int), triangleIndices, BufferUsageHint.StaticDraw);



                    var vboUvs = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboUvs);
                    GL.BufferData(BufferTarget.ArrayBuffer, uvs.Length * sizeof(float), uvs, BufferUsageHint.StaticDraw);



                    //set up a vao
                    vaoTriangle = GL.GenVertexArray();
                    GL.BindVertexArray(vaoTriangle);

                    GL.EnableVertexAttribArray(GL.GetAttribLocation(hProgram, "pos"));
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboTriangleVertices);
                    GL.VertexAttribPointer(GL.GetAttribLocation(hProgram, "pos"), 3, VertexAttribPointerType.Float, false, 0, 0);



                    GL.EnableVertexAttribArray(GL.GetAttribLocation(hProgram, "uvVertex"));
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboUvs);
                    GL.VertexAttribPointer(GL.GetAttribLocation(hProgram, "uvVertex"), 2, VertexAttribPointerType.Float, false, 0, 0);


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

                    image = new Bitmap("./height.bmp");
                    imageW = image.Width;
                    h = image.Height;
                    heightData = new byte[imageW * h * 3];
                    data = image.LockBits(new Rectangle(0, 0, imageW, h), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    unsafe
                    {
                        byte* p = (byte*)data.Scan0;
                        for (int i = 0; i < imageW * h * 3; i += 3)
                        {
                            heightData[i] = *(p++);//r
                            heightData[i + 1] = *(p++);//g
                            heightData[i + 2] = *(p++);//b

                        }

                        image.UnlockBits(data);
                    }

                    GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
                    GL.GenTextures(1, out hHeight);
                    GL.BindTexture(TextureTarget.Texture2D, hHeight);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Srgb8, image.Width, image.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgb, PixelType.UnsignedByte, heightData);
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

                    GL.ActiveTexture(TextureUnit.Texture1);
                    GL.BindTexture(TextureTarget.Texture2D, hHeight);
                    GL.Uniform1(GL.GetUniformLocation(hProgram, "height"), 1);

                    var scale = Matrix4.CreateScale(5f/gridSize);
                    var transCenter = Matrix4.CreateTranslation(-gridSize/2, 0f, -gridSize / 2);
                    var rotate = Matrix4x4.CreateFromYawPitchRoll((float)time / 2, 0, 0).ToGlMatrix();
                    var tilt = Matrix4x4.CreateFromYawPitchRoll(0, 0.2f, 0).ToGlMatrix();

                    var translate = Matrix4.CreateTranslation(0f, -1.5f, -5f);
                    var perspective = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 2, 1, 1, 100);

                    var allTransforms =  transCenter * scale * rotate * tilt * translate;

                    GL.UniformMatrix4(GL.GetUniformLocation(hProgram, "model"), false, ref allTransforms);
                    allTransforms = allTransforms * perspective;
                    GL.UniformMatrix4(GL.GetUniformLocation(hProgram, "projection"), false, ref allTransforms);



                    //render our model
                    GL.BindVertexArray(vaoTriangle);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboTriangleIndices);
                    GL.DrawElements(PrimitiveType.Triangles, triangleIndices.Length, DrawElementsType.UnsignedInt, 0);


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
