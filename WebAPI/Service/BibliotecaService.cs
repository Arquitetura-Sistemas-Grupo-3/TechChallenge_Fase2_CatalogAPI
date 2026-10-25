using Core.Output;
using Core.Repository;
using WebAPI.Interface;

namespace WebAPI.Service {
    public class BibliotecaService : IBibliotecaService {
        private readonly IBibliotecaRepository _bibliotecaRepository;
        private readonly IJogoRepository _jogoRepository;

        public BibliotecaService(IBibliotecaRepository bibliotecaRepository, IJogoRepository jogoRepository) {
            _bibliotecaRepository = bibliotecaRepository;
            _jogoRepository = jogoRepository;
        }

        public async Task<ServiceResponse<List<JogoResposta>>> ListarPorUsuario(int userId) {
            var biblioteca = await _bibliotecaRepository.GetByUsuario(userId);
            var jogoIds = biblioteca.Select(b => b.JogoId).ToHashSet();

            var jogos = (await _jogoRepository.GetAll())
                .Where(j => jogoIds.Contains(j.Id))
                .Select(j => new JogoResposta {
                    Id = j.Id,
                    Nome = j.Nome,
                    Preco = j.Preco,
                    Data = j.Data
                })
                .ToList();

            return ServiceResponse<List<JogoResposta>>.Ok(jogos);
        }
    }
}