
namespace DotnetDacMigration.Container;
internal class WaitUntilUnixPortIsAvailable : WaitUntil
{
    public WaitUntilUnixPortIsAvailable(int port)
        : base(
            "/bin/sh",
            "-c",
            $"true && (cat /proc/net/tcp{{,6}} | awk '{{print $2}}' | grep -i :{port} || nc -vz -w 1 localhost {port} || /bin/bash -c '</dev/tcp/localhost/{port}')")
    {
    }
}
