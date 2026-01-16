# Documentation Rewrite Design

**Created:** 2026-01-15

---

## Goals

1. **README:** ~150 lines featuring highlights and quick usage
2. **Docs:** Serve both developers and LLMs
3. **Remove:** Performance claims that didn't prove true
4. **Emphasize:** Readability → Duality → Compile-time safety (in that order)

## Audience

Both Moq users (migrating) and testing newcomers equally.

## Moq Comparison Strategy

- Quick comparison in README (selling point)
- Detailed comparison doc for those wanting depth
- Feature guides focus purely on KnockOff (no Moq clutter)

---

## New Structure

```
README.md                          # ~150 lines, feature showcase
docs/
├── getting-started.md             # Install → First stub → First test
├── why-knockoff/
│   ├── readability.md             # Primary value prop (detailed)
│   ├── duality-pattern.md         # User methods + callbacks
│   └── compile-time-safety.md     # Interface changes caught at compile time
├── guides/
│   ├── stub-patterns.md           # Inline vs standalone vs delegate (consolidated)
│   ├── methods.md                 # Methods, overloads, async, generics
│   ├── properties.md              # Properties, indexers, init/required
│   ├── events.md                  # Event handling
│   └── verification.md            # CallCount, LastCallArg, assertions
├── reference/
│   ├── interceptor-api.md         # Complete API reference
│   ├── attributes.md              # [KnockOff], [KnockOff<T>]
│   └── diagnostics.md             # Compiler diagnostics
├── migration-from-moq.md          # Step-by-step migration
├── knockoff-vs-moq.md             # Detailed comparison
├── for-ai-assistants.md           # LLM generation guidelines
└── release-notes/                 # Keep existing structure
```

---

## README Outline (~150 lines)

```markdown
# KnockOff

One-sentence: Source-generated test stubs with readable syntax.

## Why KnockOff?

Three short paragraphs (2-3 sentences each):
1. **Readable tests** — Less ceremony than Moq
2. **Two ways to customize** — User methods for defaults, callbacks for overrides
3. **Compile-time safety** — Interface changes break the build, not runtime

## Quick Comparison

| Task | Moq | KnockOff |
|------|-----|----------|
| Setup return value | `mock.Setup(x => x.Method()).Returns(value)` | `stub.Method.OnCall = (ko, args) => value` |
| Verify called | `mock.Verify(x => x.Method(), Times.Once)` | `Assert.Equal(1, stub.Method.CallCount)` |
| Check argument | `Callback<T>(x => captured = x)` | `stub.Method.LastCallArg` |

## Installation

dotnet add package KnockOff

## Your First Stub

One complete example: interface → stub class → test usage (~20 lines)

## Features

Bullet list of supported features

## Documentation

Links to getting-started, why-knockoff, guides, migration

## Limitations

Brief list of what Moq does that KnockOff doesn't
```

---

## Why KnockOff Docs

### `why-knockoff/readability.md`

**Goal:** Show that KnockOff tests are easier to read and write.

- The Problem with Moq Syntax (lambdas, expression trees, ceremony)
- KnockOff's Approach (direct property access, named tuples)
- Side-by-Side Examples (3-4 common scenarios)
- Line Count Comparison

### `why-knockoff/duality-pattern.md`

**Goal:** Explain the unique two-layer customization model.

- Two Ways to Customize (user methods vs callbacks)
- When to Use Each (decision table)
- Priority Order (callback → user method → default)
- Combining Both (example)

### `why-knockoff/compile-time-safety.md`

**Goal:** Concrete examples of what breaks at compile time vs runtime.

- The Problem (Moq fails at runtime)
- What KnockOff Catches (added members, renamed methods, changed signatures)
- Before/After Examples showing compiler errors

---

## Guides

### `guides/stub-patterns.md`

**Goal:** One place to understand all three stub patterns.

- Choosing a Pattern (comparison table)
- Inline Stubs (`[KnockOff<T>]` on test class)
- Standalone Stubs (`[KnockOff]` implementing interface)
- Delegate Stubs (named delegates, Func<>, Action<>)

### `guides/methods.md`

- Basic Methods (void, return values)
- Async Methods (Task, ValueTask)
- Method Overloads (numbered interceptors)
- Generic Methods
- ref/out Parameters

### `guides/properties.md`

- Properties (.Value, OnGet, OnSet)
- Init-only Properties
- Required Properties
- Indexers (Backing dictionary)
- Multiple Indexers (different key types)

### `guides/events.md`

- Subscribing and Unsubscribing
- Raising Events
- Verification (AddCount, RemoveCount, HasSubscribers)

### `guides/verification.md`

- Method Verification (CallCount, WasCalled, LastCallArg)
- Property Verification (GetCount, SetCount, LastSetValue)
- Reset Behavior
- Assertion Patterns

---

## Reference Docs

### `reference/interceptor-api.md`

Complete API reference:
- Method Interceptors (OnCall, CallCount, WasCalled, LastCallArg, LastCallArgs, Reset)
- Property Interceptors (Value, OnGet, OnSet, GetCount, SetCount, LastSetValue, Reset)
- Indexer Interceptors (Backing, OnGet, OnSet, GetCount, SetCount, LastGetKey, LastSetEntry, Reset)
- Event Interceptors (Raise, AddCount, RemoveCount, HasSubscribers)

### `reference/attributes.md`

- `[KnockOff]` attribute
- `[KnockOff<T>]` attribute
- Placement rules

### `reference/diagnostics.md`

- All KO#### diagnostics with explanations and fixes

---

## Migration and Comparison

### `migration-from-moq.md`

Step-by-step process:
1. Create stub class
2. Replace Mock<T> instantiation
3. Convert Setup() → OnCall or user methods
4. Convert Verify() → Assert + CallCount

Pattern-by-pattern translation table.

### `knockoff-vs-moq.md`

- Feature support matrix
- Side-by-side examples for each supported feature
- When to use Moq vs KnockOff

---

## For AI Assistants

### `for-ai-assistants.md`

Explicit LLM guidance:
- When to use KnockOff
- Generation patterns (exact code templates)
- Common mistakes to avoid
- Interceptor naming rules
- Complete examples for each stub pattern

---

## Files to Delete

All existing guide files will be replaced:
- `docs/guides/async-methods.md` → merged into `methods.md`
- `docs/guides/best-practices.md` → distributed across relevant docs
- `docs/guides/delegates.md` → merged into `stub-patterns.md`
- `docs/guides/generics.md` → merged into `methods.md`
- `docs/guides/indexers.md` → merged into `properties.md`
- `docs/guides/inline-stubs.md` → merged into `stub-patterns.md`
- `docs/guides/interface-inheritance.md` → merged into `stub-patterns.md`
- `docs/guides/multiple-interfaces.md` → merged into `stub-patterns.md`
- `docs/concepts/customization-patterns.md` → becomes `why-knockoff/duality-pattern.md`
- `docs/framework-comparison.md` → replaced by `knockoff-vs-moq.md`

## Files to Keep

- `docs/release-notes/*` — keep as-is
- `docs/reference/diagnostics.md` — update if needed
- Sample code in `docs/samples/` — keep, may need new snippets

---

## Implementation Order

1. README.md
2. getting-started.md
3. why-knockoff/ (all three)
4. for-ai-assistants.md
5. guides/ (all five)
6. reference/ (all three)
7. migration-from-moq.md
8. knockoff-vs-moq.md
9. Delete old files
10. Run mdsnippets, verify all code blocks
