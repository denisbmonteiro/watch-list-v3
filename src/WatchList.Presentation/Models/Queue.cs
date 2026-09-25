using System.Diagnostics.CodeAnalysis;

namespace WatchList.Presentation.Models;

[SuppressMessage("Naming", "CA1711", Justification = "Renomeado para QueueEntry na fase 1 (docs/arquitetura-camadas.md).")]
public class Queue
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}