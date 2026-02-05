using Lancamentos.API.DTOs;
using Lancamentos.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lancamentos.API.Controllers;

[Authorize]
[ApiController]
[Route("api/lancamentos")]
public class LancamentosController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Efetuar(
        [FromBody] EfetuarLancamentoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EfetuarLancamentoCommand(
            request.ComercianteId,
            request.Valor,
            request.Tipo,
            request.Data);

        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(Efetuar),
            new { id },
            new { id });
    }
}
