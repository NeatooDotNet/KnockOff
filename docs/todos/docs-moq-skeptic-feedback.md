# Documentation Updates from Moq-Skeptic Review

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-01-19
**Last Updated:** 2026-01-19

---

## Problem

The moq-skeptic-reviewer agent evaluated KnockOff documentation and identified several areas for improvement:

1. The "problem" KnockOff solves is oversold - claims about Moq requiring per-test setup ignore standard patterns
2. Missing documentation for SetupSequence equivalent
3. Verification story is undersold - KnockOff eliminates expression repetition (a real strength)
4. Argument inspection via `LastArg` is undersold (a real strength)
5. Performance claims are irrelevant and should be removed
6. Boilerplate tradeoff not acknowledged - Moq's one-liner vs KnockOff's class declaration

## Solution

Update documentation to:
- Tone down oversold claims
- Add SetupSequence documentation
- Emphasize genuine strengths (no expression repetition, argument inspection, Source() delegation)
- Remove/reduce performance claims
- Acknowledge boilerplate tradeoff honestly

---

## Plans

---

## Tasks

- [ ] Add SetupSequence migration section to `from-moq.md` (functionality exists in `advanced-callbacks.md`, but migration guide doesn't reference it)
- [ ] Emphasize verification strength (no expression repetition vs Moq)
- [ ] Emphasize `LastArg`/`LastArgs` for argument inspection
- [ ] Emphasize `Source()` strength - **major win**: Moq's CallBase only works with concrete classes, NOT interfaces. For interfaces, Moq requires manual per-member delegation. KnockOff's Source() works with interfaces and delegates ALL members in one call.
- [ ] Remove or reduce performance claims in README
- [ ] Tone down "problem" framing - acknowledge Moq has sharing patterns
- [ ] Acknowledge boilerplate tradeoff - Moq's `new Mock<IFoo>()` vs KnockOff's stub class declaration (inherent to source generation)

---

## Progress Log

### 2026-01-19
- Completed moq-skeptic-reviewer evaluation
- Key findings:
  - **KnockOff wins**: Verification without expression repetition, argument inspection, Source() blanket delegation
  - **Moq wins**: DefaultValue.Mock for nested interfaces, zero boilerplate
  - **Correction**: Source() is NOT similar complexity to CallBase - it's a **major win**:
    - Moq's CallBase only works with concrete classes, NOT interfaces
    - For interfaces, Moq requires manual per-member delegation (N setup calls, error-prone, maintenance burden)
    - KnockOff's Source() works with interfaces and delegates ALL members in one call
    - Moq-skeptic verdict: "a legitimate improvement over Moq for this specific use case"
- Score: 6.5/10 - "solid library with genuine merits, but case for switching not strong enough"
  - **Moq DefaultValue clarification**: KnockOff HAS `DefaultValue.Empty` equivalent (returns null/default). What it lacks is `DefaultValue.Mock` (auto-creates nested mock objects for interface-returning properties)

---

## Possible Future Features

- [ ] `DefaultValue.Mock` equivalent - auto-create nested stubs for interface-returning properties (avoids NullReferenceException when accessing nested members)
- [ ] Reset semantics improvement - match Moq developer expectations (see [reset-semantics-improvement.md](reset-semantics-improvement.md))

---

## Results / Conclusions
