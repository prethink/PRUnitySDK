using System;

[Serializable]
public class ActionDatabase : Database<KeyValueWrapper<string, ActionBase>>
{
    public override string DataBaseKey => nameof(ActionDatabase);
}
