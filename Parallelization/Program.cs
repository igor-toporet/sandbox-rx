using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Parallelization
{
    class Program
    {
        static void Main()
        {
            var source = Enumerable.Range(0, 100);

            var repoSequence = Observable.Defer(() => Observable.Return(source));


            var queue = new ConcurrentQueue<int>(source);
            var r = new Random();
            Parallel.ForEach(queue, (item, loopState) =>
                {
                    int milliseconds = r.Next(200, 500);
                    Thread.Sleep(milliseconds);

                    if (milliseconds > 450)
                    {
                        throw new Exception("surprize");
                    }
                    //results.Enqueue(item);
                    Console.WriteLine(item);
                });

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Press <Enter> to exit.");
            Console.ReadLine();
        }


        static void Main_1()
        {

            // Source must be array or IList.
            var source = Enumerable.Range(0, 100).ToArray();

            // Partition the entire source array.
            var rangePartitioner = Partitioner.Create(0, source.Length);

            //double[] results = new double[source.Length];
            var results = new ConcurrentQueue<int>();

            // Loop over the partitions in parallel.
            Parallel.ForEach(rangePartitioner, (range, loopState) =>
                                                   {
                                                       // Loop over each range element without a delegate invocation.
                                                       for (int i = range.Item1; i < range.Item2; i++)
                                                       {
                                                           //results[i] = source[i] * Math.PI;
                                                           results.Enqueue(i);
                                                       }
                                                   });

            //Console.WriteLine("Operation complete. Print results? y/n");
            //char input = Console.ReadKey().KeyChar;
            //if (input == 'y' || input == 'Y')
            //{
                foreach (var d in results)
                {
                    Console.Write("{0}, ", d);
                }
            //}

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Press <Enter> to exit.");
            Console.ReadLine();
        }
    }
}