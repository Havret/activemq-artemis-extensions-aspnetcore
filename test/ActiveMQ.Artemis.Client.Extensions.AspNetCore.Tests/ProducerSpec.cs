using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ActiveMQ.Artemis.Client.Extensions.AspNetCore.Tests
{
    public class ProducerSpec
    {
        [Fact]
        public async Task Should_register_producer()
        {
            var address1 = Guid.NewGuid().ToString();

            await using var testFixture = await TestFixture.CreateAsync(activeMqBuilder =>
            {
                activeMqBuilder.AddProducer<TestProducer>(address1, RoutingType.Anycast);
            });

            var testProducer = testFixture.Services.GetRequiredService<TestProducer>();

            Assert.NotNull(testProducer);
        }

        [Fact]
        public async Task Should_send_message_using_registered_producer()
        {
            var address1 = Guid.NewGuid().ToString();

            await using var testFixture = await TestFixture.CreateAsync(activeMqBuilder =>
            {
                activeMqBuilder.AddProducer<TestProducer>(address1, RoutingType.Anycast);
            });

            var consumer = await testFixture.Connection.CreateConsumerAsync(address1, RoutingType.Anycast, testFixture.CancellationToken);

            var testProducer = testFixture.Services.GetRequiredService<TestProducer>();
            await testProducer.SendMessage("foo", testFixture.CancellationToken);

            var msg = await consumer.ReceiveAsync(testFixture.CancellationToken);
            Assert.Equal("foo", msg.GetBody<string>());
        }

        [Fact]
        public async Task Throws_when_producer_with_the_same_type_registered_twice()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => TestFixture.CreateAsync(activeMqBuilder =>
            {
                var address1 = Guid.NewGuid().ToString();
                activeMqBuilder.AddProducer<TestProducer>(address1, RoutingType.Anycast);
                activeMqBuilder.AddProducer<TestProducer>(address1, RoutingType.Anycast);
            }));

            Assert.Contains($"There has already been registered Producer with the type '{typeof(TestProducer).FullName}'", exception.Message);
        }

        [Fact]
        public async Task Should_register_configured_producer()
        {
            var address1 = Guid.NewGuid().ToString();

            await using var testFixture = await TestFixture.CreateAsync(activeMqBuilder =>
            {
                activeMqBuilder.AddProducer<TestProducer>(address1, RoutingType.Anycast, new ProducerOptions
                {
                    MessagePriority = 9
                });
            });

            var consumer = await testFixture.Connection.CreateConsumerAsync(address1, RoutingType.Anycast, testFixture.CancellationToken);

            var testProducer = testFixture.Services.GetRequiredService<TestProducer>();
            await testProducer.SendMessage("foo", testFixture.CancellationToken);

            var msg = await consumer.ReceiveAsync(testFixture.CancellationToken);
            Assert.Equal((byte?)9, msg.Priority);
        }

        private class TestProducer
        {
            private readonly IProducer _producer;

            public TestProducer(IProducer producer) => _producer = producer;
            public Task SendMessage(string text, CancellationToken cancellationToken) => _producer.SendAsync(new Message(text), cancellationToken);
        }

        [Fact]
        public async Task Should_register_multiple_producers()
        {
            var address1 = Guid.NewGuid().ToString();
            var address2 = Guid.NewGuid().ToString();

            await using var testFixture = await TestFixture.CreateAsync(activeMqBuilder =>
            {
                activeMqBuilder.AddProducer<TestProducer1>(address1, RoutingType.Anycast);
                activeMqBuilder.AddProducer<TestProducer2>(address2, RoutingType.Anycast);
            });

            var testProducer1 = testFixture.Services.GetRequiredService<TestProducer1>();
            var testProducer2 = testFixture.Services.GetRequiredService<TestProducer2>();

            Assert.NotEqual(testProducer1.Producer, testProducer2.Producer);
        }

        private class TestProducer1
        {
            public IProducer Producer { get; }
            public TestProducer1(IProducer producer) => Producer = producer;
        }

        private class TestProducer2
        {
            public IProducer Producer { get; }
            public TestProducer2(IProducer producer) => Producer = producer;
        }
    }
}