using Core.Output;

namespace WebAPI.Interface {
    public interface ICompraService {
        Task<ServiceResponse> IniciarCompra(int userId, int jogoId, string userEmail, string userRole, string? userName);
        Task ProcessarPagamento(Core.Events.PaymentProcessedEvent evento);
    }
}