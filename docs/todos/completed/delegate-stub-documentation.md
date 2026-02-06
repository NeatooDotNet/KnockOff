# Delegate Stub Documentation

**Status:** Complete
**Priority:** Medium
**Created:** 2026-01-24
**Last Updated:** 2026-01-24

---

## Problem

Delegate stubs (`[KnockOff<DelegateType>]`) are not documented in the main documentation guides. The feature only appears in release notes (v0.8.0, v0.20.0).

Users have no way to discover or learn how to use delegate stubs from the primary documentation.

## Solution

1. Create a dedicated delegate stub guide
2. Add delegate pattern to the stub patterns decision matrix

---

## Plans

---

## Tasks

- [x] Create `docs/guides/delegates.md` covering:
  - When to use delegate stubs
  - Inline delegate pattern (`[KnockOff<DelegateType>]`)
  - Configuring with `stub.Interceptor.OnCall(...)`
  - Verification with `stub.Interceptor.Verify()`
  - Open generic delegate support
  - Common use cases (validation rules, factories, callbacks)

- [x] Update `docs/guides/stub-patterns.md` to add delegate pattern to decision matrix:
  ```markdown
  | If you need... | Use this pattern |
  |----------------|------------------|
  | Stub a delegate type | Inline Delegate |
  ```

---

## Progress Log

**2026-01-24:** Created todo after documentation review found delegate stubs are undocumented in main guides.

**2026-01-24:** Documentation complete.
- Created `docs/guides/delegates.md` with comprehensive coverage:
  - When to use delegate stubs (validation rules, factories, event handlers)
  - Basic usage patterns for void, return, and multi-parameter delegates
  - Configuring callbacks with OnCall for void and return delegates
  - Verification with Verify() and Times constraints
  - Tracking invocations with LastCallArg, LastCallArgs, and CallCount
  - Open generic delegate support (closed and open generic patterns with typeof())
  - Reset behavior (clears tracking, preserves configuration)
  - Implicit conversion and method parameter usage
  - Real-world examples (validation rules, factories, event callbacks)
  - Complete integration example
- Updated `docs/guides/stub-patterns.md`:
  - Added "Stub a delegate type" → "Inline Delegate" to quick decision guide
  - Added full Inline Delegate Pattern section with setup, usage, benefits, and trade-offs
  - Updated pattern comparison table to include Inline Delegate column
  - Updated decision tree to include delegate check as step 1
  - Added delegate scenarios to examples table (validation rules, factories, event handlers)
  - Added Delegates Guide to Next Steps section

All documentation uses MarkdownSnippets placeholder format with descriptive snippet names. Code samples are NOT included - placeholders describe what each sample should demonstrate.

---

## Results / Conclusions

Successfully created comprehensive delegate stub documentation:

**Files Created:**
- `docs/guides/delegates.md` - Complete guide covering all delegate stub features with 18 MarkdownSnippets placeholders

**Files Modified:**
- `docs/guides/stub-patterns.md` - Added Inline Delegate pattern to decision guide, comparison table, decision tree, and scenario examples

**Documentation Coverage:**
- Basic usage (void, return, multi-param delegates)
- Configuration (OnCall for void/return/multi-param)
- Verification (Verify, Times, Verifiable pattern)
- Tracking (LastCallArg, LastCallArgs, CallCount)
- Open generics (closed and open generic delegates with type constraints)
- Reset behavior
- Implicit conversion
- Real-world examples (validation rules, factories, event handlers)

**MarkdownSnippets Placeholders:**
All code samples use descriptive placeholder names following the `delegate-stub-*` pattern. No actual code was written - placeholders describe what each sample should demonstrate.

**Next Steps:**
A separate docs-code-samples agent would implement the actual code samples referenced by the MarkdownSnippets placeholders.
