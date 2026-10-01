using Core.Entidade;

namespace Core.Repository
{
    public interface IRepository<T> where T : BaseEntity
    {
        public Task<List<T>> GetAll();

        public Task<T?> GetById(int id);

        public void Add(T entity);

        public void Update(T entity);
    }
}
