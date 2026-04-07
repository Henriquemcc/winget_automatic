using WingetAutomatic.Model;

namespace WingetAutomatic.Repository;

interface ConfigurationRepository
{
    public void save(Configuration configuration);
    public Configuration? load();
}