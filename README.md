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