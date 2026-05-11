namespace AverageSpeed.Test
{
    using Autofac;

    using Factories;
    using Messaging;
    using Services;

    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CaptureEventFactory>().As<ICaptureEventFactory>();
            builder.RegisterType<RoadFactory>().As<IRoadFactory>();
            builder.RegisterType<VehicleFactory>().As<IVehicleFactory>();

            builder.RegisterGeneric(typeof(Repository<>)).AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TestMessageContext>().AsSelf();
            builder.RegisterType<BasicVehicleChecker>();
        }
    }
}
