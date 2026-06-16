using System.Diagnostics;

namespace WayFare.Tools;

internal sealed class ExecuteCommandTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "execute";
    public string DisplayName => "Execute Command";
    public string Description => "Execute a CLI command. Parameters: command (string, required - the executable or command to run), arguments (string, optional - the arguments for the command)";

    public string GetInvocationMessage(string arguments)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out ExecuteCommandArguments? executeCommandArguments, out string? executeCommandArgumentsError))
        {
            throw new ArgumentException($"Failed to deserialise arguments for {Name} tool. Error: {executeCommandArgumentsError}. Arguments: {arguments}");
        }

        return $"[{DisplayName}] [{executeCommandArguments.Command} {executeCommandArguments.Arguments}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserializeArguments(arguments, out ExecuteCommandArguments? executeCommandArguments, out string? executeCommandArgumentsError))
        {
            return new ToolExecutionResult(false, "Failed to execute command due to invalid tool arguments.", string.Empty, $"Failed to execute command: invalid tool arguments. Error: {executeCommandArgumentsError}");
        }

        if (string.IsNullOrWhiteSpace(executeCommandArguments.Command))
        {
            return new ToolExecutionResult(false, "Failed to execute command because 'command' parameter is missing.", string.Empty, "Failed to execute command: 'command' parameter is required.");
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

            if (process is null)
            {
                return new ToolExecutionResult(false, $"Failed to start command '{executeCommandArguments.Command}'.", string.Empty, $"Failed to execute command: process could not be started for '{executeCommandArguments.Command}'.");
            }

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Ignore.
                }
                throw;
            }

            string output = await outputTask;
            string error = await errorTask;

            bool success = process.ExitCode == 0;
            string displayMessage = success ? $"Command '{executeCommandArguments.Command}' executed successfully." : $"Command '{executeCommandArguments.Command}' exited with error code {process.ExitCode}.";
            string result = success ? $"Command '{executeCommandArguments.Command}' executed successfully with exit code 0.{Environment.NewLine}Output:{Environment.NewLine}{output}" : string.Empty;
            string errorMessage = success ? string.Empty : $"Command failed with exit code {process.ExitCode}. {(string.IsNullOrWhiteSpace(error) ? "No error output returned." : $"Stderr: {error}")}";

            return new ToolExecutionResult(success, displayMessage, result, errorMessage);
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while executing command '{executeCommandArguments.Command}'.", string.Empty, $"Failed to execute command: an unexpected error occurred. Error: {ex.Message}", ex);
        }
    }

    internal record ExecuteCommandArguments(string Command, string? Arguments);
}

