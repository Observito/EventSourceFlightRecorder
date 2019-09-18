using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Observito.Trace.EventSourceFlightRecorder;
using Observito.Trace.EventSourceFlightRecorder.Helpers;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                var c = 0;
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                    EchoEventSource.Log.Echo($"Testing {c}");
                    c++;
                }
            });

            using var recorder = new EventSourceFlightRecorder<string>(10);

            string ToString(EventWrittenEventArgs ev)
            {
                var msg = EventFormatting.FormatMessage(ev);
                return $"[{ev.TimeStamp:yyyy-MM-dd HH:mm:ss.ffff}] {ev.EventSource.Name}/{ev.EventName}/{ev.Opcode}: {msg}";
            }

            recorder.EnableEvents(EchoEventSource.Log, EventLevel.Informational, ToString);

            Console.WriteLine("Press enter to dump snapshot of collected events...");
            Console.ReadLine();

            cts.Cancel();

            Console.WriteLine();
            Console.WriteLine("Snapshot of events:");
            Console.WriteLine();
            foreach (var ev in recorder.Snapshot)
                Console.WriteLine($"- {ev}");
            Console.WriteLine();

            Console.WriteLine("Press enter to stop sample...");
            Console.ReadLine();
        }
    }
}
