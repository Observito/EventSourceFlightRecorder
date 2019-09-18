# EventSourceFlightRecorder

Continuous recording of in-process event sources. Useful for logging last events before an exception, e.g. unhandled top level exceptions.

## Sample

```csharp
using var recorder = new EventSourceFlightRecorder<string>(100);

recorder.EnableEvents(EchoEventSource.Log, EventLevel.Informational, ev => $"[{ev.TimeStamp:yyyy-MM-dd HH:mm:ss.ffff}] {ev.EventSource.Name}/{ev.EventName}/{ev.Opcode}: { EventFormatting.FormatMessage(ev)}");

// ... later ...

foreach (var ev in recorder.Snapshot)
    Console.WriteLine($"- {ev}");
