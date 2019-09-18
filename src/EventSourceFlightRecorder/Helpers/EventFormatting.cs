using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;

namespace Observito.Trace.EventSourceFlightRecorder.Helpers
{
    public static class EventFormatting
    {
        /// <summary>
        /// Formats the message with event arguments.
        /// </summary>
        /// <param name="event">Event with message to format</param>
        /// <param name="cultureInfo">Culture info</param>
        /// <returns>Formatted message</returns>
        public static string FormatMessage(EventWrittenEventArgs @event, CultureInfo cultureInfo = null)
        {
            bool HasPayloadIndex(string rawMsg, int payloadIndex) => rawMsg.IndexOf($"{{{payloadIndex}}}", StringComparison.OrdinalIgnoreCase) != -1;

            var rawMessage = @event.Message;

            if (string.IsNullOrWhiteSpace(rawMessage) || rawMessage.IndexOf("{", StringComparison.OrdinalIgnoreCase) == -1)
                return rawMessage;

            var args = new List<object>();
            for (var i = 0; HasPayloadIndex(rawMessage, i); i++)
            {
                if (i < @event.PayloadNames.Count)
                    args.Add(@event.Payload[i]);
                else
                    args.Add(null);
            }

            if (cultureInfo == null)
                cultureInfo = CultureInfo.InvariantCulture;

            var msg = args.Count == 0
                ? rawMessage 
                : string.Format(cultureInfo, rawMessage, args.ToArray());

            return msg;
        }
    }
}
