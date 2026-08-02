using UnityEngine;

public static class TransformExtension 
{
    public static Transform GetTransform(this MonoBehaviour obj)
    {
        if (obj.IsNull())
            return null;

        if (obj is EntityBase entity)
            return entity.EntityGameObject.transform;

        return obj.transform;
    }
}
