using Core.Input;

namespace Core.Entidade
{
    public class Jogo : BaseEntity
    {
        public string Nome { get; set; } = string.Empty;

        public decimal Preco { get; set; }

        public static Jogo Criar(JogoAdicionarRequisicao input)
        {
            return new Jogo { Nome = input.Nome, Preco = input.Preco };
        }

        public void Atualizar(JogoAtualizarRequisicao input)
        {
            Nome = input.Nome;
            Preco = input.Preco;
        }
    }
}
