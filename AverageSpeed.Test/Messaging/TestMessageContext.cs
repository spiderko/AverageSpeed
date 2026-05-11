namespace AverageSpeed.Test.Messaging
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using AverageSpeed.Messaging;

    public class TestMessageContext : IMessageContext
    {
        public TestMessageContext()
        {
            ReceivedMessages = new List<IMessage>();
        }

        public Task Send<T>(T message)
            where T : IMessage
        {
            ReceivedMessages.Add(message);
            return Task.CompletedTask;
        }

        public List<IMessage> ReceivedMessages { get; set; }
    }
}
