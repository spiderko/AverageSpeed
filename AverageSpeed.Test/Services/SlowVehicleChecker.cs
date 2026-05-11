namespace AverageSpeed.Test.Services
{
    using System.Threading;
    using System.Threading.Tasks;

    using AverageSpeed.Services;

    public class SlowVehicleChecker : BasicVehicleChecker, IVehicleChecker
    {
        public new async Task<bool?> VehicleCheck(string registration, CancellationToken token)
        {
            await Task.Delay(200, token);
            return await base.VehicleCheck(registration, token);
        }
    }
}