# Wayfare System Architecture & Execution Blueprint

This document defines the architectural structure, component layout, and agent execution lifecycle for the Wayfare codebase.

---

## 1. Solution Structure

Wayfare is organised into distinct vertical domain slices:

```
Wayfare/
├── Program.cs                                 // Composition root & top-level REPL loop
├── Agent/                                     // 5-stage loop, prompt builder, branch squashing
│   ├── AgentModels.cs                         // Agent interfaces & contract definitions
│   ├── Orchestrator.cs                        // Core agent execution cycle
│   ├── BranchSquasher.cs                      // STARL + Key Artifacts milestone compressor
│   ├── Prompts.cs                             // Message prompt builder & prompt templates
│   ├── PivotDetector.cs                       // User intent pivot detection
│   └── CircuitBreaker.cs                      // Loop detection & repetition breaker guards
├── Session/                                   // Linear turn history, state, and persistence
│   ├── ISession.cs                            // Session contracts and turn management
│   ├── Session.cs                             // In-memory turn history implementation
│   ├── SessionModels.cs                       // Immutable turn records & polymorphic message types
│   └── SessionStore.cs                        // Channel-backed async disk persistence
├── Tools/                                     // Tool interfaces, manager, and built-ins
│   ├── ITool.cs                               // Tool interfaces, schema, and execution results
│   ├── ToolManager.cs                         // Dynamic Roslyn tool compilation and registry
│   ├── ToolHelpers.cs                         // Path sanitisation, ignore filters, argument parsing
│   └── Implementations/                       // Core tools (read, write, replace, find, list, execute, inspect)
│       ├── ExecuteCommandTool.cs
│       ├── FindTool.cs
│       ├── InspectMilestoneTool.cs
│       ├── ListTool.cs
│       ├── ReadFileTool.cs
│       ├── ReplaceTool.cs
│       └── WriteFileTool.cs
├── Infrastructure/                            // External adapters and cross-cutting concerns
│   ├── Configuration/Settings.cs              // Validated application settings
│   ├── AI/                                    // MEAI streaming adapters and message mappers
│   │   ├── ChatModels.cs                      // ToolCall DTO
│   │   ├── ReasoningContent.cs                // Streaming reasoning token model
│   │   ├── SessionMessageMapper.cs            // SessionMessage to ChatMessage converter
│   │   └── ToolMapper.cs                      // ITool to AIFunction / AITool mapper
│   ├── Clients/OpenAIClient.cs                // DelegatingChatClient for OpenAI reasoning extraction
│   └── Events/                                // Channel-based event broker & event records
│       ├── Events.cs
│       └── EventBroker.cs
└── UI/                                        // Terminal rendering and visual feedback
    ├── ITerminalUI.cs                         // UI presentation contracts
    ├── TerminalUI.cs                          // Spectre.Console implementation
    ├── ColourPalette.cs                       // Solarpunk circadian colour tokens
    └── Components/                            // Streaming Markdown & thinking token renderers
        ├── MarkdownStreamRenderer.cs
        ├── ProgressRenderer.cs
        └── ThinkingStreamRenderer.cs
```

---

## 2. The 5-Stage Agent Cycle

Every turn in the agent execution loop proceeds through a strictly linear 5-stage pipeline:

```
                  ┌─────────────────────────────────────┐
                  │          1. INITIALISE              │
                  │   Branch creation & turn setup      │
                  └──────────────────┬──────────────────┘
                                     │
                                     ▼
                  ┌─────────────────────────────────────┐
                  │            2. THINK                 │
                  │   Build prompt & stream inference   │
                  └──────────────────┬──────────────────┘
                                     │
                             Tool Calls Present?
                                ┌────┴────┐
                         Yes   │         │  No / Complete
                               ▼         ▼
    ┌─────────────────────────────┐   ┌─────────────────────────────┐
    │          3. ACT             │   │         5. FINALISE         │
    │   Dispatch tool invocations │   │ Summarise milestone & store │
    └──────────────┬──────────────┘   └─────────────────────────────┘
                   │
                   ▼
    ┌─────────────────────────────┐
    │         4. OBSERVE          │
    │   Record tool observations  │
    └──────────────┬──────────────┘
                   │
                   └─────────── Loop back to (2. THINK)
```

---

## 3. Stage Responsibilities

| Stage | Focus | Key Operations |
| :--- | :--- | :--- |
| **1. Initialise** | Context Setup | Ingest user message, detect branch pivots via `PivotDetector`, create active `BranchNode`, and transition state to `Thinking`. |
| **2. Think** | LLM Inference | Construct system and branch turn messages via `MessagePromptBuilder` and stream inference tokens and tool calls via `IChatClient`. |
| **3. Act** | Tool Dispatch | Intercept repeated tool calls via `CircuitBreaker`, resolve requested tools via `ToolManager`, and execute tools using safe `IToolHelpers` boundaries. |
| **4. Observe** | Feedback Loop | Append `ToolResultMessage` observation records to active branch and loop back to **Think**. |
| **5. Finalise** | Completion | Compress completed active branch into STARL + Key Artifacts milestone summary via `BranchSquasher`, persist session state to disk asynchronously, and emit `CycleCompletedEvent`. |

---

## 4. Milestone Compression & Branch Squashing

When an active working branch concludes, `BranchSquasher` condenses the full trace of user inputs, assistant responses, tool calls, and observations into a persistent milestone record.

### Invariant Schema: STARL + Key Artifacts
Milestone summaries adhere to the following structured format inside `<milestone_summary>` tags:

```xml
<milestone_summary>
Situation: Context before starting this branch.
Task: Specific task or goal.
Action: Steps taken to address the task.
Result: Concrete outcome or produced changes.
Learnings: Key insights or constraints discovered.
Key Artifacts:
- Files: [exact file paths created, modified, or inspected]
- Invariants: [exact verified outputs, exit codes, state transitions, or symbol/schema guarantees]
</milestone_summary>
```

### Exact Invariant Guarantees
1. **Zero Detail Loss for File Mutations**: All modified or created file paths are recorded exactly to avoid ambiguity in subsequent turns.
2. **Deterministic Verification State**: Exit codes, build outcomes, and schema changes are captured under invariants.
3. **XML Boundary Extraction**: `BranchSquasher` strips wrapper tags and conversational preambles to ensure pristine storage in `BranchNode.Summary`.

---

## 5. Tool Plugin Invariants (`ITool`)

All tool implementations adhere to strict boundary standards:

1. **Sealed Class Definition**: Tools are defined as `internal sealed class <Name>Tool(IToolHelpers helpers) : ITool`.
2. **Deterministic Outputs**: Output messages are wrapped in standard structured tags (e.g., `<observation tool="read" path="...">`).
3. **Seamed I/O**: File path checks, directory listings, command executions, and ignore filters route through `IToolHelpers` to allow full in-memory unit testing.
4. **Safe Invocation Messaging**: Display metadata and invocation summaries must be non-throwing even when receiving malformed parameter strings.