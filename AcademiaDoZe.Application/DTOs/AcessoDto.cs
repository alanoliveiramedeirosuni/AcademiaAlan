// Alan Medeiros
namespace AcademiaDoZe.Application.DTOs;

public sealed class AcessoDto
{
    public int Id { get; init; }
    public DateTime Entrada { get; init; }
    public DateTime? Saida { get; init; }
    public string EntradaTexto => $"Entrada: {Entrada:dd/MM/yyyy HH:mm:ss}";
    public string SaidaTexto => Saida is { } saida ? $"Saída: {saida:dd/MM/yyyy HH:mm:ss}" : "Na academia · saída pendente";
    public string PermanenciaTexto => Saida is { } saida ? $"Permanência: {FormatarTempo(saida - Entrada)}" : "Visita em andamento";

    public static string FormatarTempo(TimeSpan tempo) => $"{(int)tempo.TotalHours:00}h {tempo.Minutes:00}min {tempo.Seconds:00}s";
}

public sealed record ResultadoAcessoDto(string Nome, string Mensagem);
