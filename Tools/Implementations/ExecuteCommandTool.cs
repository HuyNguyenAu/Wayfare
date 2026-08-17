namespace Wayfare.Tools.Implementations;

using System.Diagnostics;
using Wayfare.Tools;

internal sealed class ExecuteCommandTool(IToolHelpers toolHelpers) : ITool
{
    public string Name => "execute";
    public string DisplayName => "Execute Command";
    public string Description => "Execute a CLI command. Parameters: command (string, required - the executable or command to run), arguments (string, optional - the arguments for the command)";

    public ToolSchema Parameters => ToolSchema.Object(new Dictionary<string, ToolPropertySchema>
    {
        ["command"] = ToolPropertySchema.String("The executable or command to run."),
        ["arguments"] = ToolPropertySchema.String("The arguments for the command.")
    });

    public string GetInvocationMessage(string arguments)
    {
        return toolHelpers.TryDeserialiseArguments(arguments, out ExecuteCommandArguments? commandArguments, out _)
            ? $"[{DisplayName}] [{commandArguments.Command} {commandArguments.Arguments}]".TrimEnd()
            : $"[{DisplayName}] [{arguments}]";
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string arguments, CancellationToken cancellationToken)
    {
        if (!toolHelpers.TryDeserialiseArguments(arguments, out ExecuteCommandArguments? executeCommandArguments, out string? executeCommandArgumentsError))
        {
            return new ToolExecutionResult(false, "Failed to execute command due to invalid tool arguments.", string.Empty, $"Failed to execute command: invalid tool arguments. Error: {executeCommandArgumentsError}. Usage: {{\"command\": \"<executable>\", \"arguments\": \"<optional args>\"}}");
        }

        if (string.IsNullOrWhiteSpace(executeCommandArguments.Command))
        {
            return new ToolExecutionResult(false, "Failed to execute command because 'command' parameter is missing.", string.Empty, "Failed to execute command: 'command' parameter is required. Specify the executable or command to run, e.g. {\"command\": \"dotnet\", \"arguments\": \"test\"}.");
        }

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = executeCommandArguments.Command,
                Arguments = executeCommandArguments.Arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(startInfo);

            if (process is null)
            {
                return new ToolExecutionResult(false, $"Failed to start command '{executeCommandArguments.Command}'.", string.Empty, $"Failed to execute command: process could not be started for '{executeCommandArguments.Command}'. Verify that the executable exists and is available on PATH.");
            }

            using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(45));

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(linkedCancellationTokenSource.Token);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(linkedCancellationTokenSource.Token);

            try
            {
                await process.WaitForExitAsync(linkedCancellationTokenSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && linkedCancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best-effort cleanup.
                }

                string commandDescription = $"{executeCommandArguments.Command} {executeCommandArguments.Arguments}".Trim();
                return new ToolExecutionResult(
                    false,
                    $"Command '{executeCommandArguments.Command}' timed out after 45 seconds.",
                    string.Empty,
                    $"Failed to execute command: command '{commandDescription}' timed out after 45 seconds and was terminated. Consider running a more specific subcommand, reducing workload, or breaking the task down.");
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

            string truncatedOutput = TruncateOutput(output);
            string truncatedError = TruncateOutput(error);

            bool success = process.ExitCode == 0;
            string displayMessage = success ? $"Command '{executeCommandArguments.Command}' executed successfully." : $"Command '{executeCommandArguments.Command}' exited with error code {process.ExitCode}.";
            string result = success ? $"Command '{executeCommandArguments.Command}' executed successfully with exit code 0.{Environment.NewLine}Output:{Environment.NewLine}{truncatedOutput}" : string.Empty;
            string errorMessage = success ? string.Empty : $"Command failed with exit code {process.ExitCode}. {(string.IsNullOrWhiteSpace(truncatedError) ? (string.IsNullOrWhiteSpace(truncatedOutput) ? "No output was written to stdout or stderr." : $"Stdout: {truncatedOutput}") : $"Stderr: {truncatedError}")}";

            return new ToolExecutionResult(success, displayMessage, result, errorMessage);
        }
        catch (Exception exception)
        {
            return new ToolExecutionResult(false, $"An unexpected error occurred while executing command '{executeCommandArguments.Command}'.", string.Empty, $"Failed to execute command: an unexpected error occurred. Error: {exception.Message}", exception);
        }
    }

    internal static string TruncateOutput(string output, int maxCharacters = 6000)
    {
        if (string.IsNullOrEmpty(output) || output.Length <= maxCharacters)
        {
            return output;
        }

        int halfLength = maxCharacters / 2;
        int headLength = halfLength;
        int tailLength = halfLength;
        int truncatedCount = output.Length - maxCharacters;

        return $"{output[..headLength]}{Environment.NewLine}{Environment.NewLine}[... TRUNCATED {truncatedCount} CHARACTERS ...]{Environment.NewLine}{Environment.NewLine}{output[^tailLength..]}";
    }

    internal record ExecuteCommandArguments(string Command = "", string Arguments = "");
}
