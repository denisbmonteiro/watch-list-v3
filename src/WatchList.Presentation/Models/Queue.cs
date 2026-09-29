using System.Diagnostics.CodeAnalysis;

namespace WatchList.Presentation.Models;

[SuppressMessage("Naming", "CA1711", Justification = "Substituído por WatchList.Domain.Tracking.QueueEntry; este modelo sai na fase 4 (docs/arquitetura-camadas.md).")]
public class Queue
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}