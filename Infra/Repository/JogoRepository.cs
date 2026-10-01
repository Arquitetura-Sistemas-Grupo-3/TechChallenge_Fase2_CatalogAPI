using Core.Entidade;
using Core.Repository;

namespace Infra.Repository
{
    public class JogoRepository : EFRepository<Jogo>, IJogoRepository
    {
        public JogoRepository(ApplicationDbContext applicationDb) : base(applicationDb)
        {
        }
    }
}
