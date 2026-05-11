namespace AverageSpeed.Test.Domain
{
    using System;
    using System.Collections.Generic;

    public class CaptureEventResult
    {
        public CaptureEventResult(string registration, string vehicleType)
        {
            Registration = registration;
            VehicleType = vehicleType;
            CameraSpeed = new List<(int Speed, bool Speeding, DateTime Time)>();
        }

        public string Registration { get; set; }

        public string VehicleType { get; set; }

        public List<(int Speed, bool Speeding, DateTime Time)> CameraSpeed { get; set; }
    }
}
