public class ValueStringContainer : ValueContainer<string> { }
public class ValueIntContainer : ValueContainer<int> { }
public class ValueFloatContainer : ValueContainer<float> { }
public class ValueBoolContainer : ValueContainer<bool> { }
public class ValueDoubleContainer : ValueContainer<double> { }
public class ValueLongContainer : ValueContainer<long> { }
public class ValueVector2Container : ValueContainer<UnityEngine.Vector2> { }
public class ValueVector3Container : ValueContainer<UnityEngine.Vector3> { }
public class ValueQuaternionContainer : ValueContainer<UnityEngine.Quaternion> { }
public class ValueGameObjectContainer : ValueContainer<UnityEngine.GameObject> { }
public class ValueTransformContainer : ValueContainer<UnityEngine.Transform> { }
public class ValueObjectContainer : ValueContainer<object> { }

public class ValueContainer<T>
{
    public T Value;

    public ValueContainer() { }

    public ValueContainer(T value)
    {
        this.Value = value;
    }
}