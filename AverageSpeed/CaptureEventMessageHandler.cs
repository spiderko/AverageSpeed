namespace AverageSpeed
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Domain;
    using Messaging;
    using Services;

    public class CaptureEventMessageHandler : IHandleMessage<CaptureEvent>
    {
        private readonly IRepository<Road> _roadRepository;
        private readonly IRepository<VehicleJourney> _journeyRepository;
        private readonly IVehicleChecker _vehicleChecker;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        public CaptureEventMessageHandler(
            IRepository<Road> roadRepository,
            IRepository<VehicleJourney> journeyRepository,
            IVehicleChecker vehicleChecker)
        {
            _roadRepository = roadRepository;
            _journeyRepository = journeyRepository;
            _vehicleChecker = vehicleChecker;
        }

        public async Task HandleEvent(CaptureEvent message, IMessageContext messageContext)
        {
            var road = await _roadRepository.GetById(message.RoadId).ConfigureAwait(false);

            var journeyId = $"{message.RoadId}:{message.Vehicle.Registration}";
            var journeyLock = _locks.GetOrAdd(journeyId, _ => new SemaphoreSlim(1, 1));

            VehicleJourney journey;
            bool journeyComplete;

            await journeyLock.WaitAsync().ConfigureAwait(false);
            try
            {
                journey = await _journeyRepository.GetById(journeyId).ConfigureAwait(false);
                if (journey == null)
                {
                    journey = new VehicleJourney(journeyId, message.RoadId, message.Vehicle);
                }

                journey.CaptureEvents.Add((message.CameraPosition, message.EventTime));
                journeyComplete = journey.CaptureEvents.Count >= road.Cameras.Count;

                if (journeyComplete)
                {
                    await _journeyRepository.Delete(journeyId).ConfigureAwait(false);
                }
                else
                {
                    await _journeyRepository.Upsert(journey).ConfigureAwait(false);
                }
            }
            finally
            {
                journeyLock.Release();
            }

            if (!journeyComplete)
            {
                return;
            }

            var events = journey.CaptureEvents
                .OrderBy(e => e.CameraPosition)
                .ToList();

            var maxSpeed = 0d;
            for (var i = 1; i < events.Count; i++)
            {
                var camera = road.Cameras.Single(c => c.Position == events[i].CameraPosition);
                if (camera.MilesFromPrevious == 0)
                    continue;

                var timeDiff = (events[i].EventTime - events[i - 1].EventTime).TotalHours;
                if (timeDiff <= 0)
                    continue;

                var speed = camera.MilesFromPrevious / timeDiff;
                if (speed > maxSpeed)
                    maxSpeed = speed;
            }

            var vehicle = journey.Vehicle;
            var speedLimit = road.SpeedLimitMph ?? (road.DualCarriageway
                ? vehicle.NationalDualCarriagewayLimit
                : vehicle.NationalSingleCarriagewayLimit);
            var limitWithTolerance = (int)Math.Round(speedLimit * (1 + road.TolerancePercentage / 100d));
            var roundedMaxSpeed = (int)Math.Round(maxSpeed);

            bool? vehicleCheckPassed = null;
            try
            {
                vehicleCheckPassed = await _vehicleChecker
                    .VehicleCheck(vehicle.Registration, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Vehicle check failed, leave as null
            }

            await messageContext.Send(new VehicleResult
            {
                Registration = vehicle.Registration,
                MaximumSpeed = roundedMaxSpeed,
                ExceededLimit = roundedMaxSpeed >= limitWithTolerance,
                VehicleCheckPassed = vehicleCheckPassed
            }).ConfigureAwait(false);
        }
    }
}
