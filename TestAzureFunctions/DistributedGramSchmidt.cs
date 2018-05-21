using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TestAzureFunctions
{
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Azure.WebJobs.Host;

    using MathNet.Numerics;
    using MathNet.Numerics.LinearAlgebra;

    public struct WorkerArgs
    {
        public Vector<double> q;
        public Matrix<double> data;
        public int i, idx, cols;
    }

    public static class DistributedGramSchmidt
    {
        [FunctionName("GramSchmidtColumn")]
        public static async Task<string> Run(
            [OrchestrationTrigger] DurableOrchestrationContextBase ctx)
        {
            // TODO: Read data from cloud storage (somewhere) ...
            var X = Matrix<double>.Build.Random(100, 4200);

            var timer = Stopwatch.StartNew();

            var nTasks = 1;

            var nRows = X.RowCount;
            var nCols = X.ColumnCount;

            var colsToProcess = nCols;
            var colsPerTask = (int)Math.Floor((double)colsToProcess / nTasks);
            var missingCols = colsToProcess - nTasks * colsPerTask;

            for (int i = 0; i < nCols; i++)
            {
                // Guard
                if (i == nCols - 1) break;

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
                    //tasks.Add(Task.Run(() =>
                    //    OrthogonalizeColumns(X, i, idx, rows)));

                    var q = X.Column(i);
                    var data = X.SubMatrix(0, nRows, idx, cols);

                    q.AsArray

                    var args = new WorkerArgs { q = q, data = data, i = i, idx = idx, cols = cols };

                    tasks.Add(ctx.CallActivityAsync<int>(
                        "OrthogonalizeColumns", args));

                    //tasks.Add(ctx.CallActivityAsync<int>(
                    //    "SayHelloGS", string.Format("Sample_{0}",i)));
                }

                await Task.WhenAll(tasks.ToArray());
            }

            timer.Stop();

            //return X;
            return string.Format("Done in {0}!", timer.Elapsed);
        }

        [FunctionName("OrthogonalizeColumns")]
        public static int OrthogonalizeColumnsWorker(
            [ActivityTrigger] DurableActivityContext ctx)
        {
            // Do this little dance since Azure activity functions can only 
            // take one argument
            /*
            var q_j = args.q;
            var M = args.data;
            var idx = args.idx;
            var cols = args.cols;
            var i = args.i;

            //var q_j = M.Column(i);
            var end_idx = idx + cols;

            for (int k = idx; k < end_idx; k++)
            {
                var v_k = M.Column(k);
                M.SetColumn(k, v_k - ((q_j * v_k) * q_j));
            }
            */

            var data = ctx.in

            return 1;
        }

        [FunctionName("SayHelloGS")]
        public static string SayHelloGS([ActivityTrigger] string name)
        {
            return $"Hello {name}!";
        }
    }
}
