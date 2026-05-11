namespace AverageSpeed.Messaging
{
    using System.Threading.Tasks;

    public interface IHandleMessage<T> where T : IMessage
    {
        Task HandleEvent(T message, IMessageContext messageContext);
    }
}