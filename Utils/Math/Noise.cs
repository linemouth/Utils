using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class Noise
    {
        // Permutation table
        private static readonly int[] Source = {
            151, 160, 137,  91,  90,  15, 131,  13, 201,  95,  96,  53, 194, 233,   7, 225,
            140,  36, 103,  30,  69, 142,   8,  99,  37, 240,  21,  10,  23, 190,   6, 148,
            247, 120, 234,  75,   0,  26, 197,  62,  94, 252, 219, 203, 117,  35,  11,  32,
             57, 177,  33,  88, 237, 149,  56,  87, 174,  20, 125, 136, 171, 168,  68, 175,
             74, 165,  71, 134, 139,  48,  27, 166,  77, 146, 158, 231,  83, 111, 229, 122,
             60, 211, 133, 230, 220, 105,  92,  41,  55,  46, 245,  40, 244, 102, 143,  54,
             65,  25,  63, 161,   1, 216,  80,  73, 209,  76, 132, 187, 208,  89,  18, 169,
            200, 196, 135, 130, 116, 188, 159,  86, 164, 100, 109, 198, 173, 186,   3,  64,
             52, 217, 226, 250, 124, 123,   5, 202,  38, 147, 118, 126, 255,  82,  85, 212,
            207, 206,  59, 227,  47,  16,  58,  17, 182, 189,  28,  42, 223, 183, 170, 213,
            119, 248, 152,   2,  44, 154, 163,  70, 221, 153, 101, 155, 167,  43, 172,   9,
            129,  22,  39, 253,  19,  98, 108, 110,  79, 113, 224, 232, 178, 185, 112, 104,
            218, 246,  97, 228, 251,  34, 242, 193, 238, 210, 144,  12, 191, 179, 162, 241,
             81,  51, 145, 235, 249,  14, 239, 107,  49, 192, 214,  31, 181, 199, 106, 157,
            184,  84, 204, 176, 115, 121,  50,  45, 127,   4, 150, 254, 138, 236, 205,  93,
            222, 114,  67,  29,  24,  72, 243, 141, 128, 195,  78,  66, 215,  61, 156, 180,
            151, 160, 137,  91,  90,  15, 131,  13, 201,  95,  96,  53, 194, 233,   7, 225,
            140,  36, 103,  30,  69, 142,   8,  99,  37, 240,  21,  10,  23, 190,   6, 148,
            247, 120, 234,  75,   0,  26, 197,  62,  94, 252, 219, 203, 117,  35,  11,  32,
             57, 177,  33,  88, 237, 149,  56,  87, 174,  20, 125, 136, 171, 168,  68, 175,
             74, 165,  71, 134, 139,  48,  27, 166,  77, 146, 158, 231,  83, 111, 229, 122,
             60, 211, 133, 230, 220, 105,  92,  41,  55,  46, 245,  40, 244, 102, 143,  54,
             65,  25,  63, 161,   1, 216,  80,  73, 209,  76, 132, 187, 208,  89,  18, 169,
            200, 196, 135, 130, 116, 188, 159,  86, 164, 100, 109, 198, 173, 186,   3,  64,
             52, 217, 226, 250, 124, 123,   5, 202,  38, 147, 118, 126, 255,  82,  85, 212,
            207, 206,  59, 227,  47,  16,  58,  17, 182, 189,  28,  42, 223, 183, 170, 213,
            119, 248, 152,   2,  44, 154, 163,  70, 221, 153, 101, 155, 167,  43, 172,   9,
            129,  22,  39, 253,  19,  98, 108, 110,  79, 113, 224, 232, 178, 185, 112, 104,
            218, 246,  97, 228, 251,  34, 242, 193, 238, 210, 144,  12, 191, 179, 162, 241,
             81,  51, 145, 235, 249,  14, 239, 107,  49, 192, 214,  31, 181, 199, 106, 157,
            184,  84, 204, 176, 115, 121,  50,  45, 127,   4, 150, 254, 138, 236, 205,  93,
            222, 114,  67,  29,  24,  72, 243, 141, 128, 195,  78,  66, 215,  61, 156, 180,
        };
        // Gradient vectors for 3D (pointing to mid points of all edges of a unit cube)
        private static readonly Float3[] Grad3 =
        {
            new Float3( 1,  1,  0), new Float3(-1,  1,  0), new Float3( 1, -1,  0),
            new Float3(-1, -1,  0), new Float3( 1,  0,  1), new Float3(-1,  0,  1),
            new Float3( 1,  0, -1), new Float3(-1,  0, -1), new Float3( 0,  1,  1),
            new Float3( 0, -1,  1), new Float3( 0,  1, -1), new Float3( 0, -1, -1)
        };
        // Gradient vectors for 2D (pointing to mid points of all edges of a unit square)
        private static readonly Float2[] Grad2 =
        {
            new Float2(1, 0), new Float2(-1, 0), new Float2(0, 1), new Float2(0, -1)
        };
        // Constants
        private const int RandomSize = 256;
        private const double Sqrt3 = 1.7320508075688772935;
        private const double Sqrt5 = 2.2360679774997896964;
        // Skewing and unskewing factors for 2D, 3D and 4D, some of them pre-multiplied.
        private const double F2 = 0.5 * (Sqrt3 - 1.0);
        private const double G2 = (3.0 - Sqrt3) / 6.0;
        private const double G22 = G2 * 2.0 - 1;
        private const double F3 = 1.0 / 3.0;
        private const double G3 = 1.0 / 6.0;
        private const double F4 = (Sqrt5 - 1.0) / 4.0;
        private const double G4 = (5.0 - Sqrt5) / 20.0;
        private const double G42 = G4 * 2.0;
        private const double G43 = G4 * 3.0;
        private const double G44 = G4 * 4.0 - 1.0;

        /// <summary>Generates value, typically in range [-1, 1].</summary>
        public static double Evaluate(Float3 point)
        {
            double x = point.X;
            double y = point.Y;
            double z = point.Z;
            double n0 = 0, n1 = 0, n2 = 0, n3 = 0;

            // Noise contributions from the four corners
            // Skew the input space to determine which simplex cell we're in
            double s = (x + y + z) * F3;

            // for 3D
            int i = Math.FloorToInt(x + s);
            int j = Math.FloorToInt(y + s);
            int k = Math.FloorToInt(z + s);

            double t = (i + j + k) * G3;

            // The x,y,z distances from the cell origin
            Float3 v0 = new Float3((float)(x - (i - t)), (float)(y - (j - t)), (float)(z - (k - t)));

            // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
            // Determine which simplex we are in.
            // Offsets for second corner of simplex in (i,j,k)
            int i1, j1, k1;

            // coords
            int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords

            if (v0.X >= v0.Y)
            {
                if (v0.Y >= v0.Z)
                {
                    // X Y Z order
                    i1 = 1;
                    j1 = 0;
                    k1 = 0;
                    i2 = 1;
                    j2 = 1;
                    k2 = 0;
                }
                else if (v0.X >= v0.Z)
                {
                    // X Z Y order
                    i1 = 1;
                    j1 = 0;
                    k1 = 0;
                    i2 = 1;
                    j2 = 0;
                    k2 = 1;
                }
                else
                {
                    // Z X Y order
                    i1 = 0;
                    j1 = 0;
                    k1 = 1;
                    i2 = 1;
                    j2 = 0;
                    k2 = 1;
                }
            }
            else
            {
                if (v0.Y < v0.Z)
                {
                    // Z Y X order
                    i1 = 0;
                    j1 = 0;
                    k1 = 1;
                    i2 = 0;
                    j2 = 1;
                    k2 = 1;
                }
                else if (v0.X < v0.Z)
                {
                    // Y Z X order
                    i1 = 0;
                    j1 = 1;
                    k1 = 0;
                    i2 = 0;
                    j2 = 1;
                    k2 = 1;
                }
                else
                {
                    // Y X Z order
                    i1 = 0;
                    j1 = 1;
                    k1 = 0;
                    i2 = 1;
                    j2 = 1;
                    k2 = 0;
                }
            }

            // A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
            // a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z),
            // and
            // a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z),
            // where c = 1/6.

            // Offsets for second corner in (x,y,z) coords
            Float3 v1 = new Float3((float)(v0.X - i1 + G3), (float)(v0.Y - j1 + G3), (float)(v0.Z - k1 + G3));

            // Offsets for third corner in (x,y,z)
            Float3 v2 = new Float3((float)(v0.X - i2 + F3), (float)(v0.Y - j2 + F3), (float)(v0.Z - k2 + F3));

            // Offsets for last corner in (x,y,z)
            Float3 v3 = new Float3((float)(v0.X - 0.5), (float)(v0.Y - 0.5), (float)(v0.Z - 0.5));

            // Work out the hashed gradient indices of the four simplex corners
            int ii = i & 0xff;
            int jj = j & 0xff;
            int kk = k & 0xff;

            // Calculate the contribution from the four corners
            double t0 = 0.6 - v0.SqrMagnitude;
            if (t0 > 0)
            {
                t0 *= t0;
                int gi0 = Source[ii + Source[jj + Source[kk]]] % 12;
                n0 = t0 * t0 * Float3.Dot(Grad3[gi0], v0);
            }

            double t1 = 0.6 - v1.SqrMagnitude;
            if (t1 > 0)
            {
                t1 *= t1;
                int gi1 = Source[ii + i1 + Source[jj + j1 + Source[kk + k1]]] % 12;
                n1 = t1 * t1 * Float3.Dot(Grad3[gi1], v1);
            }

            double t2 = 0.6 - v2.SqrMagnitude;
            if (t2 > 0)
            {
                t2 *= t2;
                int gi2 = Source[ii + i2 + Source[jj + j2 + Source[kk + k2]]] % 12;
                n2 = t2 * t2 * Float3.Dot(Grad3[gi2], v2);
            }

            double t3 = 0.6 - v3.SqrMagnitude;
            if (t3 > 0)
            {
                t3 *= t3;
                int gi3 = Source[ii + 1 + Source[jj + 1 + Source[kk + 1]]] % 12;
                n3 = t3 * t3 * Float3.Dot(Grad3[gi3], v3);
            }

            // Add contributions from each corner to get the final noise value.
            // The result is scaled to stay just inside [-1,1]
            return (n0 + n1 + n2 + n3) * 32;
        }
        public static double Evaluate(Float2 point)
        {
            double x = point.x;
            double y = point.y;
            double n0 = 0, n1 = 0, n2 = 0, n3 = 0;

            // Noise contributions from the four corners
            // Skew the input space to determine which simplex cell we're in
            double s = (x + y) * F2;

            // for 3D
            int i = Math.FloorToInt(x + s);
            int j = Math.FloorToInt(y + s);

            double t = (i + j) * G2;

            // The x,y,z distances from the cell origin
            Float2 v0 = new Float2((float)(x - (i - t)), (float)(y - (j - t)));

            int i1 = v0.x >= v0.y ? 1 : 0;
            int j1 = v0.y >= v0.x ? 1 : 0;
            int i2 = v0.x  < v0.y ? 1 : 0;
            int j2 = v0.y  < v0.x ? 1 : 0;

            // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y),
            // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y),
            // where c = 1/3.

            // Offsets for second corner in (x,y) coords
            Float2 v1 = new Float2((float)(v0.x - i1 + G3), (float)(v0.y - j1 + G3));

            // Offsets for third corner in (x,y)
            Float2 v2 = new Float2((float)(v0.x - i2 + F3), (float)(v0.y - j2 + F3));

            // Offsets for last corner in (x,y)
            Float2 v3 = new Float2((float)(v0.x - 0.5), (float)(v0.y - 0.5));

            // Work out the hashed gradient indices of the four simplex corners
            int ii = i & 0xff;
            int jj = j & 0xff;

            // Calculate the contribution from the four corners
            double t0 = 0.6 - v0.SqrMagnitude;
            if (t0 > 0)
            {
                t0 *= t0;
                int gi0 = Source[ii + Source[jj]] % 12;
                n0 = t0 * t0 * Float2.Dot(Grad2[gi0], v0);
            }

            double t1 = 0.6 - v1.SqrMagnitude;
            if (t1 > 0)
            {
                t1 *= t1;
                int gi1 = Source[ii + i1 + Source[jj + j1]] % 12;
                n1 = t1 * t1 * Float2.Dot(Grad2[gi1], v1);
            }

            double t2 = 0.6 - v2.SqrMagnitude;
            if (t2 > 0)
            {
                t2 *= t2;
                int gi2 = Source[ii + i2 + Source[jj + j2]] % 12;
                n2 = t2 * t2 * Float2.Dot(Grad2[gi2], v2);
            }

            double t3 = 0.6 - v3.SqrMagnitude;
            if (t3 > 0)
            {
                t3 *= t3;
                int gi3 = Source[ii + 1 + Source[jj + 1]] % 12;
                n3 = t3 * t3 * Float2.Dot(Grad2[gi3], v3);
            }

            // Add contributions from each corner to get the final noise value.
            // The result is scaled to stay just inside [-1,1]
            return (n0 + n1 + n2 + n3) * 32;
        }

        /*
        public static float GetPerlinNoise(float x, float y)
        {
            // Determine grid cell coordinates
            int x0 = ((int)x) & 0xFF;
            int x1 = (x0 + 1) & 0xFF;
            int y0 = ((int)y) & 0xFF;
            int y1 = (y0 + 1) & 0xFF;

            // Determine interpolation weights
            // Could also use higher order polynomial/s-curve here
            float sx = x - x0;
            float sy = y - y0;

            // Interpolate between grid point gradients
            float n0, n1, ix0, ix1, value;

            n0 = DotGridGradient(x0, y0, x, y);
            n1 = DotGridGradient(x1, y0, x, y);
            ix0 = Interpolate(n0, n1, sx);

            n0 = DotGridGradient(x0, y1, x, y);
            n1 = DotGridGradient(x1, y1, x, y);
            ix1 = Interpolate(n0, n1, sx);

            value = Interpolate(ix0, ix1, sy);
            return value;
        }

        private static float Interpolate(float a0, float a1, float w)
        {
            // You may want clamping by inserting:
            //if (0.0 > w) return a0;
            //if (1.0 < w) return a1;
            return (a1 - a0) * w + a0;
            
            // Use this cubic interpolation [[Smoothstep]] instead, for a smooth appearance:
            //return (a1 - a0) * (3.0 - w * 2.0) * w * w + a0;
            
            // Use [[Smootherstep]] for an even smoother result with a second derivative equal to zero on boundaries:
            //return (a1 - a0) * (x * (w * 6.0 - 15.0) * w * w *w + 10.0) + a0;
        }
        // Create random direction vector
        private static Float2 RandomGradient(int ix, int iy)
        {
            // Random float. No precomputed gradients mean this works for any number of grid coordinates
            float random = 2920f * Math.Sin(ix * 21942f + iy * 171324f + 8912f) * Math.Cos(ix * 23157f * iy * 217832f + 9758f);
            return new Float2(Math.Cos(random), Math.Sin(random));
        }
        // Computes the dot product of the distance and gradient vectors.
        private static float DotGridGradient(int ix, int iy, float x, float y)
        {
            // Get gradient from integer coordinates
            Float2 gradient = RandomGradient(ix, iy);

            // Compute the distance vector
            float dx = x - ix;
            float dy = y - iy;

            // Compute the dot-product
            return (dx * gradient.X + dy * gradient.Y);
        }*/
    }
}
