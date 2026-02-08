# Documentation Structure Plan

**Date:** 2026-01-18
**Related Todo:** [Documentation Rewrite](../todos/documentation-rewrite.md)
**Status:** Draft
**Last Updated:** 2026-01-18

---

## Overview

Complete documentation architecture for KnockOff, following a developer journey from evaluation to mastery.

---

## Documentation Structure

```
KnockOff/
├── README.md                           # Evaluation + Quick Start
└── docs/
    ├── getting-started.md              # Installation + First Stub
    ├── guides/                         # Feature-focused tutorials
    │   ├── stub-patterns.md            # Stand-Alone, Inline Interface, Inline Class
    │   ├── methods.md                  # Method interceptors and callbacks
    │   ├── properties.md               # Value vs Get/Set patterns
    │   ├── events.md                   # Raising and verifying events
    │   ├── async-patterns.md           # Task/ValueTask handling
    │   ├── verification.md             # Testing calls, arguments, and state
    │   ├── advanced-callbacks.md       # Sequential returns, exceptions
    │   ├── generic-methods.md          # Of<T>() pattern for generics
    │   ├── source-delegation.md        # Source(T) for real implementations
    │   └── stub-overrides.md             # Compile-time defaults
    ├── reference/                      # API documentation
    │   ├── interceptor-api.md          # Complete interceptor member reference
    │   ├── attribute-options.md        # KnockOff attribute configuration
    │   └── smart-defaults.md           # Default return value behavior
    ├── migration/
    │   └── from-moq.md                 # Moq to KnockOff migration guide
    └── troubleshooting.md              # Common errors and solutions
```

---

## Document Outlines

### README.md

1. **Hero Section** - One-sentence value proposition + badges
2. **The Problem** - Why existing mocking frameworks fall short
3. **The Solution** - Code teaser (`snippet: readme-teaser`)
4. **Key Features** - Bullet list of capabilities
5. **Quick Start** - 4 snippets: install, stub, configure, verify
6. **Documentation Links** - Guide navigation
7. **Why KnockOff?** - Comparison table vs Moq/NSubstitute
8. **License + Contributing**

### docs/getting-started.md

1. **Introduction** - What KnockOff does, prerequisites
2. **Installation** (`snippet: getting-started-install`)
3. **Your First Stub - Stand-Alone** (`snippet: getting-started-standalone-*`)
4. **Your First Stub - Inline** (`snippet: getting-started-inline-*`)
5. **Understanding Generated Code** - Where files are, how to debug
6. **Next Steps** - Links to guides

### docs/guides/stub-patterns.md

1. **Overview** - Three patterns, decision guide
2. **Stand-Alone/Flat** - When, how, benefits, trade-offs
3. **Inline Interface** - When, how, benefits, trade-offs
4. **Inline Class** - When, how, .Object requirement
5. **Pattern Comparison Table**
6. **Choosing a Pattern** - Decision tree
7. **Complete Example** - All three in realistic test

### docs/guides/methods.md

1. **Introduction** - OnCall signature structure
2. **Configuring Method Behavior** - void, return, multi-param
3. **Verifying Method Calls** - WasCalled, CallCount
4. **Capturing Arguments** - LastCallArg, LastCallArgs
5. **Overloaded Methods** - Naming convention
6. **Resetting Interceptors**
7. **Complete Example**

### docs/guides/properties.md

1. **Introduction** - Static vs dynamic approaches
2. **Static Values** - Value property
3. **Dynamic Getters** - Get callback
4. **Setter Interception** - Set callback
5. **Verifying Property Access** - GetCount, SetCount, LastSetValue
6. **Value vs Get Priority** - Get replaces Value
7. **Resetting Properties** - Reset preserves Value
8. **Decision Guide Table**
9. **Complete Example**

### docs/guides/events.md

1. **Introduction** - Event interceptor capabilities
2. **Raising Events** - EventHandler<T>, Action<T>
3. **Verifying Subscriptions** - HasSubscribers, AddCount
4. **Verifying Unsubscriptions** - RemoveCount
5. **Resetting Events** - Clears handlers
6. **Complete Example**

### docs/guides/async-patterns.md

1. **Introduction** - Async support overview
2. **Task<T> Methods** - Task.FromResult, CompletedTask
3. **ValueTask<T> Methods**
4. **Simulating Delays**
5. **Simulating Failures** - FromException, throw
6. **Complete Example**

### docs/guides/verification.md

1. **Introduction** - What you can verify
2. **Basic Call Verification** - WasCalled, CallCount
3. **Argument Verification** - LastCallArg/Args
4. **Call History Tracking** - Capture in OnCall
5. **Call Order Verification**
6. **Cross-Interceptor Verification**
7. **Complete Example**

### docs/guides/advanced-callbacks.md

1. **Introduction** - When simple OnCall isn't enough
2. **Sequential Returns** - Queue, counter-based
3. **Conditional Returns** - Switch on argument
4. **Throwing Exceptions**
5. **State-Dependent Behavior**
6. **Side Effects**
7. **Complete Example**

### docs/guides/generic-methods.md

1. **Introduction** - .Of<T>() solution
2. **Type-Specific Configuration**
3. **Type-Specific Verification**
4. **Multiple Type Parameters**
5. **CalledTypeArguments**
6. **Resetting** - Per-type and all
7. **Complete Example**

### docs/guides/source-delegation.md

1. **Introduction** - What Source(T) does
2. **Basic Source Delegation**
3. **Partial Delegation** - Override specific methods
4. **Interface Hierarchies**
5. **Clearing Source**
6. **When to Use Source**
7. **Priority Order** - OnCall > User Method > Source > Smart Default
8. **Complete Example**

### docs/guides/stub-overrides.md

1. **Introduction** - Compile-time defaults
2. **Defining User Methods** - Protected method syntax
3. **Priority Order**
4. **Overriding in Tests**
5. **Resetting to User Method**
6. **Common Patterns**
7. **Complete Example**

### docs/reference/interceptor-api.md

1. **Overview** - Interceptor types
2. **Method Interceptor** - All properties/methods
3. **Property Interceptor** - All properties/methods
4. **Indexer Interceptor** - All properties/methods
5. **Event Interceptor** - All properties/methods
6. **Generic Method Interceptor** - Base + .Of<T>()
7. **Reset Behavior Summary** - Table

### docs/reference/attribute-options.md

1. **Overview** - Placement rules
2. **Stand-Alone Pattern** - [KnockOff]
3. **Inline Interface Pattern** - [KnockOff<IService>]
4. **Inline Class Pattern** - [KnockOff<MyClass>]
5. **Multiple Stubs** - Multiple attributes

### docs/reference/smart-defaults.md

1. **Overview** - Priority order
2. **Value Types** - default(T)
3. **Nullable Reference Types** - null
4. **Types with new()** - new T()
5. **Collection Interfaces** - IList<T> → List<T>
6. **Non-nullable Without Constructor** - Throws
7. **Complete Mapping Table**

### docs/migration/from-moq.md

1. **Introduction** - Why migrate
2. **Quick Reference Table** - Moq → KnockOff
3. **Step 1-8** - Migration steps with before/after
4. **Complete Before/After Example**
5. **Common Gotchas**

### docs/troubleshooting.md

1. **Compilation Errors** - partial, .Object, OnCall signature
2. **Runtime Errors** - No callback configured
3. **Unexpected Behavior** - Get priority, Reset behavior
4. **Generator Issues** - Build, diagnostics
5. **Getting Help**

---

## Document Creation Order

1. **README.md** - Entry point, needed first
2. **getting-started.md** - Immediate follow-up for adopters
3. **guides/stub-patterns.md** - Core concept
4. **guides/methods.md** - Most common use case
5. **guides/properties.md** - Second most common
6. **guides/verification.md** - Essential for all tests
7. **reference/interceptor-api.md** - API reference
8. **guides/async-patterns.md**
9. **guides/events.md**
10. **guides/advanced-callbacks.md**
11. **guides/generic-methods.md**
12. **guides/source-delegation.md**
13. **guides/stub-overrides.md**
14. **reference/attribute-options.md**
15. **reference/smart-defaults.md**
16. **migration/from-moq.md**
17. **troubleshooting.md**

---

## MarkdownSnippets Placeholders

All code samples use the format:
```markdown
<!-- snippet: descriptive-name -->
<!-- endSnippet -->
```

Naming convention: `{document-area}-{feature}-{scenario}`

Examples:
- `readme-teaser`
- `methods-oncall-multi-param`
- `properties-value-basic`
- `moq-migration-complete-after`

---

## Acceptance Criteria

- [ ] All documents created with structure above
- [ ] All snippet placeholders in place
- [ ] Links between documents work
- [ ] Developer journey flows logically
- [ ] Code samples project compiles
- [ ] MarkdownSnippets generates final docs

---

## Risks / Considerations

- Many documents - ensure consistency across all
- Snippet naming must be unique across entire docs
- Need to verify all interceptor API members are documented
