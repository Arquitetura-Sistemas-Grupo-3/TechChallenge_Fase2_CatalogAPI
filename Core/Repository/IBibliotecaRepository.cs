using Core.Entidade;
using Core.Repository;

namespace Core.Repository
{
    public interface IBibliotecaRepository : IRepository<BibliotecaJogo>
    {
        Task<List<BibliotecaJogo>> GetByUsuario(int userId);
        Task<bool> Existe(int userId, int jogoId);
    }
}
