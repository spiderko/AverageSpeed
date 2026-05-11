namespace AverageSpeed
{
    using Autofac;

    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CaptureEventMessageHandler>().AsImplementedInterfaces().AsSelf();
        }
    }
}
