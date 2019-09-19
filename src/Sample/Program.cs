using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Observito.Trace.EventSourceFlightRecorder;
using Observito.Trace.EventSourceFormatter;

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

            string Format(EventWrittenEventArgs ev)
            {
                return EventSourceFormatter.Format(ev, includePayload: true /*, selector: ...*/); // optionally use selector to scrub sensitive data
            }

            recorder.EnableEvents(EchoEventSource.Log, EventLevel.Informational, Format);

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
