namespace AverageSpeed.Domain
{
    public class Lorry : Vehicle
    {
        public Lorry(string registration, double weightTonnes) : base(registration)
        {
            WeightTonnes = weightTonnes;
        }

        public double WeightTonnes { get; }

        public override int NationalSingleCarriagewayLimit => WeightTonnes > 7.5 ? 40 : 50;

        public override int NationalDualCarriagewayLimit => WeightTonnes > 7.5 ? 50 : 60;
    }
}