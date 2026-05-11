namespace AverageSpeed
{
    using System.Linq;
    using System.Threading.Tasks;

    using Domain;
    using Messaging;

    public class CaptureEventMessageHandler : IHandleMessage<CaptureEvent>
    {
        private readonly IRepository<Road> _roadRepository;

        public CaptureEventMessageHandler(IRepository<Road> roadRepository)
        {
            _roadRepository = roadRepository;
        }

        public async Task HandleEvent(CaptureEvent message, IMessageContext messageContext)
        {
            // Example retrieving road and event camera from repository
            var road = await _roadRepository.GetById(message.RoadId).ConfigureAwait(false);
            var eventCamera = road.Cameras.Single(c => c.Position == message.CameraPosition);
        }
    }
}
