namespace AverageSpeed.Domain
{
    using System;

    public interface IEntity
    {
        string Id { get; }

        Guid ETag { get; set; }
    }
}