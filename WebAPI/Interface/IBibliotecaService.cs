using Core.Output;

namespace WebAPI.Interface {
    public interface IBibliotecaService {
        Task<ServiceResponse<List<JogoResposta>>> ListarPorUsuario(int userId);
    }
}