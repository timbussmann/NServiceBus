namespace NServiceBus.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;
    using Unicast;

    public class DelegateMessageHandlerTest : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_invoke_method_handler_delegate()
        {
            var cotnext = await Scenario.Define<Context>()
                .WithEndpoint<DelegateHandlerEndpoint>(e => e
                    .When(ctx => ctx.SendLocal(new DemoMessage()))
                    .When(ctx => ctx.Publish(new DemoEvent())))
                .Done(c => c.DelegateInvoked && c.MessageHandlerInvoked && c.EventDelegateInvoked)
                .Run(TimeSpan.FromSeconds(10));

            Assert.IsTrue(cotnext.EventDelegateInvoked);
            Assert.IsTrue(cotnext.DelegateInvoked);
            Assert.IsTrue(cotnext.MessageHandlerInvoked);
        }

        public class Context :ScenarioContext
        {
            public bool DelegateInvoked { get; set; }
            public bool MessageHandlerInvoked { get; set; }
            public bool EventDelegateInvoked { get; set; }
        }

        public class DelegateHandlerEndpoint : EndpointConfigurationBuilder
        {
            public DelegateHandlerEndpoint()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    var registy = new MessageHandlerRegistry();
                    registy.RegisterHandler<DemoMessage>(Guid.NewGuid().ToString(), typeof(DelegateMessageHandler), (_, message, context) =>
                    {
                        ((Context)r.ScenarioContext).DelegateInvoked = true;
                        return Task.FromResult(0);
                    });
                    registy.RegisterHandler<DemoEvent>(Guid.NewGuid().ToString(), typeof(DelegateMessageHandler), (_, @event, context) =>
                    {
                        ((Context)r.ScenarioContext).EventDelegateInvoked = true;
                        return Task.FromResult(0);
                    });
                    c.GetSettings().Set(registy);
                    // Register a dummy handler type as the pipeline tries to resolve it from the container
                    c.RegisterComponents(components => components.RegisterSingleton(new DelegateMessageHandler()));
                });
            }

            public class Handler : IHandleMessages<DemoMessage>
            {
                Context testContext;

                public Handler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(DemoMessage message, IMessageHandlerContext context)
                {
                    testContext.MessageHandlerInvoked = true;
                    return Task.FromResult(0);
                }
            }
        }

        class DelegateMessageHandler
        {
        }

        public class DemoMessage : ICommand
        {
        }

        public class DemoEvent : IEvent
        {
        }
    }
}