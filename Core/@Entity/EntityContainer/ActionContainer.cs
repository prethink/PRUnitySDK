using System;

public class ActionContainer : ContainerEntityBase<ActionBase>
{
    public override Enumeration EntityType => ContainerTypeEnumerationProvider.ActionContainer;

    public override string Name => Info.GetLocalization();

    public override string GetPoolKey()
    {
        return base.GetPoolKey() + containerItem.name;
    }

    protected override void InitializeEntityInfo()
    {
        //TODO:
        throw new NotImplementedException();
    }

    protected override bool TryPickup(PlayerBase player)
    {
        containerItem.Execute();
        return true;
    }
}
