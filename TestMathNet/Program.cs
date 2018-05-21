using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace TestMathNet
{
    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.Data.Text;

    using Util;

    class Program
    {
        static void Main(string[] args)
        {
            //Control.UseNativeMKL();
            Control.UseManaged();

            var A = Matrix<double>.Build.Random(5000, 4200);
            var B = A.ToRowMajorArray();
            var C = Matrix<double>.Build.DenseOfRowMajor(5000, 4200, B);
            
            

            //Stopwatch timer = Stopwatch.StartNew();
            //var C = A * B;
            //timer.Stop();

            //Console.WriteLine(string.Format("time elapsed: {0}", timer.Elapsed));
#if TEST
            double[,] x = {{ 1.0, 2.0 },
                           { 3.0, 4.0 }};

            var mat = Matrix<double>.Build.DenseOfArray(x);
            Console.WriteLine(mat);

            var mul = mat * mat;
            Console.WriteLine(mul);
#endif
            Console.WriteLine("Running Gram-Schmidt test ...");

            var n = 498000;
            var m = 4200;

            var M = util.rand(500, m);
            var N = M.Transpose();
            var H = util.rand_native(1000, m);

            //var format = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
            //var M = DelimitedReader.Read<double>(@"C:\MATLAB\hest.csv", false, ",", false, format);

            var q = M.Row(0);

            Stopwatch timer = Stopwatch.StartNew();
            //M = M.Transpose();
            GramSchmidt(N);

            //GramSchmidtRowManual(H);
            
            //for (int i = 0; i < n; i++)
            //{
            //    var v = M.Row(1);
            //    var p = v - (q * v) * q;
            //    M.SetRow(2, p);
            //}

            timer.Stop();

            Console.WriteLine(string.Format("time elapsed: {0}", timer.Elapsed));

            //q = N.Column(0);
            //timer.Restart();
            //for (int i = 0; i < n; i++)
            //{
            //    var c = N.Column(1);
            //    var r = c - (q * c) * q;
            //    N.SetColumn(2, r);
            //}
            //timer.Stop();
            //
            //Console.WriteLine(string.Format("time elapsed: {0}", timer.Elapsed));



            var hest = 5;
            Console.ReadKey();
        }

        public static void GramSchmidt(Matrix<double> X)
        {
            var epsilon = 9.094947017729282e-13;
            double low_limit = -1.0;

            // TODO: Decide this dynamically ...
            var nTasks = 1;

            var nRows = X.RowCount;
            var nCols = X.ColumnCount;

            // Initialize counters
            //var rowsToProcess = nRows;
            //var rowsPerTask = (int)Math.Floor((double)rowsToProcess / nTasks);
            //var missingRows = rowsToProcess - nTasks * rowsPerTask;

            var colsToProcess = nCols;
            var colsPerTask = (int)Math.Floor((double)colsToProcess / nTasks);
            var missingCols = colsToProcess - nTasks * colsPerTask;

            for (int i = 0; i < nCols; i++)
            {
                // Guard
                if (i == nCols - 1) break;

                // Normalize adjusted row
                //X.SetRow(i, X.Row(i).Normalize(2));
                X.SetColumn(i, X.Column(i).Normalize(2));

                // Update counters
                colsToProcess -= 1;
                colsPerTask = (int)Math.Floor((double)colsToProcess / nTasks);
                missingCols = colsToProcess - nTasks * colsPerTask;
                
                // Start threads
                var tasks = new List<Task<int>>();
                for (int j = 0; j < nTasks; j++)
                {
                    // Calculate starting idx and number of rows to process
                    var idx = (i + 1) + j * colsPerTask;
                    var rows = j != nTasks ? colsPerTask : colsPerTask + missingCols;

                    // Run task for given data slice
                    tasks.Add(Task.Run(() =>
                        OrthogonalizeColumns(X, i, idx, rows)));
                }

                Task.WaitAll(tasks.ToArray());

                //Console.WriteLine("Samples handled: {0}", i + 1);
            }
        }

        private static int OrthogonalizeColumns(Matrix<double> M, int i, int idx, int cols)
        {
            var q_j = M.Column(i);

            for (int k = idx; k < idx + cols; k++)
            {
                var v_k = M.Column(k);
                M.SetColumn(k, v_k - ((q_j * v_k) * q_j));
            }

            return 1;
        }

        private static int OrthogonalizeColumnsVectorized(Matrix<double> M, int i, int idx, int cols)
        {
            var q_j = M.Column(i);

            var V = M.SubMatrix(0, M.RowCount, idx, cols);
            var c = q_j.ToRowMatrix() * V;
            var P = q_j.ToColumnMatrix() * c;
            var S = V - P;
            M.SetSubMatrix(0, idx, S);

            return 1;
        }

        public static double[,] GramSchmidtRowManual(double[,] spectra)
        {
            //var X = Matrix<double>.Build.DenseOfArray(spectra);
            var X = spectra;

            // Use 1 thread per logical processor (leave one free for the main loop)
            var logicalProcessors = Environment.ProcessorCount;
            var nTasks = logicalProcessors - 1;

            var nRows = X.GetLength(0);
            var nCols = X.GetLength(1);

            // Initialize counters
            var rowsToProcess = nRows;
            var rowsPerTask = (int)Math.Floor((double)rowsToProcess / nTasks);
            var missingRows = rowsToProcess - nTasks * rowsPerTask;

            for (int i = 0; i < nRows; i++)
            {
                // Normalize adjusted row
                //X.SetRow(i, X.Row(i).Normalize(2));
                var ssum = 0.0;
                var suminv = 0.0;
                for (int j = 0; j < nCols; j++)
                {
                    ssum += X[i, j] * X[i, j];
                }

                ssum = Math.Sqrt(ssum);
                if (ssum > 0.0) suminv = 1 / ssum;

                for (int j = 0; j < nCols; j++)
                {
                    X[i, j] = X[i, j] * suminv;
                }

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
                        OrthogonalizeRowsManual(X, i, idx, rows)));
                }

                Task.WaitAll(tasks.ToArray());
            }

            //return X.ToArray();
            return X;
        }

        private static int OrthogonalizeRowsManual(double[,] M, int i, int idx, int nRows)
        {
            // var q_j = M.Row(i);

            var end = idx + nRows;
            var nPoints = M.GetLength(1);

            for (int k = idx; k < end; k++)
            {
                //var v_k = M.Row(k);
                //M.SetRow(k, v_k - ((q_j * v_k) * q_j));

                var prod = 0.0;

                // Projection
                for (int j = 0; j < nPoints; j++)
                {
                    prod += M[i, j] * M[k, j];
                }

                // Subtraction
                for (int j = 0; j < nPoints; j++)
                {
                    M[k, j] = M[k, j] - prod * M[i, j];
                }
            }

            return 1;
        }
    }
}
