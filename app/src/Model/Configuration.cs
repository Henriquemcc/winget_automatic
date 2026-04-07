using WingetAutomatic.Models;

namespace WingetAutomatic.Model;

class Configuration
{
    TimeSpan updateInterval = TimeSpan.FromDays(1);
    List<String> ignoredPackages = new List<string>();
    RebootPolicy rebootPolicy = RebootPolicy.Never;
    bool ignoreSecurityHash = false;
    bool ignoreMalwareScan = false;
    string? downloadProxy = null;
    bool disableProxy = false;
}