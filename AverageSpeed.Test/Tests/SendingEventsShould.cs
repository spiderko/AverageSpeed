namespace AverageSpeed.Test.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    using Autofac;

    using AverageSpeed.Domain;
    using AverageSpeed.Services;
    using Factories;
    using Messaging;
    using Services;

    using FluentAssertions;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SendingEventsShould
    {
        private CaptureEventMessageHandler _sut;

        private IContainer _container;

        private IVehicleChecker _basicVehicleChecker;

        private TestMessageContext _messageContext;

        private IRepository<Road> _roadRepository;

        public void SetupSut<TChecker>() where TChecker : IVehicleChecker
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<AverageSpeed.AutofacModule>();
            builder.RegisterModule<AutofacModule>();

            builder.RegisterType<TChecker>().As<IVehicleChecker>();
            _container = builder.Build();

            _basicVehicleChecker = _container.Resolve<BasicVehicleChecker>();
            _sut = _container.Resolve<CaptureEventMessageHandler>();
            _messageContext = _container.Resolve<TestMessageContext>();
            _roadRepository = _container.Resolve<IRepository<Road>>();
        }

        [DataTestMethod]
        // Basic Speed 30
        [DataRow(30, false, 0, 5, 100)]
        [DataRow(30, false, 1, 5, 100)]
        [DataRow(30, false, 1, 5, 0)]
        [DataRow(30, false, 10, 5, 50)]
        [DataRow(30, false, 10, 10, 50)]

        // National Speed Single Carriageway
        [DataRow(null, false, 0, 5, 100)]
        [DataRow(null, false, 1, 5, 100)]
        [DataRow(null, false, 1, 5, 0)]
        [DataRow(null, false, 10, 5, 50)]
        [DataRow(null, false, 10, 10, 50)]

        // National Speed Single Carriageway
        [DataRow(null, true, 0, 5, 100)]
        [DataRow(null, true, 1, 5, 100)]
        [DataRow(null, true, 1, 5, 0)]
        [DataRow(null, true, 10, 5, 50)]
        [DataRow(null, true, 10, 10, 50)]

        // Higher Volume
        // [DataRow(30, false, 5000, 10, 1)]
        public Task HandleEventsBasicChecker(
            int? limit,
            bool dualCarriageway,
            int vehicles,
            int cameras,
            int percentageSpeeding)
        {
            return HandleEventsChecker<BasicVehicleChecker>(limit, dualCarriageway, vehicles, cameras, percentageSpeeding, 1);
        }

        [DataTestMethod]
        // Basic Speed 30
        [DataRow(30, false, 0, 5, 100)]
        [DataRow(30, false, 1, 5, 100)]
        [DataRow(30, false, 1, 5, 0)]
        [DataRow(30, false, 10, 5, 50)]
        [DataRow(30, false, 10, 10, 50)]
        public Task Stretch_HandleEventsSlowChecker(
            int? limit,
            bool dualCarriageway,
            int vehicles,
            int cameras,
            int percentageSpeeding)
        {
            return HandleEventsChecker<SlowVehicleChecker>(limit, dualCarriageway, vehicles, cameras, percentageSpeeding, 1);
        }

        [DataTestMethod]
        // Basic Speed 30
        [DataRow(30, false, 0, 5, 100)]
        [DataRow(30, false, 1, 5, 100)]
        [DataRow(30, false, 1, 5, 0)]
        [DataRow(30, false, 10, 5, 50)]
        [DataRow(30, false, 10, 10, 50)]
        public Task Stretch_HandleEventsThrottledChecker(
            int? limit,
            bool dualCarriageway,
            int vehicles,
            int cameras,
            int percentageSpeeding)
        {
            return HandleEventsChecker<ThrottledVehicleChecker>(limit, dualCarriageway, vehicles, cameras, percentageSpeeding, 1);
        }

        [DataTestMethod, DataRow(30, false, 10, 20, 50)]
        public Task Stretch_HandleEventsScaled(
            int? limit,
            bool dualCarriageway,
            int vehicles,
            int cameras,
            int percentageSpeeding)
        {
            return HandleEventsChecker<BasicVehicleChecker>(limit, dualCarriageway, vehicles, cameras, percentageSpeeding, 10);
        }

        private async Task HandleEventsChecker<TVehicleChecker>(int? limit, bool dualCarriageway, int vehicles, int cameras, int percentageSpeeding, int maxDegreeOfParallelism) where TVehicleChecker : IVehicleChecker
        {
            // Arrange
            SetupSut<TVehicleChecker>();

            var roadFactory = _container.Resolve<IRoadFactory>();
            var captureEventFactory = _container.Resolve<ICaptureEventFactory>();
            var road = roadFactory.Create("ARoadName:North", limit, dualCarriageway, 20, cameras);
            await _roadRepository.Upsert(road);

            var (events, results) = captureEventFactory.Create(vehicles, percentageSpeeding, road);
            results.Count.Should().Be(vehicles, $"Factory has generated wrong number of events. {nameof(vehicles)}");
            events.Count.Should().Be(vehicles * cameras, $"Factory has generated wrong number of events. Total");

            // Act
            var stopwatch = Stopwatch.StartNew();
            var vehiclesCheck = 0;

            // TODO: Could time each message individually
            var bufferBlock = new BufferBlock<CaptureEvent>();
            var senderBlock = new ActionBlock<CaptureEvent>(
                async captureEvent =>
                    {
                        for (var retryCount = 0; retryCount < 5; retryCount++)
                        {
                            try
                            {
                                await _sut.HandleEvent(captureEvent, _messageContext).ConfigureAwait(false);
                                break;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Message failed: {ex.Message}. Message will be retried. {captureEvent.Vehicle.Registration}:{captureEvent.CameraPosition}");
                                await Task.Delay(retryCount * 100).ConfigureAwait(false);
                            }
                        }
                    },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism });
            bufferBlock.LinkTo(senderBlock, new DataflowLinkOptions { PropagateCompletion = true });

            events.ForEach(e => bufferBlock.Post(e));

            bufferBlock.Complete();
            await senderBlock.Completion;

            stopwatch.Stop();

            // Assert
            var testResults = _messageContext.ReceivedMessages.OfType<VehicleResult>().ToList();
            foreach (var result in testResults)
            {
                var expectedResult = results.FirstOrDefault(x => x.Registration == result.Registration);
                expectedResult.Should().NotBeNull($"Couldn't find registration in expected results. {result.Registration}");

                var speedingResult = expectedResult.CameraSpeed.Any(x => x.Speeding);
                var maxSpeedResult = expectedResult.CameraSpeed.Max(x => x.Speed);
                result.ExceededLimit.Should().Be(speedingResult, $"Expected {nameof(result.ExceededLimit)} of ({expectedResult.VehicleType}){result.Registration} to be {speedingResult}. Maximum Speed reached {maxSpeedResult}mph (Result states {result.MaximumSpeed})");
                result.MaximumSpeed.Should().BeCloseTo(maxSpeedResult, 1, $"Expected {nameof(result.MaximumSpeed)} of {result.Registration} to be {maxSpeedResult}mph");
                
                if (result.VehicleCheckPassed.HasValue)
                {
                    var checkResult = await _basicVehicleChecker.VehicleCheck(result.Registration, CancellationToken.None);
                    result.VehicleCheckPassed.Value.Should().Be(checkResult.Value, $"{nameof(checkResult)} of {result.Registration} should be {checkResult}");
                    vehiclesCheck++;
                }
            }

            var missingVehicles = events.Select(x => x.Vehicle.Registration).Distinct().ToList();
            missingVehicles.RemoveAll(x => testResults.Select(x => x.Registration).Contains(x));
            testResults.Count.Should().Be(vehicles, $"Not all vehicles have been processed. Expected {vehicles} results. Missing vehicles: {string.Join(", ", missingVehicles)}");
            Console.WriteLine($"Processing time: {stopwatch.Elapsed.TotalMilliseconds}ms");
            Console.WriteLine($"Vehicles checked {vehiclesCheck} ({Math.Round((100d / vehicles) * vehiclesCheck, 2)}%)");
        }
    }
}
