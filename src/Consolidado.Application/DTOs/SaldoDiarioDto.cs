namespace Consolidado.Application.DTOs;
public sealed record SaldoDiarioDto(
    Guid ComercianteId,
    DateTime Dia,
    decimal TotalCreditos,
    decimal TotalDebitos,
    decimal Saldo
);
