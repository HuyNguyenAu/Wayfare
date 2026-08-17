# Wayfare System Architecture & Execution Blueprint

This document defines the architectural structure, component layout, and agent execution lifecycle for the Wayfare codebase.

---

## 1. Solution Structure

Wayfare is organised into distinct vertical domain slices:

```
Wayfare/
├── Program.cs                         // Composition root & top-level REPL loop
├── Agent/                             // 5-stage loop, prompt builder, intent resolution
│   ├── Orchestrator.cs                // Core agent execution cycle
│   ├── IntentResolver.cs              // Goal extraction & active intent tracking
│   ├── Prompts.cs                     // Message prompt builder & prompt templates
│   └── CircuitBreaker.cs              // Loop detection & repetition guards
├── Session/                           // Linear turn history, state, and persistence
│   ├── ISession.cs                    // Session contracts and turn management
│   ├── Session.cs                     // In-memory turn history implementation
│   ├── SessionModels.cs               // Immutable turn records & message types
│   └── SessionStore.cs                // Async disk persistence
├── Tools/                             // Tool interfaces, manager, and built-ins
│   ├── ITool.cs                       // Tool interfaces, schema, and execution results
│   ├── ToolManager.cs                 // Dynamic Roslyn tool compilation and registry
│   ├── ToolHelpers.cs                 // Path sanitisation, ignore filters, argument parsing
│   └── Implementations/               // Core tools (read, write, replace, find, list, exec)
├── Infrastructure/                    // External adapters and cross-cutting concerns
│   ├── Configuration/Settings.cs      // Validated application settings
│   ├── AI/                            // MEAI streaming adapters and message mappers
│   └── Events/                        // In-memory event broker & event records
└── UI/                                // Terminal rendering and visual feedback
    ├── ITerminalUI.cs                 // UI presentation contracts
    ├── TerminalUI.cs                  // Spectre.Console implementation
    └── Components/                    // Streaming Markdown & thinking token renderers
```

---

## 2. The 5-Stage Agent Cycle

Every turn in the agent execution loop proceeds through a strictly linear 5-stage pipeline:

```
                  ┌─────────────────────────────────────┐
                  │          1. INITIALISE              │
                  │   Extract goal & setup turn state   │
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
| **1. Initialise** | Context Setup | Ingest user message, extract active `<goal>` tags via `IntentResolver`, and initialise the turn state. |
| **2. Think** | LLM Inference | Construct prompt via `MessagePromptBuilder` and stream tokens via `IChatClient`. |
| **3. Act** | Tool Dispatch | Resolve requested tools via `ToolManager` and execute tools using safe `IToolHelpers` boundaries. |
| **4. Observe** | Feedback Loop | Append tool observation records to turn history and loop back to **Think**. |
| **5. Finalise** | Completion | Compress completed turns into milestone summary, persist session state to disk asynchronously, and emit `CycleCompletedEvent`. |

---

## 4. Tool Plugin Invariants (`ITool`)

All tool implementations adhere to strict boundary standards:

1. **Sealed Class Definition**: Tools are defined as `internal sealed class <Name>Tool(IToolHelpers helpers) : ITool`.
2. **Deterministic Outputs**: Output messages are wrapped in standard structured tags (e.g., `<observation tool="read" path="...">`).
3. **Seamed I/O**: File path checks, directory listings, command executions, and ignore filters route through `IToolHelpers` to allow full in-memory unit testing.
4. **Safe Invocation Messaging**: Display metadata and invocation summaries must be non-throwing even when receiving malformed parameter strings.