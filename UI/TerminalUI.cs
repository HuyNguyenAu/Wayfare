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
    private readonly ThinkingStreamRenderer _thinkingStreamRenderer;
    private readonly MarkdownStreamRenderer _markdownStreamRenderer;
    private readonly TaskCompletionSource _agentReadyTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Dictionary<Type, Func<IEvent, CancellationToken, Task>> _eventHandlers;

    public TerminalUI(IEventBroker eventBroker, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventBroker);

        _eventBroker = eventBroker;
        _thinkingStreamRenderer = new ThinkingStreamRenderer();
        _markdownStreamRenderer = new MarkdownStreamRenderer();
        Console.OutputEncoding = Encoding.UTF8;
        AnsiConsole.Clear();

        _eventHandlers = new()
        {
            [typeof(StartupStartedEvent)] = (_, _) => { ProgressRenderer.RenderStartupStarted(); return Task.CompletedTask; },
            [typeof(StartupCompletedEvent)] = (_, _) => { ProgressRenderer.RenderStartupCompleted(); return Task.CompletedTask; },
            [typeof(AgentStartedEvent)] = (_, _) => { ProgressRenderer.RenderStartAgent(); _agentReadyTaskCompletionSource.TrySetResult(); return Task.CompletedTask; },
            [typeof(ToolCompilationStartedEvent)] = (eventInstance, _) => { EnsureToolsHeaderRendered(); ProgressRenderer.RenderToolCompilationStarted(((ToolCompilationStartedEvent)eventInstance).ToolName); return Task.CompletedTask; },
            [typeof(ToolCompilationCompletedEvent)] = (_, _) => { ProgressRenderer.RenderToolCompilationCompleted(); return Task.CompletedTask; },
            [typeof(ToolCompilationFailedEvent)] = (eventInstance, _) => { EnsureToolsHeaderRendered(); ToolCompilationFailedEvent failedEvent = (ToolCompilationFailedEvent)eventInstance; ProgressRenderer.RenderToolCompilationFailed(failedEvent.ToolName, failedEvent.Error); return Task.CompletedTask; },
            [typeof(ToolLoadingStartedEvent)] = (eventInstance, _) => { EnsureToolsHeaderRendered(); ProgressRenderer.RenderToolLoadingStarted(((ToolLoadingStartedEvent)eventInstance).ToolName); return Task.CompletedTask; },
            [typeof(ToolLoadingCompletedEvent)] = (_, _) => { ProgressRenderer.RenderToolLoadingCompleted(); return Task.CompletedTask; },
            [typeof(ToolLoadingFailedEvent)] = (eventInstance, _) => { EnsureToolsHeaderRendered(); ToolLoadingFailedEvent loadingFailedEvent = (ToolLoadingFailedEvent)eventInstance; ProgressRenderer.RenderToolLoadingFailed(loadingFailedEvent.ToolName, loadingFailedEvent.Error); return Task.CompletedTask; },
            [typeof(ChatRequestStartedEvent)] = (eventInstance, cancellationToken) => { ChatRequestStartedEvent chatStartedEvent = (ChatRequestStartedEvent)eventInstance; ProgressRenderer.RenderChatRequestStarted(chatStartedEvent.ToolNames); _thinkingStreamRenderer.StartStream(); _markdownStreamRenderer.StartStream(cancellationToken); return Task.CompletedTask; },
            [typeof(ChatRequestCompletedEvent)] = async (_, _) => { await _thinkingStreamRenderer.CompleteStreamAsync(); await _markdownStreamRenderer.CompleteStreamAsync(); },
            [typeof(ThinkingChunkReceivedEvent)] = async (eventInstance, cancellationToken) => { ThinkingChunkReceivedEvent thinkingEvent = (ThinkingChunkReceivedEvent)eventInstance; await _thinkingStreamRenderer.AppendChunkAsync(thinkingEvent.Content, cancellationToken); _markdownStreamRenderer.HeaderCompleted = true; },
            [typeof(TokenChunkReceivedEvent)] = async (eventInstance, cancellationToken) => { TokenChunkReceivedEvent tokenEvent = (TokenChunkReceivedEvent)eventInstance; await _thinkingStreamRenderer.CompleteStreamAsync(); await _markdownStreamRenderer.AppendChunkAsync(tokenEvent.Content, cancellationToken); },
            [typeof(ToolExecutionStartedEvent)] = (eventInstance, _) => { ToolExecutionStartedEvent executionStartedEvent = (ToolExecutionStartedEvent)eventInstance; ProgressRenderer.RenderToolExecutionStarted(executionStartedEvent.InvocationMessage); return Task.CompletedTask; },
            [typeof(ToolExecutionCompletedEvent)] = (eventInstance, _) => { ToolExecutionCompletedEvent executionCompletedEvent = (ToolExecutionCompletedEvent)eventInstance; ProgressRenderer.RenderToolExecutionCompleted(executionCompletedEvent.Success, executionCompletedEvent.DisplayMessage); return Task.CompletedTask; },
            [typeof(SquashingBranchEvent)] = (_, _) => { ProgressRenderer.RenderSquashingBranch(); return Task.CompletedTask; },
            [typeof(CycleCompletedEvent)] = (eventInstance, _) => { CycleCompletedEvent cycleCompletedEvent = (CycleCompletedEvent)eventInstance; ProgressRenderer.RenderObjectiveAndMilestones(cycleCompletedEvent.Objective, cycleCompletedEvent.Milestones); return Task.CompletedTask; },
        };

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
            // Expected completion / cancellation.
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
            // Shutdown expected.
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
        if (_eventHandlers.TryGetValue(@event.GetType(), out Func<IEvent, CancellationToken, Task>? handler))
        {
            await handler(@event, cancellationToken);
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
            // Ignore task cancellation.
        }

        _markdownStreamRenderer.ForceDisposePipe();
    }
}
