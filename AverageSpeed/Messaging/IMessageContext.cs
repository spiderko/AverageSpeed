namespace AverageSpeed.Messaging
{
    using System.Threading.Tasks;

    public interface IMessageContext
    {
        Task Send<T>(T message)
            where T : IMessage;
    }
}