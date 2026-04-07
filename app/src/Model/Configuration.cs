using WingetAutomatic.Models;

namespace WingetAutomatic.Model;

public class Configuration
{
    public TimeSpan updateInterval = TimeSpan.FromDays(1);
    public List<String> ignoredPackages = new List<string>();
    public RebootPolicy rebootPolicy = RebootPolicy.Never;
    public bool ignoreSecurityHash = false;
    public bool ignoreMalwareScan = false;
    public string? downloadProxy = null;
    public bool disableProxy = false;
}