namespace WebApplication1
{
    using System.Threading.Tasks;
    using NServiceBus;

    public class MessageHandler : IHandleMessages<TestMessage>
    {
        readonly SomeDependency dependency;

        public MessageHandler(SomeDependency dependency)
        {
            this.dependency = dependency;
        }

        public Task Handle(TestMessage message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
        }
    }

    public class SomeDependency
    {

    }
}