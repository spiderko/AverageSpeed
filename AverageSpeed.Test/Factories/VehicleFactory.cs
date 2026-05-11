using System;
using System.Text;
using AverageSpeed.Domain;

namespace AverageSpeed.Test.Factories
{
    public class VehicleFactory : IVehicleFactory
    {
        public Vehicle CreateRandom()
        {
            var random = new Random();
            var randomNumber = random.Next(0, 5);

            if (randomNumber < 4)
            {
                return new Car(RandomString(7));
            }

            var weight = random.Next(2, 9);
            return new Lorry(RandomString(7), weight);
        }

        private string RandomString(int size)
        {
            var builder = new StringBuilder();
            var random = new Random();
            for (var i = 0; i < size; i++)
            {
                var ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }
    }
}