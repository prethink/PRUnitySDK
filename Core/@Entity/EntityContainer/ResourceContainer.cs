using System;
using UnityEngine;

public class ResourceContainer : ContainerEntityBase<ResourceItemDefinition>
{
    [SerializeField] private long resourceCount;

    #region Базовый класс

    public override Enumeration EntityType => ContainerTypeEnumerationProvider.ResourceContainer;

    public override string Name => containerItem.Name;

    public override bool CanPickup(PlayerBase player)
    {
        return (canPickup & PlayerBase.ConvertToFlag(player.PlayerType)) != 0;
    }

    protected override void InitializeEntityInfo()
    {
        //TODO:
        throw new NotImplementedException();
    }

    protected override bool TryPickup(PlayerBase player)
    {
        if (player.gameObject.TryGetComponentInChildren<IPickupResource>(out var pickup))
        {
            isTaken = pickup.Pickup(containerItem, resourceCount);
            if(isTaken)
            {
                //TODO:
                //PRUnitySDK.Managers.Sound.PlayClipAtPoint(containerItem.GetResourceSound, transform.position);
                DestroyEntity();
            }

            return isTaken;
        }

        return isTaken;
    }

    #endregion
}
