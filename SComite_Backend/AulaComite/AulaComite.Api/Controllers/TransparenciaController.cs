using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AulaComite.Application.Aulas.Queries;
using AulaComite.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AccesoApoderado")]
    public class TransparenciaController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TransparenciaController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("aula/{aulaId:int}/balance")]
        public async Task<IActionResult> ObtenerBalanceAula(int aulaId, [FromQuery] int anio)
        {
            var anioConsulta = anio > 0 ? anio : DateTimeHelper.ObtenerHoraPeru().Year;
            var result = await _mediator.Send(new GetBalanceAulaQuery(aulaId, anioConsulta));
            return Ok(result);
        }
    }
}
