namespace Wayfare.UI;

using System.Text;
using System.Threading.Channels;
using Spectre.Console;
using Wayfare.Infrastructure.Events;
using Wayfare.UI.Components;

public sealed class TerminalUI : ITerminalUI, IAsyncDisposable
{
    private bool _disposed;
    private bool _hasPrompted;
    private bool _hasRenderedToolsHeader;

    private readonly IEventBroker _eventBroker;
    private readonly Task _eventLoopTask;
    private readonly ThinkingStreamRenderer _thinkingStreamRenderer = new();
    private readonly MarkdownStreamRenderer _markdownStreamRenderer = new();
    private readonly TaskCompletionSource _agentReadyTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TerminalUI(IEventBroker eventBroker, CancellationToken cancellationToken)
    {
        _eventBroker = eventBroker ?? throw new ArgumentNullException(nameof(eventBroker));
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
        }
    }

    private async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (AgentEvent @event in _eventBroker.ReadAllAsync(cancellationToken))
            {
                await HandleEventAsync(@event, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or ChannelClosedException)
        {
        }
        catch (Exception exception)
        {
            AnsiConsole.MarkupLine($"[{ColourPalette.HexClayEmber}]⚠ UI Event Processing Error: {Markup.Escape(exception.Message)}[/]");
        }
        finally
        {
            _agentReadyTaskCompletionSource.TrySetResult();
            await _thinkingStreamRenderer.CompleteStreamAsync();
            await _markdownStreamRenderer.CompleteStreamAsync();
        }
    }

    private async Task HandleEventAsync(AgentEvent @event, CancellationToken cancellationToken)
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

            case ToolCompilationStartedEvent e:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolCompilationStarted(e.ToolName);
                break;

            case ToolCompilationCompletedEvent:
                ProgressRenderer.RenderToolCompilationCompleted();
                break;

            case ToolCompilationFailedEvent e:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolCompilationFailed(e.ToolName, e.Error);
                break;

            case ToolLoadingStartedEvent e:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolLoadingStarted(e.ToolName);
                break;

            case ToolLoadingCompletedEvent:
                ProgressRenderer.RenderToolLoadingCompleted();
                break;

            case ToolLoadingFailedEvent e:
                EnsureToolsHeaderRendered();
                ProgressRenderer.RenderToolLoadingFailed(e.ToolName, e.Error);
                break;

            case ChatRequestStartedEvent e:
                ProgressRenderer.RenderChatRequestStarted(e.ToolNames);
                _thinkingStreamRenderer.StartStream();
                _markdownStreamRenderer.StartStream(cancellationToken);
                break;

            case ChatRequestCompletedEvent:
                await _thinkingStreamRenderer.CompleteStreamAsync();
                await _markdownStreamRenderer.CompleteStreamAsync();
                break;

            case ThinkingChunkReceivedEvent e:
                await _thinkingStreamRenderer.AppendChunkAsync(e.Content, cancellationToken);
                _markdownStreamRenderer.HeaderCompleted = true;
                break;

            case TokenChunkReceivedEvent e:
                await _thinkingStreamRenderer.CompleteStreamAsync();
                await _markdownStreamRenderer.AppendChunkAsync(e.Content, cancellationToken);
                break;

            case ToolExecutionStartedEvent e:
                ProgressRenderer.RenderToolExecutionStarted(e.InvocationMessage);
                break;

            case ToolExecutionCompletedEvent e:
                ProgressRenderer.RenderToolExecutionCompleted(e.Success, e.DisplayMessage);
                break;

            case SquashingBranchEvent:
                ProgressRenderer.RenderSquashingBranch();
                break;

            case CycleCompletedEvent e:
                ProgressRenderer.RenderMilestones(e.Milestones);
                ProgressRenderer.RenderAuditReport(e.AuditReport);
                break;
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
        }

        _markdownStreamRenderer.ForceDisposePipe();
    }
}
