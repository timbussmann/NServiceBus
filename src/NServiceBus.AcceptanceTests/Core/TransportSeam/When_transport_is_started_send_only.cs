namespace NServiceBus.AcceptanceTests.Core.TransportSeam;

using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NServiceBus.Transport;
using NUnit.Framework;

public class When_transport_is_started_send_only : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_provide_access_to_address_resolution()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>()
            .Done(c => c.EndpointsStarted)
            .Run();

        var endpointName = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Endpoint));

        Assert.Multiple(() =>
        {
            Assert.That(context.ResolvedAddress, Is.EqualTo("SomeAddress"));
            Assert.That(context.LocalQueueAddress.ToString(), Is.EqualTo(endpointName));
            Assert.That(context.InstanceSpecificQueueAddress, Is.Null);
        });
    }

    class Context : ScenarioContext
    {
        public string ResolvedAddress { get; set; }
        public QueueAddress LocalQueueAddress { get; set; }
        public QueueAddress InstanceSpecificQueueAddress { get; set; }
    }

    class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.SendOnly();
                c.EnableFeature<FeatureAccessingAddressing>();
            });
        }

        class FeatureAccessingAddressing : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                var testContext = (Context)context.Settings.Get<ScenarioContext>();

                testContext.LocalQueueAddress = context.LocalQueueAddress();
                testContext.InstanceSpecificQueueAddress = context.InstanceSpecificQueueAddress();

                context.RegisterStartupTask(s => new StartupTask(testContext,
                    (ITransportAddressResolver)s.GetService(typeof(ITransportAddressResolver))));
            }
        }

        class StartupTask : FeatureStartupTask
        {
            readonly Context testContext;
            readonly ITransportAddressResolver resolver;

            public StartupTask(Context testContext, ITransportAddressResolver resolver)
            {
                this.testContext = testContext;
                this.resolver = resolver;
            }
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                testContext.ResolvedAddress = resolver.ToTransportAddress(new QueueAddress("SomeAddress"));
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

        }
    }
}