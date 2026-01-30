# Evaluate ref/out Variables with Stand-alone Stubs

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-01-30
**Last Updated:** 2026-01-30

---

## Problem

Need to understand how KnockOff handles `ref` and `out` parameters when using stand-alone stub patterns. Key questions:

1. **Does it work?** - Can you currently use ref/out parameters with stand-alone stubs?
2. **Could it work?** - If not currently supported, what would be required?
3. **Does it give you .Verifiable?** - Can you verify that ref/out parameters were called with specific values?

Stand-alone stubs are partial classes that implement an interface or extend a class directly. The ref/out variable handling may differ from inline stubs due to how the user implements the interceptor logic.

## Solution

Investigate the current behavior by:
1. Writing test cases with ref/out parameters using stand-alone stubs
2. Examining generated code to understand what's available
3. Comparing capabilities with inline stubs
4. Documenting findings and any gaps

---

## Plans

---

## Tasks

- [ ] Create test interface with ref/out method signatures
- [ ] Write stand-alone stub implementing the interface
- [ ] Test basic ref/out functionality (does it compile/work?)
- [ ] Test verification scenarios (can you verify call arguments?)
- [ ] Compare with inline stub capabilities
- [ ] Document findings and any limitations
- [ ] Determine if enhancements are needed

---

## Progress Log

---

## Results / Conclusions

