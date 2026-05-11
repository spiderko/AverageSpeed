using System.Threading.Tasks;

namespace AverageSpeed.Services
{
    using System.Threading;

    public interface IVehicleChecker
    {
        public Task<bool?> VehicleCheck(string registration, CancellationToken token);
    }
}
