using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FCMathLib.Numerics
{
    using MathNet.Numerics.LinearAlgebra;

    public static class LinAlg
    {
        public static double[,] GramSchmidtRow(double[,] spectra)
        {
            var X = Matrix<double>.Build.DenseOfArray(spectra);

            // Use 1 thread per logical processor (leave one free for the main loop)
            var logicalProcessors = Environment.ProcessorCount;
            var nTasks = logicalProcessors - 1;

            var nRows = X.RowCount;
            var nCols = X.ColumnCount;

            // Initialize counters
            var rowsToProcess = nRows;
            var rowsPerTask = (int)Math.Floor((double)rowsToProcess / nTasks);
            var missingRows = rowsToProcess - nTasks * rowsPerTask;

            for (int i = 0; i < nRows; i++)
            {
                // Normalize adjusted row
                X.SetRow(i, X.Row(i).Normalize(2));

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
            }

            return X.ToArray();
        }

        public static double[,] GramSchmidtColumn(double[,] spectra)
        {
            var X = Matrix<double>.Build.DenseOfArray(spectra);

            // Use 1 thread per logical processor (leave one free for the main loop)
            var logicalProcessors = Environment.ProcessorCount;
            var nTasks = logicalProcessors - 1;

            var nRows = X.RowCount;
            var nCols = X.ColumnCount;

            // Initialize counters
            var colsToProcess = nRows;
            var colsPerTask = (int)Math.Floor((double)colsToProcess / nTasks);
            var missingCols = colsToProcess - nTasks * colsPerTask;

            for (int i = 0; i < nCols; i++)
            {
                // Normalize adjusted row
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
                    var cols = j != nTasks ? colsPerTask : colsPerTask + missingCols;

                    // Run task for given data slice
                    tasks.Add(Task.Run(() =>
                        OrthogonalizeCols(X, i, idx, cols)));
                }

                Task.WaitAll(tasks.ToArray());
            }

            return X.ToArray();
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

        private static int OrthogonalizeRows(Matrix<double> M, int i, int idx, int nRows)
        {
            var q_j = M.Row(i);
            var end = idx + nRows;

            for (int k = idx; k < end; k++)
            {
                var v_k = M.Row(k);
                M.SetRow(k, v_k - ((q_j * v_k) * q_j));
            }

            return 1;
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

        private static int OrthogonalizeCols(Matrix<double> M, int i, int idx, int nCols)
        {
            var q_j = M.Column(i);
            var end = idx + nCols;

            for (int k = idx; k < end; k++)
            {
                var v_k = M.Column(k);
                M.SetColumn(k, v_k - ((q_j * v_k) * q_j));
            }

            return 1;
        }
    }
}
