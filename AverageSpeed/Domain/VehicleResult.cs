namespace AverageSpeed.Domain
{
    using Messaging;

    public class VehicleResult : IMessage
    {
        public string Registration { get; set; }

        public int MaximumSpeed { get; set; }

        public bool ExceededLimit { get; set; }

        public bool? VehicleCheckPassed { get; set; }
    }
}
