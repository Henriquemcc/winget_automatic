using WingetAutomatic.Model;

namespace WingetAutomatic.Repository;

public interface ConfigurationRepository
{
    public void save(Configuration configuration);
    public Configuration? load();
}