using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using CacheExample;

namespace RxTest1
{
    //public class CacheMissException : ApplicationException
    //{

    //}

    public interface IIdentifiable<TKey> where TKey:IEquatable<TKey>
    {
        TKey Key { get; set; }
    }

    public class MongoDoc : IIdentifiable<int>
    {
        public int Key { get; set; }
        public string Value { get; set; }
    }

    public class SqlRecord : IIdentifiable<int>
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public interface IRepository<out TItem, in TKey> where TItem:IIdentifiable<TKey> where TKey : IEquatable<TKey>
    {
        TItem GetResultById(TKey id);
        IEnumerable<TItem> GetAll();
    }

    public interface IStorage<in T>
    {
        void Save (T item);
    }

    public class Repository<TItem,TKey> : IRepository<TItem,TKey>, IStorage<TItem> where  TItem : IIdentifiable<TKey> where TKey : IEquatable<TKey>
    {
        private readonly ConcurrentDictionary<TKey, TItem> _storedDocs = new ConcurrentDictionary<TKey, TItem>();

        public Repository()
        {
        }

        public Repository(IEnumerable<TItem> storedDocs):this()
        {
            foreach (var doc in storedDocs)
            {
                Save(doc);
            }
        }

        public TItem GetResultById(TKey id)
        {
            Thread.Sleep(150);

            return _storedDocs[id];
        }

        public IEnumerable<TItem> GetAll()
        {
            var enumerator = _storedDocs.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Thread.Sleep(50);

                yield return enumerator.Current.Value;
            }
        }

        public void Save(TItem item)
        {
            _storedDocs[item.Key] = item;
        }
    }

    internal static class Program
    {
        private static void Main(string[] cmdLineParams)
        {
            var mongoDocs = Enumerable.Range(100, 999).Select(i => new MongoDoc {Key = i, Value = "doc-" + i});
            var mongoRepository = new Repository<MongoDoc, int>(mongoDocs);

            var queue = new ConcurrentQueue<MongoDoc>( /*mongoRepository.GetAll()*/);
            var sqlDatabase = new Repository<SqlRecord, int>();

            mongoRepository.GetAll().ToObservable().Subscribe(
                doc =>
                    {
                        
                    });

            var mongoObservable = Observable.Defer(() => Observable.Return());
            //var databaseObservable = Observable.Defer(() => Observable.Return(databaseRepository.GetResultById(id)));

            // Try to retrieve the result from the cache, falling over
            // to the DB in case of cache miss.
            var cacheFailover =
                cacheObservable
                    .Timeout(cacheTimeout)
                    .Catch<MongoDoc, CacheMissException>(
                        (x) =>
                            {
                                Console.WriteLine("Cache miss. Attempting to retrieve from database.");
                                return databaseObservable.Timeout(databaseTimeout);
                            })
                    .Catch<MongoDoc, TimeoutException>(
                        (x) =>
                            {
                                Console.WriteLine("Time out retrieving result from cache. Giving up.");
                                return Observable.Empty<MongoDoc>();
                            });

            var result = new Subject<MongoDoc>();
            result.Subscribe(
                x => Console.WriteLine("SUCCESS: Result: " + x.Value),
                x => Console.WriteLine("FAILURE: Exception!"),
                () => Console.WriteLine("Sequence finished."));

            cacheFailover.Subscribe(result);

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static void DoMapAndSave(MongoDoc doc)
        {
            throw new NotImplementedException();
        }
    }
}
