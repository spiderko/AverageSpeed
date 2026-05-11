namespace AverageSpeed.Domain
{
    public class Camera
    {
        public Camera(int position, double milesFromPrevious)
        {
            Position = position;
            MilesFromPrevious = milesFromPrevious;
        }

        public int Position { get; }

        public double MilesFromPrevious { get; }
    }
}