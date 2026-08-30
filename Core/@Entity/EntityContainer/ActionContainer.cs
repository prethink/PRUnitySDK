using System;

public class ActionContainer : ContainerEntityBase<IconActionBase>
{
    public override Enumeration EntityType => ContainerTypeEnumerationProvider.ActionContainer;

    public override string Name => Description.GetLocalization();

    public override string GetPoolKey()
    {
        return base.GetPoolKey() + containerItem.name;
    }

    protected override bool TryPickup(PlayerBase player)
    {
        return containerItem != null && containerItem.Execute();
    }
}
