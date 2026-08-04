using AulaComite.Application.Periodos.Commands;
using AulaComite.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Administrador")]
    public class PeriodosController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PeriodosController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePeriodoCommand command)
        {
            var id = await _mediator.Send(command);
            return Ok(new { id, mensaje = "Periodo lectivo creado exitosamente." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePeriodoCommand command)
        {
            if (id != command.Id) return BadRequest(new { mensaje = "El ID no coincide." });

            var result = await _mediator.Send(command);
            if (!result) return NotFound(new { mensaje = "No se encontró el periodo a actualizar." });

            return Ok(new { mensaje = "Periodo lectivo actualizado exitosamente." });
        }
    }
}
