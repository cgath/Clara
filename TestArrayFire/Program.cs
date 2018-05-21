using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using ArrayFire;

namespace TestArrayFire
{
    using array = ArrayFire.Array;
    using static ArrayFire.Data;
    using static ArrayFire.Arith;
    using static ArrayFire.Util;

    class Program
    {
        static void Main(string[] args)
        {
            Device.SetBackend(Backend.CPU);
            Device.PrintInfo();

            var A = RandNormal<double>(5000, 5000);
            var B = RandNormal<double>(5000, 5000);

            Console.WriteLine(A.ElemCount);
            
            Print(A[Seq(0, 1), Seq(0, 1)], "A Array");
            Print(B[Seq(0, 1), Seq(0, 1)], "B Array");

            Stopwatch stopwatch = Stopwatch.StartNew();
            var C = Matrix.Multiply(A, B);
            Util.Print(C[Seq(0, 1), Seq(0, 1)], "C Array");
            stopwatch.Stop();
            
            Console.WriteLine(String.Format("Time elapsed: {0}", stopwatch.Elapsed));

#if TEST
            var arr = new double[2, 2];
            arr[0, 0] = 1; arr[0, 1] = 3;
            arr[1, 0] = 2; arr[1, 1] = 4;

            var mat = Data.CreateArray(arr);
            Util.Print(mat, "mat");

            var mul = mat * mat;
            Util.Print(mul, "mul");

            var matmul = Matrix.Multiply(mat, mat);
            Util.Print(matmul, "matmul");
#endif

            //var hest = 5;
            Console.ReadKey();
        }

        public static void GramSchmidt(array X)
        {
            var epsilon = 9.094947017729282e-13;
            double low_limit = -1.0;

            // TODO: Decide this dynamically ...
            var nTasks = 4;

            var nRows = X.Dimensions[0]; //X.RowCount;
            var nCols = X.Dimensions[1]; //X.ColumnCount;

            // Initialize counters
            var rowsToProcess = nRows;
            var rowsPerTask = (int)Math.Floor((double)rowsToProcess / nTasks);
            var missingRows = rowsToProcess - nTasks * rowsPerTask;

            for (int i = 0; i < nRows; i++)
            {
                // Normalize adjusted row
                //X.SetRow(i, X.Row(i).Normalize(2));
                

                // Update counters
                rowsToProcess -= 1;
                rowsPerTask = (int)Math.Floor((double)rowsToProcess / nTasks);
                missingRows = rowsToProcess - nTasks * rowsPerTask;

                // Start threads
                var tasks = new List<Task<int>>();
                for (int j = 0; j < nTasks; j++)
                {
                    // Calculate starting idx and number of rows to process
                    var idx = (i + 1) + j * rowsPerTask;
                    var rows = j != nTasks ? rowsPerTask : rowsPerTask + missingRows;

                    // Run task for given data slice
                    tasks.Add(Task.Run(() =>
                        OrthogonalizeRows(X, i, idx, rows)));
                }

                Task.WaitAll(tasks.ToArray());

                //Console.WriteLine("Samples handled: {0}", i + 1);
            }
        }

        private static int OrthogonalizeRows(Matrix<double> M, int i, int idx, int nRows)
        {
            var q_j = M.Row(i);
            for (int k = idx; k < idx + nRows; k++)
            {
                var v_k = M.Row(k);
                M.SetRow(k, v_k - ((q_j * v_k) * q_j));
            }

            return 1;
        }
    }
}
