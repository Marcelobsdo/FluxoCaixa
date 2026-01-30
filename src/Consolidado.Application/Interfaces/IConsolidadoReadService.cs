using Consolidado.Application.DTOs;

namespace Consolidado.Application.Interfaces;

public interface IConsolidadoReadService
{
    Task<SaldoDiarioDto?> ObterSaldoDiarioAsync(Guid comercianteId, DateTime dia, CancellationToken cancellationToken);
}
