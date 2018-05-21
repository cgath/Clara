using System;
using System.Diagnostics;

namespace Util
{
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;

    public static class util
    {
        public static Matrix<double> rand(int rows, int cols)
        {
            return Matrix<double>.Build.Random(rows, cols);
        }

        public static double[,] rand_native(int rows, int cols)
        {
            var rng = new Random();
            var array = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    array[i, j] = rng.NextDouble();
                }
            }

            return array;
        }
    }
}
