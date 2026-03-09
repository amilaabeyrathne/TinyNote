using System.Diagnostics.Metrics;

namespace TinyNote.Api.Metrics;

public sealed class TinyNoteMetrics : IDisposable
{
    public const string MeterName = "TinyNote.Api";

    private readonly Meter _meter;

    public Counter<long> NotesCreated { get; }
    public Counter<long> NotesDeleted { get; }
    public Counter<long> NotesUpdated { get; }

    public TinyNoteMetrics()
    {
        _meter = new Meter(MeterName, "1.0.0");

        NotesCreated = _meter.CreateCounter<long>(
            "tinynote.notes.created",
            unit: "{notes}",
            description: "Number of notes created");

        NotesDeleted = _meter.CreateCounter<long>(
            "tinynote.notes.deleted",
            unit: "{notes}",
            description: "Number of notes deleted");

        NotesUpdated = _meter.CreateCounter<long>(
            "tinynote.notes.updated",
            unit: "{notes}",
            description: "Number of notes updated");
       
    }

    public void Dispose() => _meter.Dispose();
}
