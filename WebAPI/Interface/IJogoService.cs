using Core.Input;
using Core.Output;

namespace WebAPI.Interface
{
    public interface IJogoService
    {
        Task<ServiceResponse<List<JogoResposta>>> Listar(string? nome = null);
        Task<ServiceResponse<JogoResposta>> BuscarPorId(int id);
        Task<ServiceResponse<JogoAdicionarResposta>> AdicionarJogo(JogoAdicionarRequisicao input);
        Task<ServiceResponse> AtualizarJogo(JogoAtualizarRequisicao input, int id);
        Task<ServiceResponse> RemoverJogo(int id);
    }
}
