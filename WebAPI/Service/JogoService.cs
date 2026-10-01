using Core.Entidade;
using Core.Input;
using Core.Output;
using Core.Repository;
using Infra.Exceptions;
using WebAPI.Interface;

namespace WebAPI.Service
{
    public class JogoService : IJogoService
    {
        private readonly IJogoRepository _jogoRepository;
        private readonly ILogger<JogoService> _logger;

        public JogoService(IJogoRepository jogoRepository, ILogger<JogoService> logger)
        {
            _jogoRepository = jogoRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<List<JogoResposta>>> Listar(string? nome = null)
        {
            _logger.LogInformation("Buscando jogos com filtro Nome={Nome}", nome);

            var jogos = await _jogoRepository.GetAll();

            if (!string.IsNullOrWhiteSpace(nome))
                jogos = jogos
                    .Where(j => j.Nome.Contains(nome, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            _logger.LogInformation("Retornados {Count} jogos", jogos.Count);

            return ServiceResponse<List<JogoResposta>>.Ok(jogos.Select(Mapear).ToList());
        }

        public async Task<ServiceResponse<JogoResposta>> BuscarPorId(int id)
        {
            _logger.LogInformation("Buscando jogo {Id}", id);

            var jogo = await _jogoRepository.GetById(id)
                ?? throw new ExceptionJogoNaoEncontrado("Jogo não encontrado");

            return ServiceResponse<JogoResposta>.Ok(Mapear(jogo));
        }

        public Task<ServiceResponse<JogoAdicionarResposta>> AdicionarJogo(JogoAdicionarRequisicao input)
        {
            _logger.LogInformation("Adicionando jogo {Nome}", input.Nome);

            var jogo = Jogo.Criar(input);
            _jogoRepository.Add(jogo);

            _logger.LogInformation("Jogo {Nome} adicionado com sucesso, Id={Id}", input.Nome, jogo.Id);

            return Task.FromResult(ServiceResponse<JogoAdicionarResposta>.Ok(
                new JogoAdicionarResposta { Id = jogo.Id }, "Jogo adicionado com sucesso"));
        }

        public async Task<ServiceResponse> AtualizarJogo(JogoAtualizarRequisicao input, int id)
        {
            _logger.LogInformation("Atualizando jogo {Id}", id);

            var jogo = await _jogoRepository.GetById(id)
                ?? throw new ExceptionJogoNaoEncontrado("Jogo não encontrado");

            jogo.Atualizar(input);
            _jogoRepository.Update(jogo);

            _logger.LogInformation("Jogo {Id} atualizado com sucesso", id);

            return ServiceResponse.Ok("Jogo atualizado com sucesso");
        }

        public async Task<ServiceResponse> RemoverJogo(int id)
        {
            _logger.LogInformation("Removendo jogo {Id}", id);

            var jogo = await _jogoRepository.GetById(id)
                ?? throw new ExceptionJogoNaoEncontrado("Jogo não encontrado");

            jogo.Desativar();
            _jogoRepository.Update(jogo);

            _logger.LogInformation("Jogo {Id} removido com sucesso", id);

            return ServiceResponse.Ok("Deletado com sucesso");
        }

        private static JogoResposta Mapear(Jogo jogo) => new()
        {
            Id = jogo.Id,
            Nome = jogo.Nome,
            Preco = jogo.Preco,
            Data = jogo.Data
        };
    }
}
