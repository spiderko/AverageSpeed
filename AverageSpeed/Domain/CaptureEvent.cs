namespace AverageSpeed.Domain
{
    using System;

    using Messaging;

    public class CaptureEvent : IMessage
    {
        public CaptureEvent(string roadId, int cameraPosition, Vehicle vehicle, DateTime eventTime)
        {
            RoadId = roadId;
            CameraPosition = cameraPosition;
            Vehicle = vehicle;
            EventTime = eventTime;
        }

        public string RoadId { get; }

        public int CameraPosition { get; }

        public Vehicle Vehicle { get; }

        public DateTime EventTime { get; }
    }
}
