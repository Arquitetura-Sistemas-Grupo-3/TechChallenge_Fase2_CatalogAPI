using Core.Input;
using Core.Output;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Interface;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Operações de gerenciamento de jogos.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class JogoController : Controller
    {
        private readonly IJogoService _jogoService;

        public JogoController(IJogoService jogoService)
        {
            _jogoService = jogoService;
        }

        /// <summary>
        /// Lista todos os jogos cadastrados.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ListarAsync([FromQuery] string? nome)
        {
            var jogos = await _jogoService.Listar(nome);
            return Ok(jogos);
        }

        /// <summary>
        /// Consulta um jogo pelo identificador.
        /// </summary>
        /// <response code="404">Jogo não encontrado.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ServiceResponse<JogoResposta>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<IActionResult> BuscarPorIdAsync(int id)
        {
            var jogo = await _jogoService.BuscarPorId(id);
            return Ok(jogo);
        }

        /// <summary>
        /// Cadastra um novo jogo.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ServiceResponse<JogoAdicionarResposta>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Adicionar([FromBody] JogoAdicionarRequisicao jogoInput)
        {
            var jogo = await _jogoService.AdicionarJogo(jogoInput);
            return CreatedAtAction("BuscarPorId", new { id = jogo.Data!.Id }, jogo);
        }

        /// <summary>
        /// Inicia a compra de um jogo para o usuário autenticado (publica OrderPlacedEvent).
        /// </summary>
        /// <response code="404">Jogo não encontrado.</response>
        [HttpPost("{id}/comprar")]
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [Authorize]
        public async Task<IActionResult> Comprar([FromRoute] int id, [FromServices] ICompraService compraService) {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (!int.TryParse(idClaim, out var userId))
                return Unauthorized();

            var email = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value;

            var role = User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value;

            var nome = User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("name")?.Value;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(role))
                return Unauthorized();

            var resposta = await compraService.IniciarCompra(userId, id, email, role, nome);
            return Accepted(resposta);
        }

        /// <summary>
        /// Atualiza os dados
        /// </summary>
        /// <response code="404">Jogo não encontrado.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Atualizar([FromBody] JogoAtualizarRequisicao jogoUpdate, [FromRoute] int id)
        {
            var jogo = await _jogoService.AtualizarJogo(jogoUpdate, id);
            return Ok(jogo);
        }

        /// <summary>
        /// Remove um jogo existente.
        /// </summary>
        /// <response code="404">Jogo não encontrado.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Remover(int id)
        {
            var jogo = await _jogoService.RemoverJogo(id);
            return Ok(jogo);
        }
    }
}
