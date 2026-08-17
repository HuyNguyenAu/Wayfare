namespace Wayfare.UI;

using System.Text;
using System.Threading.Channels;
using Spectre.Console;
using Wayfare.Infrastructure.Events;
using Wayfare.UI.Components;

public sealed class TerminalUI : ITerminalUI, IEventSink, IAsyncDisposable
{
    private bool _disposed;
    private bool _hasPrompted;
    private bool _hasRenderedToolsHeader;

    private readonly IEventBroker _eventBroker;
    private readonly Task _eventLoopTask;
    private readonly ThinkingStreamRenderer _thinkingStreamRenderer;
    private readonly MarkdownStreamRenderer _markdownStreamRenderer;
    private readonly TaskCompletionSource _agentReadyTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TerminalUI(IEventBroker eventBroker, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventBroker);

        _eventBroker = eventBroker;
        _thinkingStreamRenderer = new ThinkingStreamRenderer();
        _markdownStreamRenderer = new MarkdownStreamRenderer();
        Console.OutputEncoding = Encoding.UTF8;
        AnsiConsole.Clear();

        _eventLoopTask = Task.Run(() => ProcessEventsAsync(cancellationToken), cancellationToken);
    }

    public Task HandleAsync(StartupStartedEvent @event, CancellationToken cancellationToken)
    {
        ProgressRenderer.RenderStartupStarted();
        return Task.CompletedTask;
    }

    public Task HandleAsync(StartupCompletedEvent @event, CancellationToken cancellationToken)
    {
        ProgressRenderer.RenderStartupCompleted();
        return Task.CompletedTask;
    }

    public Task HandleAsync(AgentStartedEvent @event, CancellationToken cancellationToken)
    {
        ProgressRenderer.RenderStartAgent();
        _agentReadyTaskCompletionSource.TrySetResult();
        return Task.CompletedTask;
    }

    public Task HandleAsync(ToolCompilationStartedEvent @event, CancellationToken cancellationToken)
    {
        EnsureToolsHeaderRendered();
        ProgressRenderer.RenderToolCompilationStarted(@event.ToolName);
        return Task.CompletedTask;
    }

    public Task HandleAsync(ToolCompilationCompletedEvent @event, CancellationToken cancellationToken)
    {
        ProgressRenderer.RenderToolCompilationCompleted();
        return Task.CompletedTask;
    }

    public Task HandleAsync(ToolCompilationFailedEvent @event, CancellationToken cancellationToken)
    {
        EnsureToolsHeaderRendered();
        ProgressRenderer.RenderToolCompilationFailed(@event.ToolName, @event.Error);
        return Task.CompletedTask;
    }

    public Task HandleAsync(ToolLoadingStartedEvent @event, CancellationToken cancellationToken)
    {
        EnsureToolsHeaderRendered();
        ProgressRenderer.RenderToolLoadingStarted(@event.ToolName);
        return Task.CompletedTask;
    }

    public Task HandleAsync(ToolLoadingCompletedEvent @event, CancellationToken cancellationToken)
    {
        ProgressRenderer.RenderToolLoadingCompleted();
        return Task.CompletedTask;
    }

    public Task HandleAsync(ToolLoadingFailedEvent @event, CancellationToken cancellationToken)
    {
        EnsureToolsHeaderRendered();
        ProgressRenderer.RenderToolLoadingFailed(@event.ToolName, @event.Error);
        return Task.CompletedTask;
    }

    public Task HandleAsync(ChatRequestStartedEvent @event, CancellationToken cancellationToken)
    {
        ProgressRenderer.RenderChatRequestStarted(@event.ToolNames);
        _thinkingStreamRenderer.StartStream();
        _markdownStreamRenderer.StartStream(cancellationToken);
        return Task.CompletedTask;
    }

    public async Task HandleAsync(ChatRequestCompletedEvent @event, CancellationToken cancellationToken)
    {
        await _thinkingStreamRenderer.CompleteStreamAsync();
        await _markdownStreamRenderer.CompleteStreamAsync();
    }

    public async Task HandleAsync(ThinkingChunkReceivedEvent @event, CancellationToken cancellationToken)
    {
        await _thinkingStreamRenderer.AppendChunkAsync(@event.Content, cancellationToken);
        _markdownStreamRenderer.HeaderCompleted = true;
    }

    public async Task HandleAsync(TokenChunkReceivedEvent @event, CancellationToken cancellationToken)
    {
        await _thinkingStreamRenderer.CompleteStreamAsync();
        await _markdownStreamRenderer.AppendChunkAsync(@event.Content, cancellationToken);
    }

    public Task HandleAsync(ToolExecutionStartedEvent @event, CancellationToken cancellationToken)
    {
        ProgressRenderer.RenderToolExecutionStarted(@event.InvocationMessage);
        return Task.CompletedTask;
    }

    public Task HandleAsync(ToolExecutionCompletedEvent @event, CancellationToken cancellationToken)
    {
        ProgressRenderer.RenderToolExecutionCompleted(@event.Success, @event.DisplayMessage);
        return Task.CompletedTask;
    }

    public Task HandleAsync(SquashingBranchEvent @event, CancellationToken cancellationToken)
    {
        ProgressRenderer.RenderSquashingBranch();
        return Task.CompletedTask;
    }

    public Task HandleAsync(CycleCompletedEvent @event, CancellationToken cancellationToken)
    {
        ProgressRenderer.RenderMilestones(@event.Milestones);
        ProgressRenderer.RenderAuditReport(@event.AuditReport);
        return Task.CompletedTask;
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
                await @event.DispatchAsync(this, cancellationToken);
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
