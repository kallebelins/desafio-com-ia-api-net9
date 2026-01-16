using System.Diagnostics.Metrics;

namespace DesafioComIA.Application.Telemetry;

/// <summary>
/// Métricas customizadas para operações de clientes.
/// </summary>
public class ClienteMetrics
{
    /// <summary>
    /// Nome do meter para métricas de clientes.
    /// </summary>
    public const string MeterName = "DesafioComIA.Clientes";

    private readonly Counter<long> _clientesCriados;
    private readonly Counter<long> _clientesAtualizados;
    private readonly Counter<long> _clientesRemovidos;
    private readonly Counter<long> _buscasRealizadas;
    private readonly Histogram<double> _tempoProcessamento;

    /// <summary>
    /// Cria uma nova instância de ClienteMetrics.
    /// </summary>
    /// <param name="meterFactory">Factory de meters do OpenTelemetry.</param>
    public ClienteMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _clientesCriados = meter.CreateCounter<long>(
            name: "clientes.criados",
            unit: "{cliente}",
            description: "Total de clientes criados");

        _clientesAtualizados = meter.CreateCounter<long>(
            name: "clientes.atualizados",
            unit: "{cliente}",
            description: "Total de clientes atualizados (PUT e PATCH)");

        _clientesRemovidos = meter.CreateCounter<long>(
            name: "clientes.removidos",
            unit: "{cliente}",
            description: "Total de clientes removidos");

        _buscasRealizadas = meter.CreateCounter<long>(
            name: "clientes.buscas",
            unit: "{busca}",
            description: "Total de buscas realizadas (listagem e pesquisa)");

        _tempoProcessamento = meter.CreateHistogram<double>(
            name: "clientes.processamento.tempo",
            unit: "ms",
            description: "Tempo de processamento das operações de clientes em milissegundos");
    }

    /// <summary>
    /// Registra a criação de um cliente.
    /// </summary>
    public void ClienteCriado() => _clientesCriados.Add(1);

    /// <summary>
    /// Registra a atualização de um cliente.
    /// </summary>
    public void ClienteAtualizado() => _clientesAtualizados.Add(1);

    /// <summary>
    /// Registra a remoção de um cliente.
    /// </summary>
    public void ClienteRemovido() => _clientesRemovidos.Add(1);

    /// <summary>
    /// Registra uma busca realizada.
    /// </summary>
    public void BuscaRealizada() => _buscasRealizadas.Add(1);

    /// <summary>
    /// Registra o tempo de processamento de uma operação.
    /// </summary>
    /// <param name="milliseconds">Tempo em milissegundos.</param>
    public void RegistrarTempoProcessamento(double milliseconds) => _tempoProcessamento.Record(milliseconds);

    /// <summary>
    /// Registra o tempo de processamento de uma operação com tags adicionais.
    /// </summary>
    /// <param name="milliseconds">Tempo em milissegundos.</param>
    /// <param name="operacao">Nome da operação.</param>
    /// <param name="sucesso">Se a operação foi bem-sucedida.</param>
    public void RegistrarTempoProcessamento(double milliseconds, string operacao, bool sucesso) =>
        _tempoProcessamento.Record(
            milliseconds,
            new KeyValuePair<string, object?>("operacao", operacao),
            new KeyValuePair<string, object?>("sucesso", sucesso));
}
