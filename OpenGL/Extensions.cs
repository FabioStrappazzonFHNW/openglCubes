using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenGL
{
    public static class Extentions
    {

        public static Matrix4 ToGlMatrix(this System.Numerics.Matrix4x4 m)
        {
            var ret = new Matrix4(m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44);
            return ret;
        }
    }
}
