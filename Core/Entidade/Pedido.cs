namespace Core.Entidade
{
    public enum PedidoStatus
    {
        Pendente = 1,
        Aprovado = 2,
        Rejeitado = 3
    }

    public class Pedido : BaseEntity
    {
        public int UserId { get; set; }
        public int JogoId { get; set; }
        public decimal Preco { get; set; }
        public PedidoStatus Status { get; set; } = PedidoStatus.Pendente;

        public static Pedido Criar(int userId, Jogo jogo) => new()
        {
            UserId = userId,
            JogoId = jogo.Id,
            Preco = jogo.Preco
        };
    }
}
