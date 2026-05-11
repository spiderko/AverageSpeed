namespace AverageSpeed.Domain
{
    using System;
    using System.Collections.Generic;

    public class Road : IEntity
    {
        public Road(string id, int? speedLimitMph, bool dualCarriageway, int tolerancePercentage, List<Camera> cameras)
        {
            Id = id;
            SpeedLimitMph = speedLimitMph;
            TolerancePercentage = tolerancePercentage;
            DualCarriageway = dualCarriageway;
            Cameras = cameras;
        }

        /// <summary>
        /// If null, national speed limit is applied.
        /// </summary>
        public int? SpeedLimitMph { get; }

        public bool DualCarriageway { get; }

        public int TolerancePercentage { get; }

        public List<Camera> Cameras { get; }

        public string Id { get; }

        public Guid ETag { get; set; }
    }
}
