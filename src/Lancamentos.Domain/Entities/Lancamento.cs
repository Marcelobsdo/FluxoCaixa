using Lancamentos.Domain.Enums;
using Lancamentos.Domain.Exceptions;

namespace Lancamentos.Domain.Entities;

public class Lancamento
{
    public Guid Id { get; private set; }
    public Guid ComercianteId { get; private set; }
    public decimal Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public DateTime Data { get; private set; }

    protected Lancamento() { }

    private Lancamento(Guid comercianteId, decimal valor, TipoLancamento tipo, DateTime data)
    {
        Validar(comercianteId, valor, data);

        Id = Guid.NewGuid();
        ComercianteId = comercianteId;
        Valor = valor;
        Tipo = tipo;
        Data = data;
    }

    public static Lancamento Criar(Guid comercianteId, decimal valor, TipoLancamento tipo, DateTime data)
        => new(comercianteId, valor, tipo, data);

    private static void Validar(Guid comercianteId, decimal valor, DateTime data)
    {
        if (comercianteId == Guid.Empty)
            throw new DomainException("O identificador do comerciante é inválido.");

        if (valor <= 0)
            throw new DomainException("O valor do lançamento deve ser maior que zero.");

        if (data == default)
            throw new DomainException("A data do lançamento é inválida.");
    }
}
