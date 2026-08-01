using UnityEngine;

public interface IStateReactionTrigger : IStateReaction
{
    bool TryReact(Collider collider);
}
