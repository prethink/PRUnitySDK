public interface IPickupResource : IPickup
{
    public bool Pickup(ResourceItemDefinition resource, long count);
}