namespace AverageSpeed.Test.Factories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AverageSpeed.Domain;
    using Domain;

    public class CaptureEventFactory : ICaptureEventFactory
    {
        private readonly IVehicleFactory _vehicleFactory;

        public CaptureEventFactory(IVehicleFactory vehicleFactory)
        {
            this._vehicleFactory = vehicleFactory;
        }

        public (List<CaptureEvent>, List<CaptureEventResult>) Create(int numberOfVehicles, int percentageSpeeding, Road road)
        {
            var vehicles = Enumerable.Range(0, numberOfVehicles).Select(x => _vehicleFactory.CreateRandom()).ToList();
            var quantitySpeeding = Math.Round(numberOfVehicles / 100d * percentageSpeeding);

            var eventResults = new List<CaptureEventResult>();
            var vehicleEvents = vehicles.SelectMany((v, i) =>
            {
                // Drive the route, to get times at the cameras
                var random = new Random();
                var vehicleLimit = road.SpeedLimitMph ?? (road.DualCarriageway
                    ? v.NationalDualCarriagewayLimit
                    : v.NationalSingleCarriagewayLimit);
                var vehicleLimitWithTolerance = (int)Math.Round(vehicleLimit * (1 + (road.TolerancePercentage / 100d)));
                var targetSpeed = i < quantitySpeeding
                    ? vehicleLimitWithTolerance + random.Next(1, 30)
                    : vehicleLimitWithTolerance - random.Next(1, 20);

                var eventTime = DateTime.Parse("2020-01-10 08:00");
                eventTime = eventTime.AddMinutes(random.Next(0, 300));

                var eventResult = new CaptureEventResult(v.Registration, v.GetType().Name);
                var vehicleEvents = new List<CaptureEvent>();
                foreach (var camera in road.Cameras)
                {
                    var cameraSpeed = (int)Math.Round(targetSpeed * (1 + random.Next(-30, 30) / 100d));
                    var distance = camera.MilesFromPrevious;
                    var distanceSecond = cameraSpeed / 60d / 60d;
                    var seconds = distance / distanceSecond;
                    eventTime = eventTime.AddSeconds(seconds);

                    cameraSpeed = (int)camera.MilesFromPrevious == 0 ? 0 : (int)Math.Round(camera.MilesFromPrevious / TimeSpan.FromSeconds(seconds).TotalHours);
                    vehicleEvents.Add(new CaptureEvent(road.Id, camera.Position, v, eventTime));

                    eventResult.CameraSpeed.Add((cameraSpeed, cameraSpeed >= vehicleLimitWithTolerance, eventTime));
                }

                eventResults.Add(eventResult);
                return vehicleEvents;
            }).OrderBy(x=> x.EventTime).ToList();

            return (vehicleEvents, eventResults);
        }
    }
}