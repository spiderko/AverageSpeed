namespace AverageSpeed.Domain
{
    public abstract class Vehicle
    {
        protected Vehicle(string registration)
        {
            Registration = registration;
        }

        public string Registration { get; }

        public abstract int NationalSingleCarriagewayLimit { get; }
        public abstract int NationalDualCarriagewayLimit { get; }
    }
}