namespace AverageSpeed.Domain
{
    using System;
    using System.Collections.Generic;

    public class VehicleJourney : IEntity
    {
        public VehicleJourney(string id, string roadId, Vehicle vehicle)
        {
            Id = id;
            RoadId = roadId;
            Vehicle = vehicle;
            CaptureEvents = new List<(int CameraPosition, DateTime EventTime)>();
        }

        public string Id { get; }

        public string RoadId { get; }

        public Vehicle Vehicle { get; }

        public List<(int CameraPosition, DateTime EventTime)> CaptureEvents { get; }

        public Guid ETag { get; set; }
    }
}
