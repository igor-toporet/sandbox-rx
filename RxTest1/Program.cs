using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace CacheExample
{
    public class CacheMissException : ApplicationException
    {

    }

    // Represents the entity we are trying to retrieve from the cache
    // or database
    public class ResultEntity
    {
        public ResultEntity(string value)
        {
            Value = value;
        }

        public string Value { get; set; }
    }

    public interface IResultRepository
    {
        ResultEntity GetResultById(int id);
    }

    public class DatabaseRepository : IResultRepository
    {
        public ResultEntity GetResultById(int id)
        {
            Console.WriteLine("Retrieving results from database...");

            // Increment the following wait time to simulate a
            //database timeout.
            Thread.Sleep(150);

            // Note that this code is still executed even if the
            // observer is disposed.
            // This, conveniently, allows for "side-effects".
            // In this case we could put the result into the
            //cache so the next user gets a cache hit!
            Console.WriteLine("Retrieved result from database.");
            return new ResultEntity("Database Result");
        }
    }

    public class CacheRepository : IResultRepository
    {
        public ResultEntity GetResultById(int id)
        {
            Console.WriteLine("Retrieving result from cache...");

            //Increment the following value to simulate a cache timeout.
            Thread.Sleep(20);

            //Uncomment the next line to simulate a cache miss
            throw new CacheMissException();

            Console.WriteLine("Retrieved result from cache!");
            return new ResultEntity("Cached Result");
        }
    }

    internal class Program
    {
        private static readonly IResultRepository cacheRepository =
            new CacheRepository();

        private static readonly IResultRepository databaseRepository =
            new DatabaseRepository();

        private static void Main_old(string[] cmdLineParams)
        {
            int id = 123;
            var cacheTimeout = TimeSpan.FromMilliseconds(50);
            var databaseTimeout = TimeSpan.FromMilliseconds(200);

            var cacheObservable = Observable.Defer(() => Observable.Return(cacheRepository.GetResultById(id)));
            var databaseObservable = Observable.Defer(() => Observable.Return(databaseRepository.GetResultById(id)));

            // Try to retrieve the result from the cache, falling over
            // to the DB in case of cache miss.
            var cacheFailover =
                cacheObservable
                    .Timeout(cacheTimeout)
                    .Catch<ResultEntity, CacheMissException>(
                        (x) =>
                            {
                                Console.WriteLine("Cache miss. Attempting to retrieve from database.");
                                return databaseObservable.Timeout(databaseTimeout);
                            })
                    .Catch<ResultEntity, TimeoutException>(
                        (x) =>
                            {
                                Console.WriteLine("Time out retrieving result from cache. Giving up.");
                                return Observable.Empty<ResultEntity>();
                            });

            var result = new Subject<ResultEntity>();
            result.Take(1).Subscribe(
                x => Console.WriteLine("SUCCESS: Result: " + x.Value),
                x => Console.WriteLine("FAILURE: Exception!"),
                () => Console.WriteLine("Sequence finished."));

            cacheFailover.Subscribe(result);

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
