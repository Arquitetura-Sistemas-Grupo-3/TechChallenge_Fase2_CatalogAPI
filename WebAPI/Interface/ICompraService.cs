using Core.Output;

namespace WebAPI.Interface
{
    public interface ICompraService
    {
        Task<ServiceResponse> IniciarCompra(int userId, int jogoId);
        Task ProcessarPagamento(Core.Events.PaymentProcessedEvent evento);
    }
}
