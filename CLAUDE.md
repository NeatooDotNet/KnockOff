# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**KnockOff** is a Roslyn Source Generator for creating unit test stubs. Unlike Moq's fluent runtime configuration, KnockOff uses partial classes for compile-time setup—trading flexibility for readability and performance.

Key concept: A class marked with `[KnockOff]` that implements an interface will have:
1. Explicit interface implementations generated for all members
2. Interface-named properties for test verification (call counts, args, callbacks)
3. User-defined methods detected and called from generated interceptors

## API Design

| Access | What You Get |
|--------|--------------|
| `stub.Member` | Interceptor (tracking, callbacks) |
| `stub.Object.Member` | Actual value (interface/class instance) |

Three patterns: Inline Interface (`[KnockOff<IFoo>]`), Inline Class (`[KnockOff<SomeClass>]`), and Stand-Alone (`[KnockOff]` on a class implementing an interface). See `docs/getting-started.md` or `Documentation.Samples` for examples.

## Source Generator Requirements

- **Must target `netstandard2.0`** (Roslyn requirement)
- Use `ForAttributeWithMetadataName` for the predicate
- Transform must return **equatable** types (use `EquatableArray<T>`, records)
- Reference RemoteFactory for patterns

## Generator Principles

1. **Generated code must compile.** Emit diagnostics instead of broken code.
2. **Fail fast with clear diagnostics.** Users must understand why and how to fix.
3. **No silent failures.** Every unsupported scenario needs a diagnostic.

## Naming Conventions

Use **Interceptor** terminology for generated tracking/callback classes:
- Per-member: `{Interface}_{Member}Interceptor`
- Container: `{Interface}Interceptors`

**Do NOT use:** `*Intercept`, `*Intercepts`, `*Handler`

## Testing Approach

Use "create objects then test them" pattern:
1. Define a test interface and `[KnockOff]` stub class
2. Source generator produces explicit interface implementations
3. Instantiate the stub and verify behavior through the interface
