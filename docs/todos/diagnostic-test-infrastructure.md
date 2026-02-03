# Diagnostic Test Infrastructure

**Status:** In Progress
**Priority:** Low
**Created:** 2026-02-03
**Last Updated:** 2026-02-03

---

## Problem

KnockOff has error diagnostics (e.g., KO0200) that cannot be tested with the current test infrastructure. Error diagnostics prevent code from compiling, so adding triggering code to the test project breaks the build. This means diagnostic behavior is verified manually but lacks automated regression tests.

Current diagnostics without automated tests:
- **KO0200**: Standalone stub cannot have user-defined base class

Future diagnostics will also need automated testing infrastructure.

## Solution

Implement a Roslyn `CSharpGeneratorDriver` test harness that:
1. Accepts source code as a string
2. Runs the KnockOff generator
3. Captures emitted diagnostics
4. Allows assertions on diagnostic IDs, severity, messages, and locations

This approach compiles test code in an isolated context without affecting the main test project build.

---

## Plans

[None yet - implementation is straightforward]

---

## Tasks

- [ ] Add Microsoft.CodeAnalysis.CSharp reference to test project (if not already present)
- [ ] Create `DiagnosticTestHelper` class with `CreateCompilation` and `RunGenerator` methods
- [ ] Add test `KO0200_Emitted_WhenStandaloneHasUserBaseClass`
- [ ] Add test `KO0200_BlocksGeneration_WhenBaseClassPresent` (verify no output)
- [ ] Review for additional diagnostics that need tests

---

## Progress Log

### 2026-02-03
- Created this todo as follow-up from Phase 1 of base-class-followup-fixes plan
- KO0200 was manually verified to work correctly (see plan completion evidence)
- This todo tracks the infrastructure work to automate diagnostic testing

---

## Results / Conclusions

[To be completed]
