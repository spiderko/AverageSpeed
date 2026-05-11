using AverageSpeed.Domain;

namespace AverageSpeed.Test.Factories
{
    public interface IRoadFactory
    {
        Road Create(string id, int? speedLimit, bool dualCarriageway, int tolerancePercentage, int cameraCount);
    }
}