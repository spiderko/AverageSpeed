namespace AverageSpeed
{
    using System.Threading.Tasks;

    using Domain;

    public interface IRepository<T> where T : IEntity
    {
        Task<T> GetById(string id);

        Task Upsert(T entity);

        Task Delete(string id);
    }
}
