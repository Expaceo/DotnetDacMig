namespace DotnetDacMigration.Container;

/// <summary>
/// Starts a new docker container
/// </summary>
public abstract class ContainerRunner
{
    private const string GetPortCmd = @"inspect --format=""{{range $p, $conf := .NetworkSettings.Ports}} {{$p}}={{(index $conf 0).HostPort}} {{end}}"" ";
    private readonly string dockerRunArguments;

    protected ContainerRunner(string dockerRunArguments)
    {
        this.dockerRunArguments = dockerRunArguments;
    }

    public string? ConnectionString { get; protected set; }


    protected string? ContainerId { get; private set; }


    protected int Port { get; private set; }

    public async Task<bool> StartAsync()
    {
        var result = true;
        var startTime = DateTimeOffset.UtcNow;

        (string stdout, string stderr, _) = await RunDockerCommand(dockerRunArguments);
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            const int maxAttemps = 3;
            ContainerId = stdout.Trim(' ', '\n');
            for (int i = 1; i <= maxAttemps; i++)
            {
                int exitCode;
                (stdout, stderr, exitCode) = await RunDockerCommand(GetPortCmd + ContainerId);
                if (exitCode != 0)
                {
                    if (i == maxAttemps)
                    {
                        throw new Exception($"Unable to get exposed port{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
                    }

                    await Task.Delay(1000 * i);
                    continue;
                }

                string[] tab = stdout.Split('=');
                if (tab.Length == 1)
                {
                    if (i == maxAttemps)
                    {
                        throw new Exception($"Unable to get exposed port{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
                    }

                    await Task.Delay(1000 * i);
                    continue;
                }

                Port = int.Parse(tab[1]);
                break;
            }

            WaitUntil? waitUntil;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                waitUntil = new WaitUntilUnixPortIsAvailable(Port);
            }
            else
            {
                waitUntil = new WaitUntilWindowsPortIsAvailable(Port);
            }

            while (true)
            {
                if (await waitUntil.Until())
                {
                    break;
                }

                var endTime = DateTimeOffset.UtcNow;
                if (endTime - startTime > TimeSpan.FromSeconds(60))
                {
                    break;
                }
            }

            // wait until succeffully container ready or 2 minutes
            startTime = DateTime.UtcNow;
            while (!await ReadinessProbe())
            {
                var endTime = DateTimeOffset.UtcNow;
                if (endTime - startTime > TimeSpan.FromSeconds(120))
                {
                    // too long
                    break;
                }
                else
                {
                    await Task.Delay(1000);
                }
            }

            await OnStarted();
        }
        else if (!string.IsNullOrWhiteSpace(stderr))
        {
            throw new Exception(stderr);
        }
        else
        {
            result = false;
        }

        return result;
    }

    /// <summary>
    /// Kill container if started previously.
    /// </summary>
    /// <returns>true.</returns>
    public async Task<bool> StopAsync()
    {
        if (!string.IsNullOrWhiteSpace(ContainerId))
        {
            var id = ContainerId;
            ContainerId = string.Empty;

            await RunDockerCommand($"kill {id}");
            await RunDockerCommand($"rm {id}");
        }

        return true;
    }

    protected abstract Task OnStarted();

    protected abstract Task<bool> ReadinessProbe();

    protected Task<(string Stdout, string Stderr, int Exitcode)> RunDockerCommand(string arguments) =>
        ProcessLauncher.Run("docker", arguments);

    protected Task<(string Stdout, string Stderr, int Exitcode)> RunCommandInContainer(string arguments)
    {
        return RunDockerCommand($"exec {ContainerId} {arguments}");
    }
}