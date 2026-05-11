namespace AverageSpeed.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using AverageSpeed.Domain;

    using Newtonsoft.Json;

    public class Repository<T> : IRepository<T> where T : IEntity
    {
        private readonly object _lockObject = new object();

        public Repository()
        {
            Store = new List<T>();
        }

        private List<T> Store { get; }

        public Task<T> GetById(string id)
        {
            lock (_lockObject)
            {
                var result = Store.ToList().FirstOrDefault(x => x.Id == id);

                // Passing ref, need to clone object.
                return Task.FromResult(result);
            }
        }

        private T DeepClone(T obj)
        {
            var serialized = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public Task Upsert(T entity)
        {
            lock (_lockObject)
            {
                var current = Store.FirstOrDefault(x => x.Id == entity.Id);
                if ((current?.ETag ?? entity.ETag) != entity.ETag)
                {
                    throw new Exception("Document Changed");
                }

                Store.RemoveAll(x => x.Id == entity.Id);
                entity.ETag = Guid.NewGuid();
                Store.Add(entity);
                return Task.CompletedTask;
            }
        }

        public Task Delete(string id)
        {
            lock (_lockObject)
            {
                Store.RemoveAll(x => x.Id == id);
                return Task.CompletedTask;
            }
        }
    }
}
