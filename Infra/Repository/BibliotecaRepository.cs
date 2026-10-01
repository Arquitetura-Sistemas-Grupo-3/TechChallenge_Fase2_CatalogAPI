using Core.Entidade;
using Core.Repository;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repository
{
    public class BibliotecaRepository : EFRepository<BibliotecaJogo>, IBibliotecaRepository
    {
        public BibliotecaRepository(ApplicationDbContext applicationDb) : base(applicationDb)
        {
        }

        public Task<List<BibliotecaJogo>> GetByUsuario(int userId) =>
            _dbSet.Where(x => x.Ativo && x.UserId == userId).ToListAsync();

        public Task<bool> Existe(int userId, int jogoId) =>
            _dbSet.AnyAsync(x => x.Ativo && x.UserId == userId && x.JogoId == jogoId);
    }
}
