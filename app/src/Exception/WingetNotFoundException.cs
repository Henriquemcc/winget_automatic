namespace WingetAutomatic.Exception;

public class WingetNotFoundException: System.Exception
{
    public WingetNotFoundException(): base("WinGet not found on the system.")
    {
        
    }
}