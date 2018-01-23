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
    static class Program
    {
        static void Main()
        {
            Cubes.Run();
            HeightMap.Run();
            Transparent.Run();
        }
    }
}
