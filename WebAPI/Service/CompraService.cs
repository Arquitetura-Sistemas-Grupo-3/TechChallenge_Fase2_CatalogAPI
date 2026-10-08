using Core.Entidade;
using Core.Events;
using Core.Output;
using Core.Repository;
using Infra.Exceptions;
using WebAPI.Interface;

namespace WebAPI.Service
{
    public class CompraService : ICompraService
    {
        private readonly IJogoRepository _jogoRepository;
        private readonly IRepository<Pedido> _pedidoRepository;
        private readonly IBibliotecaRepository _bibliotecaRepository;
        private readonly ILogger<CompraService> _logger;

        public CompraService(
            IJogoRepository jogoRepository,
            IRepository<Pedido> pedidoRepository,
            IBibliotecaRepository bibliotecaRepository,
            ILogger<CompraService> logger)
        {
            _jogoRepository = jogoRepository;
            _pedidoRepository = pedidoRepository;
            _bibliotecaRepository = bibliotecaRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse> IniciarCompra(int userId, int jogoId)
        {
            var jogo = await _jogoRepository.GetById(jogoId)
                ?? throw new ExceptionJogoNaoEncontrado("Jogo não encontrado");

            var pedido = Pedido.Criar(userId, jogo);
            _pedidoRepository.Add(pedido);

            _logger.LogInformation("Pedido criado. Pedido={PedidoId} User={UserId} Jogo={JogoId}",
                pedido.Id, userId, jogoId);

            return ServiceResponse.Ok("Compra iniciada. Aguardando processamento do pagamento");
        }

        public async Task ProcessarPagamento(PaymentProcessedEvent evento)
        {
            var pedido = await _pedidoRepository.GetById(evento.OrderId);
            if (pedido is null)
            {
                _logger.LogWarning("Pedido {OrderId} não encontrado", evento.OrderId);
                return;
            }

            if (pedido.Status != PedidoStatus.Pendente)
                return;

            if (evento.Status == PaymentStatus.Approved)
            {
                pedido.Status = PedidoStatus.Aprovado;
                _pedidoRepository.Update(pedido);

                if (!await _bibliotecaRepository.Existe(pedido.UserId, pedido.JogoId))
                    _bibliotecaRepository.Add(new BibliotecaJogo { UserId = pedido.UserId, JogoId = pedido.JogoId });

                _logger.LogInformation("Pagamento aprovado. Jogo {JogoId} adicionado à biblioteca do usuário {UserId}",
                    pedido.JogoId, pedido.UserId);
            }
            else
            {
                pedido.Status = PedidoStatus.Rejeitado;
                _pedidoRepository.Update(pedido);
                _logger.LogInformation("Pagamento rejeitado para o pedido {OrderId}", pedido.Id);
            }
        }
    }
}
