namespace AverageSpeed.Test.Services
{
    using System.Threading;
    using System.Threading.Tasks;

    using AverageSpeed.Services;

    public class ThrottledVehicleChecker : BasicVehicleChecker, IVehicleChecker
    {
        private int _inFlight = 0;

        public new async Task<bool?> VehicleCheck(string registration, CancellationToken token)
        {
            if (_inFlight >= 5)
            {
                await Task.Delay(1500, token);
                return null;
            }

            _inFlight++;
            await Task.Delay(200, token);
            _inFlight--;

            return await base.VehicleCheck(registration, token);
        }
    }
}
