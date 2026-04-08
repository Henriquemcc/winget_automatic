using WingetAutomatic.Model;

namespace WingetAutomatic.Repository;

public interface LastUpdateRepository
{
    public void save(LastUpdate lastUpdate);
    public LastUpdate? load();
}