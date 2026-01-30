using Consolidado.Application.DTOs;
using Consolidado.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Consolidado.API.Controllers;

[ApiController]
[Route("api/consolidado")]
public sealed class ConsolidadoController : ControllerBase
{
    private readonly IConsolidadoReadService _readService;

    public ConsolidadoController(IConsolidadoReadService readService)
    {
        _readService = readService;
    }

    [HttpGet("saldo-diario")]
    [ProducesResponseType(typeof(SaldoDiarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSaldoDiario(
        [FromQuery] Guid comercianteId,
        [FromQuery] DateTime dia,
        CancellationToken ct)
    {
        if (comercianteId == Guid.Empty)
            return BadRequest("comercianteId inválido.");

        if (dia == default)
            return BadRequest("dia inválido.");

        var dto = await _readService.ObterSaldoDiarioAsync(comercianteId, dia, ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
