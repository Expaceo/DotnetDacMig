
namespace DotnetDacMigration.Container;
internal abstract class WaitUntil
{
    private readonly string[] command;

    protected WaitUntil(params string[] command)
    {
        this.command = command;
    }

    public virtual async Task<bool> Until()
    {
        var fileName = this.command[0];
        var arguments = string.Join(" ", this.command.Skip(1).ToArray());
        (_, _, int exitCode) = await ProcessLauncher.Run(fileName ?? string.Empty, arguments).ConfigureAwait(false);

        return 0L.Equals(exitCode);
    }
}
