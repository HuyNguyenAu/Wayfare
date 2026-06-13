using System.Diagnostics;

namespace WayFare.Tools;

internal sealed class ExecuteCommandTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "Execute Command";
    public string Description => "Execute a CLI command. Parameters: command (string, required - the executable or command to run), arguments (string, optional - the arguments for the command)";

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out ExecuteCommandArguments? executeCommandArguments, out string? executeCommandArgumentsError))
        {
            return new ToolExecutionResult(false, string.Empty, executeCommandArgumentsError);
        }

        if (string.IsNullOrWhiteSpace(executeCommandArguments.Command))
        {
            return new ToolExecutionResult(false, string.Empty, "Failed to execute command because 'command' parameter is required");
        }

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = executeCommandArguments.Command,
                Arguments = executeCommandArguments.Arguments ?? string.Empty,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(startInfo);

            if (process == null)
            {
                return new ToolExecutionResult(false, string.Empty, $"Failed to execute '{executeCommandArguments.Command}' command because the process could not be started.");
            }

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            string output = await outputTask;
            string error = await errorTask;

            bool success = process.ExitCode == 0;

            return new ToolExecutionResult(success, output, success ? string.Empty : error);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, string.Empty, "Failed to execute command", ex);
        }
    }

    internal record ExecuteCommandArguments(string Command, string? Arguments);
}

