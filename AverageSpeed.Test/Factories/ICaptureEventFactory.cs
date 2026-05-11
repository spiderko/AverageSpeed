using System.Collections.Generic;
using AverageSpeed.Domain;
using AverageSpeed.Test.Domain;

namespace AverageSpeed.Test.Factories
{
    public interface ICaptureEventFactory
    {
        (List<CaptureEvent> Events, List<CaptureEventResult> Results) Create(int numberOfVehicles, int percentageSpeeding, Road road);
    }
}