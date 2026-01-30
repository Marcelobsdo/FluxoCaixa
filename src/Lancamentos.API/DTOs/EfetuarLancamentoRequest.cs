using Lancamentos.Domain.Enums;

namespace Lancamentos.API.DTOs;

public record EfetuarLancamentoRequest(
    Guid ComercianteId,
    decimal Valor,
    TipoLancamento Tipo,
    DateTime Data
);
