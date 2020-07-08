using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using Scheduling.Domain.Infrastructure.EventStore;

namespace Scheduling.Domain.Infrastructure.Projections
{
    public class SubscriptionManager
    {
        readonly ICheckpointStore _checkpointStore;
        readonly string _name;
        readonly StreamName _streamName;
        readonly EventStoreClient _client;
        readonly ISubscription[] _subscriptions;
        StreamSubscription _subscription;
        bool _isAllStream;

        public SubscriptionManager(
            EventStoreClient client,
            ICheckpointStore checkpointStore,
            string name,
            StreamName streamName,
            params ISubscription[] subscriptions
        )
        {
            _client = client;
            _checkpointStore = checkpointStore;
            _name = name;
            _streamName = streamName;
            _subscriptions = subscriptions;
            _isAllStream = streamName.IsAllStream;
        }

        public async Task Start()
        {
            var position = await _checkpointStore.GetCheckpoint();

            _subscription = _isAllStream
                ? (StreamSubscription)
                await _client.SubscribeToAllAsync(
                    GetAllStreamPosition(),
                    EventAppeared,
                    subscriptionDropped: SubscriptionDropped)
                : await _client.SubscribeToStreamAsync(
                    _streamName,
                    GetStreamPosition(),
                    EventAppeared
                );

            Position GetAllStreamPosition()
                => position.HasValue
                    ? new Position(position.Value, position.Value)
                    : Position.Start;

            StreamPosition GetStreamPosition()
                => position ?? StreamPosition.Start;
        }

        private void SubscriptionDropped(StreamSubscription _, SubscriptionDroppedReason reason, Exception? c)
        {
            Console.WriteLine(c.Message);
        }

        async Task EventAppeared(StreamSubscription _, ResolvedEvent resolvedEvent, CancellationToken c)
        {
            if (resolvedEvent.Event.EventType.StartsWith("$") ||
                resolvedEvent.Event.EventStreamId.Contains("async_command_handler")) return;

            var @event = resolvedEvent.Deserialize();

            await Task.WhenAll(
                _subscriptions.Select(x => x.Project(@event))
            );

            await _checkpointStore.StoreCheckpoint(
                // ReSharper disable once PossibleInvalidOperationException
                _isAllStream
                    ? resolvedEvent.OriginalPosition.Value.CommitPosition
                    : resolvedEvent.Event.EventNumber.ToUInt64()
            );
        }

        public void Stop() => _subscription.Dispose();
    }
}
