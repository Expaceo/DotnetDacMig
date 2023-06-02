
namespace DotnetDacMigration.Container;
internal class WaitUntilWindowsPortIsAvailable : WaitUntil
{
    public WaitUntilWindowsPortIsAvailable(int port)
      : base("PowerShell", "-Command", $"Exit !(Test-NetConnection -ComputerName 'localhost' -Port {port}).TcpTestSucceeded")
    {
    }
}