using System.Collections.Generic;

public interface IRatingSystem 
{
    public long Rank { get; }
    public void RequestRating();

    HashSet<string> TableName { get; }

    void RegisterTableName(string tableName);
}
