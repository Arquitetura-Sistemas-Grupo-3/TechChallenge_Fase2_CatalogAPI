using Core.Entidade;
using Core.Repository;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repository
{
    public class EFRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected ApplicationDbContext _context;
        protected DbSet<T> _dbSet;

        public EFRepository(ApplicationDbContext applicationDb)
        {
            _context = applicationDb;
            _dbSet = _context.Set<T>();
        }

        public void Add(T entity)
        {
            _dbSet.Add(entity);
            _context.SaveChanges();
        }

        public async Task<List<T>> GetAll()
        {
            return await _dbSet.Where(x => x.Ativo).ToListAsync();
        }

        public async Task<T?> GetById(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.Id == id && x.Ativo);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
            _context.SaveChanges();
        }
    }
}
