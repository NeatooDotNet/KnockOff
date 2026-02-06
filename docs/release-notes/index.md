# Release Notes

Version history for [KnockOff](https://nuget.org/packages/KnockOff) NuGet package.

## Highlights

Releases with notable changes.

| Version | Date | Highlights |
|---------|------|------------|
| [v0.36.0](v0.36.0.md) | 2026-02-06 | Delegate stub rewrite, async API consistency across all 9 patterns, 3 bug fixes |
| [v0.35.0](v0.35.0.md) | 2026-02-05 | Re-versioned from 10.x to 0.x, all previous NuGet packages unlisted |
| [v0.34.0](v0.34.0.md) | 2026-02-04 | **Breaking:** Sequence exhaustion repeats last value, standalone class stubs (patterns 3 & 4), params sequence overloads |
| [v0.33.0](v0.33.0.md) | 2026-02-01 | Verifiable support for user-defined methods |
| [v0.32.0](v0.32.0.md) | 2026-01-30 | **Breaking:** OnCall(value)→Returns(value), When() API for parameter matching |
| [v0.30.0](v0.30.0.md) | 2026-01-27 | Assembly-wide strict mode with `[assembly: KnockOffStrict]` |
| [v0.27.0](v0.27.0.md) | 2026-01-26 | **Breaking:** Unified callback API to method syntax, removed .Value property, added value-based overloads |
| [v0.26.0](v0.26.0.md) | 2026-01-22 | **Breaking:** Removed count properties from public API |
| [v0.25.0](v0.25.0.md) | 2026-01-22 | **Breaking:** Removed WasCalled, added Verify() to method interceptors |
| [v0.24.0](v0.24.0.md) | 2026-01-20 | **Breaking:** Removed CallCount from public API |
| [v0.21.0](v0.21.0.md) | 2026-01-16 | **Breaking:** Interceptor API redesign, method overloading support |
| [v0.20.0](v0.20.0.md) | 2026-01-15 | Open generic delegate and class stubs |
| [v0.18.0](v0.18.0.md) | 2026-01-15 | Open generic inline interface stubs |
| [v0.17.0](v0.17.0.md) | 2026-01-13 | Fluent `.Strict()` extension method |
| [v0.16.0](v0.16.0.md) | 2026-01-13 | Strict mode support |
| [v0.15.0](v0.15.0.md) | 2026-01-12 | stub.Object property, advanced features docs |
| [v0.14.0](v0.14.0.md) | 2026-01-11 | Generic standalone stubs |
| [v0.13.0](v0.13.0.md) | 2026-01-10 | Property `Value` pattern prioritization |
| [v0.12.0](v0.12.0.md) | 2026-01-09 | BCL interface compatibility (117 interfaces tested) |
| [v0.11.0](v0.11.0.md) | 2026-01-09 | Mixed generic/non-generic overload fix |
| [v0.10.0](v0.10.0.md) | 2026-01-08 | Inline stubs generic methods support |
| [v0.7.0](v0.7.0.md) | 2026-01-06 | **Breaking:** Spy→KO rename, zero-allocation tracking |
| [v0.6.0](v0.6.0.md) | 2026-01-04 | Generic methods support with `.Of<T>()` pattern |
| [v0.5.0](v0.5.0.md) | 2026-01-03 | **Breaking:** Interface-scoped handlers, smart defaults |
| [v0.3.0](v0.3.0.md) | 2026-01-01 | Out/ref parameter support |
| [v0.2.0](v0.2.0.md) | 2026-01-01 | Method overloads & delegate-based callbacks |
| [v0.0.0](v0.0.0.md) | 2026-01-01 | Initial release |

## All Releases

- [v0.36.0](v0.36.0.md) - 2026-02-06 - Delegate stub rewrite, async API consistency across all 9 patterns, 3 bug fixes
- [v0.35.0](v0.35.0.md) - 2026-02-05 - Re-versioned from 10.x to 0.x, all previous NuGet packages unlisted
- [v0.34.0](v0.34.0.md) - 2026-02-04 - **Breaking:** Sequence exhaustion repeats last value, standalone class stubs (patterns 3 & 4), params sequence overloads
- [v0.33.0](v0.33.0.md) - 2026-02-01 - Verifiable support for user-defined methods
- [v0.32.0](v0.32.0.md) - 2026-01-30 - **Breaking:** OnCall(value)→Returns(value), When() API for parameter matching
- [v0.31.0](v0.31.0.md) - 2026-01-27 - Source delegation bug fix for inherited interface members
- [v0.30.0](v0.30.0.md) - 2026-01-27 - Assembly-wide strict mode with `[assembly: KnockOffStrict]`
- [v0.29.0](v0.29.0.md) - 2026-01-27 - Simplified async callbacks for overload groups
- [v0.28.0](v0.28.0.md) - 2026-01-27 - Simplified async callbacks
- [v0.27.0](v0.27.0.md) - 2026-01-26 - **Breaking:** Unified callback API to method syntax, removed .Value property, added value-based overloads
- [v0.26.0](v0.26.0.md) - 2026-01-22 - **Breaking:** Removed count properties from public API
- [v0.25.0](v0.25.0.md) - 2026-01-22 - **Breaking:** Removed WasCalled, added Verify() to method interceptors
- [v0.24.0](v0.24.0.md) - 2026-01-20 - **Breaking:** Removed CallCount from public API
- [v0.23.0](v0.23.0.md) - 2026-01-18 - Fix init-only property set tracking in standalone stubs
- [v0.22.0](v0.22.0.md) - 2026-01-17 - Fix LastCallArg/LastCallArgs with sequences
- [v0.21.0](v0.21.0.md) - 2026-01-16 - **Breaking:** Interceptor API redesign, method overloading support
- [v0.20.0](v0.20.0.md) - 2026-01-15 - Open generic delegate and class stubs
- [v0.19.1](v0.19.1.md) - 2026-01-15 - Fix inline stub method overload type mismatches
- [v0.19.0](v0.19.0.md) - 2026-01-15 - BCL collection delegation fix, Model+Renderer refactor
- [v0.18.0](v0.18.0.md) - 2026-01-15 - Open generic inline interface stubs
- [v0.17.0](v0.17.0.md) - 2026-01-13 - Fluent `.Strict()` extension method
- [v0.16.0](v0.16.0.md) - 2026-01-13 - Strict mode support
- [v0.15.0](v0.15.0.md) - 2026-01-12 - stub.Object property, advanced features docs
- [v0.14.0](v0.14.0.md) - 2026-01-11 - Generic standalone stubs
- [v0.13.1](v0.13.1.md) - 2026-01-10 - Best practices guide, generator refactoring
- [v0.13.0](v0.13.0.md) - 2026-01-10 - Property `Value` pattern prioritization
- [v0.12.0](v0.12.0.md) - 2026-01-09 - BCL interface compatibility (117 interfaces tested)
- [v0.11.1](v0.11.1.md) - 2026-01-09 - BCL interface compatibility improvements
- [v0.11.0](v0.11.0.md) - 2026-01-09 - Mixed generic/non-generic overload fix
- [v0.10.1](v0.10.1.md) - 2026-01-08 - Documentation sync and snippet migration
- [v0.10.0](v0.10.0.md) - 2026-01-08 - Inline stubs generic methods support
- [v0.9.0](v0.9.0.md) - 2026-01-07 - **Breaking:** Flat API, single interface constraint, class stub composition
- [v0.8.0](v0.8.0.md) - 2026-01-07 - Inline stubs, delegate stubs, class stubs
- [v0.7.0](v0.7.0.md) - 2026-01-06 - **Breaking:** Spy→KO rename, zero-allocation tracking
- [v0.6.0](v0.6.0.md) - 2026-01-04 - Generic methods support with `.Of<T>()` pattern
- [v0.5.2](v0.5.2.md) - 2026-01-04 - Nested class support for test organization
- [v0.5.1](v0.5.0.md) - 2026-01-03 - Fix mismatched version
- [v0.5.0](v0.5.0.md) - 2026-01-03 - **Breaking:** Interface-scoped handlers, separate overload handlers, property-based OnCall, smart default return values
- [v0.4.1](v0.4.1.md) - 2026-01-02 - Bug fixes for nullable events and collection defaults
- [v0.4.0](v0.4.0.md) - 2026-01-02 - Documentation updates
- [v0.3.0](v0.3.0.md) - 2026-01-01 - Out/ref parameter support
- [v0.2.0](v0.2.0.md) - 2026-01-01 - Method overloads & delegate-based callbacks
- [v0.1.0](v0.1.0.md) - 2026-01-01 - Documentation fixes
- [v0.0.1](v0.0.1.md) - 2026-01-01 - Documentation and repository URL fix
- [v0.0.0](v0.0.0.md) - 2026-01-01 - Initial release
