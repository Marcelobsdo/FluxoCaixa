using Consolidado.Application.DTOs;
using Consolidado.Application.Interfaces;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Consolidado.Infrastructure.Queries;

public sealed class ConsolidadoReadService(IConfiguration configuration) : IConsolidadoReadService
{
    private readonly string _connectionString = configuration.GetConnectionString("ConsolidadoDb")
            ?? throw new InvalidOperationException("ConnectionStrings:ConsolidadoDb não configurada");

    public async Task<SaldoDiarioDto?> ObterSaldoDiarioAsync(Guid comercianteId, DateTime dia, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT
                              comerciante_id AS ""ComercianteId"",
                              dia::date      AS ""Dia"",
                              COALESCE(SUM(CASE WHEN tipo = 1 THEN valor ELSE 0 END), 0) AS ""TotalCreditos"",
                              COALESCE(SUM(CASE WHEN tipo = 2 THEN valor ELSE 0 END), 0) AS ""TotalDebitos"",
                              COALESCE(SUM(CASE WHEN tipo = 1 THEN valor ELSE -valor END), 0) AS ""Saldo""
                            FROM lancamentos
                            WHERE comerciante_id = @ComercianteId
                              AND dia = @Dia::date
                            GROUP BY comerciante_id, dia::date;";

        await using var conn = new NpgsqlConnection(_connectionString);

        return await conn.QuerySingleOrDefaultAsync<SaldoDiarioDto>(
            new CommandDefinition(sql, new { ComercianteId = comercianteId, Dia = dia }, cancellationToken: cancellationToken));
    }
}
