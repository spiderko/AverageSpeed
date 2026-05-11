namespace AverageSpeed.Test.Factories
{
    using System;
    using System.Linq;

    using AverageSpeed.Domain;

    public class RoadFactory : IRoadFactory
    {
        public Road Create(string id, int? speedLimit, bool dualCarriageway, int tolerancePercentage, int cameraCount)
        {
            var random = new Random();
            var cameras = Enumerable.Range(0, cameraCount).Select(i => new Camera(i, i == 0 ? 0 : random.Next(200, 400) / 100d)).ToList();
            return new Road(id, speedLimit, dualCarriageway, tolerancePercentage, cameras);
        }
    }
}
