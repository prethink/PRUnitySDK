using System;

public interface IDamageModifier 
{
    Guid ModifierIdentifier { get; }
    string ModifierName { get; }
}
