using Observito.Trace.EventSourceFlightRecorder.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Observito.Trace.EventSourceFlightRecorder
{
    /// <summary>
    /// Records <see cref="EventSource"/> messages to an internal buffer.
    /// Useful for logging last messages before an exception occurred.
    /// </summary>
    public sealed class EventSourceFlightRecorder<TEvent> : IDisposable
    {
        /// <summary>
        /// Flight recorder settings. Determines what events to record and how to capture them.
        /// </summary>
        public sealed class Settings
        {
            /// <summary>
            /// Creates a new instance.
            /// </summary>
            /// <param name="level">Minimum event level</param>
            public Settings(EventLevel level, Func<EventWrittenEventArgs, TEvent> selector)
            {
                Level = level;
                Selector = selector ?? throw new ArgumentNullException(nameof(selector));
            }

            /// <summary>
            /// Minimum event level to log.
            /// </summary>
            public EventLevel Level { get; }

            /// <summary>
            /// Optional filter to include or exclude events.
            /// </summary>
            public Predicate<EventWrittenEventArgs> Filter { get; set; }

            /// <summary>
            /// Selector transform events into their recorded form.
            /// </summary>
            public Func<EventWrittenEventArgs, TEvent> Selector { get; }

            /// <summary>
            /// Does the current filer accept the event data? No filter means all events are accepted.
            /// </summary>
            /// <param name="args">Event to test</param>
            /// <returns>True if accepted, false otherwise</returns>
            public bool Accepts(EventWrittenEventArgs args) => Filter == null || Filter(args);
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="capacity">Event buffer capacity</param>
        public EventSourceFlightRecorder(uint capacity)
        {
            _source = new Source();
            _settings = new Dictionary<string, Settings>(StringComparer.OrdinalIgnoreCase);
            _buffer = new RingBuffer<TEvent>(capacity);

            _source.EventWrittenImpl += OnEventWritten;
        }
        
        private readonly Source _source;
        private readonly Dictionary<string, Settings> _settings;
        private readonly RingBuffer<TEvent> _buffer;

        /// <summary>
        /// Enable event recording for an event source.
        /// </summary>
        /// <param name="log">Event source to record events from</param>
        /// <param name="level">Minimum event level to record</param>
        /// <exception cref="ArgumentNullException">If the event source is null</exception>
        public void EnableEvents(EventSource log, EventLevel level, Func<EventWrittenEventArgs, TEvent> selector)
        {
            if (log is null) throw new ArgumentNullException(nameof(log));
            if (selector is null) throw new ArgumentNullException(nameof(selector));

            var settings = new Settings(level, selector);
            EnableEvents(log, settings);
        }

        /// <summary>
        /// Enable event logging for an event source.
        /// </summary>
        /// <param name="log">Event source to record events from</param>
        /// <param name="level">Recording settings</param>
        /// <exception cref="ArgumentNullException">If any argument is null</exception>
        public void EnableEvents(EventSource log, Settings settings)
        {
            if (log is null) throw new ArgumentNullException(nameof(log));
            if (settings is null) throw new ArgumentNullException(nameof(settings));

            _settings[log.Name] = settings;
            _source.EnableEvents(log, settings.Level);
        }

        /// <summary>
        /// Disposes internal resources.
        /// </summary>
        public void Dispose()
        {
            _source.EventWrittenImpl -= OnEventWritten;
            _source.Dispose();
        }

        /// <summary>
        /// Gets a snapshot of the currently buffered events.
        /// </summary>
        public TEvent[] Snapshot => _buffer.ToArray();

        #region Implementation
        private void OnEventWritten(object sender, EventWrittenEventArgs @event)
        {
            if (_settings.TryGetValue(@event.EventSource.Name, out var settings) && settings.Accepts(@event))
            {
                TEvent result = default;
                try
                {
                    result = settings.Selector(@event);
                    _buffer.Put(result);
                }
                catch
                {
                    // TODO strategy
                }
            }
        }

        private class Source : EventListener
        {
            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                EventWrittenImpl?.Invoke(this, eventData);
            }

            // Strange name due to the fact that in .NET Core 3 (and std 2.1) this event actually exists, so in the future this event can be removed.
            public event EventHandler<EventWrittenEventArgs> EventWrittenImpl;
        }
        #endregion
    }
}
