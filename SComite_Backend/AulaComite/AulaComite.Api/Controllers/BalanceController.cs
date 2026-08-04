using Microsoft.AspNetCore.Mvc;
using AulaComite.Application.Balance.Queries;
using Microsoft.AspNetCore.Authorization;
using MediatR;

namespace AulaComite.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ManejoFinanciero")]
    public class BalanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BalanceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("aula/{aulaId:int}")]
        public async Task<IActionResult> GetConsolidado(int aulaId, [FromQuery] int anioLectivo, [FromQuery] int? mes)
        {
            var result = await _mediator.Send(new GetBalanceConsolidadoQuery(aulaId, anioLectivo, mes));
            return Ok(result);
        }
    }
}
