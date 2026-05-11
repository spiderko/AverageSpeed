using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AverageSpeed.Services;

namespace AverageSpeed.Test.Services
{
    using System.Threading;

    public class BasicVehicleChecker : IVehicleChecker
    {
        public Task<bool?> VehicleCheck(string registration, CancellationToken token)
        {
            using var algorithm = SHA256.Create();
            var hashedBytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(registration));
            var firstChar = hashedBytes[0].ToString("X2");
            return Task.FromResult<bool?>(firstChar[0] == 'a');
        }
    }
}