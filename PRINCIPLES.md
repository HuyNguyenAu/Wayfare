# Wayfare Engineering & Design Principles

This document defines the core engineering standards, coding mindsets, and architectural patterns followed across the Wayfare codebase. Inspired by clear, pedagogical software systems (e.g., Bob Nystrom's *Crafting Interpreters*), Wayfare emphasises explicit mental models, direct data flow, and transparent state management.

---

## Core Principles at a Glance

| Principle | Core Idea | Practical Rule |
| :--- | :--- | :--- |
| **1. Explicit Data Flow** | Predictable, traceable execution | Flat records, linear pipelines, no hidden magic or reflection hooks. |
| **2. Two-Pass Refinement** | Correctness before optimisation | Implement clean semantics first; optimise performance only when verified. |
| **3. Table-Driven Logic** | Declarative dispatch | Replace nested `switch`/`if` trees with dispatch maps and registries. |
| **4. Boundary Guards** | Fail-fast validation | Validate state and parameters at entry boundaries; keep internals clean. |
| **5. 100% Seamed I/O** | Deterministic testability | Route all side effects (disk, process, API) behind explicit interfaces. |

---

## 1. Explicit Data Flow & Flat State

* **Prefer Flat Polymorphism**: Represent turn states, events, and AST nodes with algebraic discriminated unions (C# positional `record` types). Avoid deep class inheritance hierarchies.
* **Single Source of Truth**: Keep execution state centralised and sequential. Avoid parallel mutable state across disparate manager classes.
* **Traceable Transformations**: Prefer deterministic functions mapping input data to output data over mutable state machines that alter shared singleton context.

---

## 2. Table-Driven Logic Over Deep Conditionals

* When handling disparate message types, tool calls, or user intents, avoid sprawling `if/else` ladders or deeply nested `switch` cascades.
* Use explicit dispatch tables (dictionaries or static map registries) mapping token types/commands to handler delegates or strategy records.
* Adding a new tool, token type, or event handler should be an append-only operation.

---

## 3. Strict Boundary Validation (Fail-Fast)

* **Edge Guards**: Validate public entry points, constructor parameters, and configuration upfront using standard guard clauses (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`).
* **No Tolerated Nulls**: Never tolerate missing required dependencies by silently constructing fallback instances (`?? new FallbackService()`). Required collaborators must be non-null and validated at instantiation.
* **Fail-Fast Startup**: If application configuration, environment keys, or required tool registries fail validation at boot, print clear diagnostic messages to `Console.Error` and exit immediately with a non-zero status.

---

## 4. Zero-Noise Internal Helpers

* **Assume Valid Invariants**: Because entry boundaries guarantee valid state, internal and private methods should execute their tasks directly without repeating redundant null-checks or defensive assertions.
* **Pragmatic Method Sizing**: Write coherent, linear methods that tell a complete story from top to bottom. Do not arbitrarily fragment a cohesive 25-line method into multiple 5-line micro-helpers just to satisfy arbitrary line counts.
* **No Obsolete Directives**: Eliminate visual clutter such as `#region` / `#endregion` blocks.

---

## 5. Pure Boundary Seams for 100% Testability

* Keep business logic completely decoupled from external runtime environments.
* All file system access, terminal renderers, chat clients, and system process executions must sit behind focused interfaces (`IChatClient`, `ISessionStore`, `IToolHelpers`, `IEventBroker`).
* The entire agent execution loop must be 100% testable in-memory with deterministic test doubles and zero side effects.