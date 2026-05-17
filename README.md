# Solirius Average Speed Coding Test
## Background
Your solution should handle events from speed cameras.
As a vehicle travels along a stretch of road with an average speed check, a `CaptureEvent` is triggered at each camera.
Once the vehicle has passed all cameras on the stretch of road a `VehicleResult` should be returned on the `messageContext`.

``` c#
public class VehicleResult : IMessage
{
    public string Registration { get; set; }

    public int MaximumSpeed { get; set; }

    public bool ExceededLimit { get; set; }

    public bool? VehicleCheckPassed { get; set; }
}
```

The MaximumSpeed should be the fastest speed the vehicle was traveling between any 2 consective cameras.
The vehicle speed will need to be calculated using the `EventTime` from the `CaptureEvent` and the `MilesFromPrevious` from the camera past.

The entry point is the handler for `CaptureEvent` in the `CaptureEventMessageHandler`

```c#
public Task HandleEvent(CaptureEvent message, IMessageContext messageContext)
{
    return messageContext.Send(new VehicleResult());
}
```

Autofac has been used for IoC container. A module is available in the test project `AutofacModule` where any additional registrations can be added if required.

A generic repository has been implimented to store data between requests inject `IRepository<T> where T : TEntity`. An `IRepository<Road>` has already been injected into the handler as an example.

## Testing
Tests for the problem have already been written allowing you to follow a TDD approach.

## Stretch Goal
Message ordering, if this service was to be scaled, we couldnt guarentee the order the messages. The solution should allow for events to come in unordered.

Each vehicle should be checked using Vehicle Checker `IVehicleChecker`
Chekcer will only process 5 requests concurrently and take 200ms to respond. Ensure the messages are handled in a timely manner despite this limitation.

## Questions
What underlying technologies would you recommend for the repository and messaging and why?

## Answers

### Repository: Azure Cosmos DB

The `IRepository<T>` interface in this project exposes a simple key-value contract — `GetById`, `Upsert`, and `Delete` — with an `ETag` on every entity for optimistic concurrency control. This shaped the technology choice significantly.

**Why Cosmos DB:**

- **Native ETag / optimistic concurrency** — The test `Repository` already throws on ETag mismatch during `Upsert`. Cosmos DB supports this natively: every document carries an `_etag`, and you can pass an `If-Match` header to reject stale writes. This maps directly to our `IEntity.ETag` pattern without any additional locking logic in the database layer.
- **Key-value access pattern** — Every repository call is a single-document lookup by `Id`. We never query by secondary fields or run joins. Cosmos DB is optimised for exactly this: point-reads by partition key are single-digit-millisecond operations with predictable cost (1 RU).
- **Partition key aligns with journey isolation** — `VehicleJourney.Id` is `"{roadId}:{registration}"`. Using this as the partition key means all operations for a given journey hit a single logical partition, so there are no cross-partition queries and no hot-partition concerns as traffic scales across many vehicles.
- **TTL for automatic cleanup** — Journeys are deleted after processing, but if a vehicle never passes the final camera (e.g. it exits the road early), orphaned documents would accumulate. Cosmos DB's per-document TTL can expire these automatically without application-side garbage collection.
- **Serverless tier for cost** — Average speed systems have bursty traffic (rush hour vs. overnight). Cosmos DB's serverless mode charges per-request rather than provisioned throughput, keeping costs proportional to actual camera events.

**Why not a relational database (SQL Server / PostgreSQL)?** — There are no relational queries, no joins, no transactions spanning multiple entities. A relational database would work but adds overhead (connection pooling, ORM mapping, schema migrations) for a data model that is fundamentally a document store. The ETag-based concurrency is also more natural in a document database than managing row versions or `SELECT ... FOR UPDATE` in SQL.

**Why not Redis?** — Redis would deliver even lower latency, but it is primarily an in-memory store. Journey data is transient but must survive process restarts — if the service crashes between the first and last camera event, in-flight journeys should not be lost. Cosmos DB persists to disk with replication. Redis persistence (AOF/RDB) is possible but adds operational complexity and is not its primary strength.

### Messaging: Azure Service Bus with session-enabled queues

The `IHandleMessage<CaptureEvent>` / `IMessageContext` abstraction implies an event-driven, message-based architecture. The key constraints from the problem are:

1. **Capture events for the same vehicle on the same road must be processed sequentially** (or at least safely aggregated) — our solution uses per-journey locking via `ConcurrentDictionary<string, SemaphoreSlim>`.
2. **Events may arrive out of order** — the stretch goal explicitly states this.
3. **The system should scale horizontally** — multiple handler instances processing events concurrently.

**Why Azure Service Bus:**

- **Sessions for ordered, affinity-based processing** — Service Bus sessions allow you to group messages by a session ID (in our case, `"{roadId}:{registration}"`). All messages with the same session ID are delivered to the same consumer in FIFO order. This eliminates the need for in-process locking entirely — if we use sessions, the `ConcurrentDictionary<SemaphoreSlim>` becomes unnecessary because the broker guarantees that only one consumer processes a given journey's events at a time. This is a cleaner architectural solution than application-level locks, which break down when running multiple service instances.
- **At-least-once delivery with peek-lock** — Camera events must not be lost. If a handler crashes mid-processing, the message returns to the queue after the lock timeout expires and gets reprocessed. Combined with the `ETag` concurrency on the repository, this gives us safe retry semantics without duplicating results.
- **Dead-letter queue for poison messages** — If a capture event consistently fails processing (e.g. corrupted data, unknown road ID), it moves to the dead-letter queue after a configured number of attempts rather than blocking the queue. This is critical for a system where we cannot afford to halt processing for all vehicles because of one bad event.
- **Scalability without coordination** — With sessions enabled, adding more consumer instances automatically distributes sessions (journeys) across them. No need for an external load balancer or distributed lock manager. Each instance "owns" its sessions and processes them independently.

**Why not RabbitMQ?** — RabbitMQ is a strong general-purpose broker, but it lacks native session affinity. To achieve the same per-vehicle ordering guarantee, you would need to either implement consistent-hash exchanges (which only provide partition-level affinity, not message-level ordering within a partition) or manage consumer affinity externally. Service Bus sessions provide this out of the box with less operational overhead.

**Why not Kafka?** — Kafka provides ordering within partitions and is excellent for high-throughput event streaming. However, it introduces more operational complexity (managing partitions, consumer groups, offsets) for what is fundamentally a task-processing pattern rather than an event-streaming pattern. The number of concurrent journeys is dynamic — partitions would need to be over-provisioned or rebalanced. Service Bus sessions handle dynamic affinity more naturally. Kafka would be a better fit if we also needed to replay historical events or feed the data into analytics pipelines.

### Why Azure-native?

Both recommendations are Azure services because they integrate naturally — Cosmos DB change feed can publish to Service Bus, both use Azure AD for authentication, and both work seamlessly with Azure Functions or App Service for hosting. For an AWS deployment, the equivalent choices would be DynamoDB (with its conditional writes mapping to our ETag pattern) and SQS with message group IDs (which provide similar session-like ordering guarantees).