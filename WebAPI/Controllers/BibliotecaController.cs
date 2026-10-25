using Core.Output;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Interface;

namespace WebAPI.Controllers {
    /// <summary>
    /// Consulta da biblioteca de jogos do usuário.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class BibliotecaController : Controller {
        private readonly IBibliotecaService _bibliotecaService;

        public BibliotecaController(IBibliotecaService bibliotecaService) {
            _bibliotecaService = bibliotecaService;
        }

        /// <summary>
        /// Lista os jogos da biblioteca de um usuário.
        /// </summary>
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(ServiceResponse<List<JogoResposta>>), StatusCodes.Status200OK)]
        [Authorize]
        public async Task<IActionResult> ListarPorUsuarioAsync(int userId) {
            var jogos = await _bibliotecaService.ListarPorUsuario(userId);
            return Ok(jogos);
        }
    }
}