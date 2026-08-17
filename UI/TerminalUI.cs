namespace Wayfare.UI;

using System.Text;
using System.Threading.Channels;
using Spectre.Console;
using Wayfare.Infrastructure.Events;
using Wayfare.UI.Components;

public class TerminalUI : ITerminalUI, IAsyncDisposable
{
    private bool _disposed;
    private bool _hasPrompted;
    private bool _hasRenderedToolsHeader;

    private readonly IEventBroker _eventBroker;
    private readonly Task _eventLoopTask;
    private readonly MarkdownStreamRenderer _markdownStreamRenderer;
    private readonly TaskCompletionSource _agentReadyTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TerminalUI(IEventBroker eventBroker, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventBroker);

        _eventBroker = eventBroker;
        _markdownStreamRenderer = new MarkdownStreamRenderer();
        Console.OutputEncoding = Encoding.UTF8;
        AnsiConsole.Clear();

        _eventLoopTask = Task.Run(() => ProcessEventsAsync(cancellationToken), cancellationToken);
    }

    public async Task<string> GetUserInputAsync(CancellationToken cancellationToken)
    {
        await _agentReadyTaskCompletionSource.Task.WaitAsync(cancellationToken);

        if (_hasPrompted)
        {
            AnsiConsole.WriteLine(Environment.NewLine);
        }
        else
        {
            _hasPrompted = true;
        }

        string promptMarkup = $"[{ColourPalette.HexTerracottaSol}]⁖ verdant[/][{ColourPalette.HexSunlitOchre}] ❯[/] ";

        return await AnsiConsole.PromptAsync(
            new TextPrompt<string>(promptMarkup)
                .PromptStyle(new Style(foreground: ColourPalette.MyceliumLinen))
                .AllowEmpty(),
            cancellationToken
        );
    }

    public async Task WaitForCompletionAsync()
    {
        try
        {
            await _eventLoopTask;
        }
        catch (Exception exception) when (exception is OperationCanceledException or ChannelClosedException)
        {
            // Expected completion / cancellation
        }
    }

    private async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (IEvent @event in _eventBroker.ReadAllAsync(cancellationToken))
            {
                await HandleEventAsync(@event, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or ChannelClosedException)
        {
            // Shutdown expected
        }
        catch (Exception exception)
        {
            AnsiConsole.MarkupLine($"[{ColourPalette.HexClayEmber}]⚠ UI Event Processing Error: {Markup.Escape(exception.Message)}[/]");
        }
        finally
        {
            _agentReadyTaskCompletionSource.TrySetResult();
            await _markdownStreamRenderer.CompleteStreamAsync();
        }
    }

    private void EnsureToolsHeaderRendered()
    {
        if (!_hasRenderedToolsHeader)
        {
            _hasRenderedToolsHeader = true;
            ProgressRenderer.RenderLoadingToolsStarted();
        }
    }

    private async Task HandleEventAsync(IEvent @event, CancellationToken cancellationToken)
    {
        switch (@event)
        {
            case StartupStartedEvent:
                ProgressRenderer.RenderStartupStarted();
                break;
            case StartupCompletedEvent:
                ProgressRenderer.RenderStartupCompleted();
                break;
            case AgentStartedEvent:
                ProgressRenderer.RenderStartAgent();
                _agentReadyTaskCompletionSource.TrySetResult();
                break;
            case ToolCompilationStartedEvent toolCompilationStartedEvent:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolCompilationStarted(toolCompilationStartedEvent.ToolName);
                break;
            case ToolCompilationCompletedEvent:
                ProgressRenderer.RenderToolCompilationCompleted();
                break;
            case ToolCompilationFailedEvent toolCompilationFailedEvent:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolCompilationFailed(toolCompilationFailedEvent.ToolName, toolCompilationFailedEvent.Error);
                break;
            case ToolLoadingStartedEvent toolLoadingStartedEvent:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolLoadingStarted(toolLoadingStartedEvent.ToolName);
                break;
            case ToolLoadingCompletedEvent:
                ProgressRenderer.RenderToolLoadingCompleted();
                break;
            case ToolLoadingFailedEvent toolLoadingFailedEvent:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolLoadingFailed(toolLoadingFailedEvent.ToolName, toolLoadingFailedEvent.Error);
                break;
            case ChatRequestStartedEvent chatRequestStartedEvent:
                ProgressRenderer.RenderChatRequestStarted(chatRequestStartedEvent.ToolNames);
                _markdownStreamRenderer.StartStream(cancellationToken);
                break;
            case ChatRequestCompletedEvent:
                await _markdownStreamRenderer.CompleteStreamAsync();
                break;
            case TokenChunkReceivedEvent tokenChunkReceivedEvent:
                await _markdownStreamRenderer.AppendChunkAsync(tokenChunkReceivedEvent.Content, cancellationToken);
                break;
            case ToolExecutionStartedEvent toolExecutionStartedEvent:
                ProgressRenderer.RenderToolExecutionStarted(toolExecutionStartedEvent.InvocationMessage);
                break;
            case ToolExecutionCompletedEvent toolExecutionCompletedEvent:
                ProgressRenderer.RenderToolExecutionCompleted(toolExecutionCompletedEvent.Success, toolExecutionCompletedEvent.DisplayMessage);
                break;
            case SquashingBranchEvent:
                ProgressRenderer.RenderSquashingBranch();
                break;
            case CycleCompletedEvent cycleCompletedEvent:
                ProgressRenderer.RenderObjectiveAndMilestones(cycleCompletedEvent.Objective, cycleCompletedEvent.Milestones);
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _agentReadyTaskCompletionSource.TrySetResult();

        _eventBroker.Complete();

        try
        {
            await _eventLoopTask;
        }
        catch (Exception exception) when (exception is OperationCanceledException or ChannelClosedException)
        {
            // Ignore task cancellation
        }

        _markdownStreamRenderer.ForceDisposePipe();
    }
}
