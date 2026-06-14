using System.Threading.Tasks;

public interface ISaveable 
{
    public Task<bool> TrySaveData();
}
