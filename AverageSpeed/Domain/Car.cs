namespace AverageSpeed.Domain
{
    public class Car : Vehicle
    {
        public Car(string registration) : base(registration)
        {
        }

        public override int NationalSingleCarriagewayLimit => 60;

        public override int NationalDualCarriagewayLimit => 70;
    }
}