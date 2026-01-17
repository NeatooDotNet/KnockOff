# KnockOff Documentation Rewrite

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-17

---

## Problem

The existing KnockOff documentation has been removed. We need a complete documentation rewrite from scratch that targets senior C# developers familiar with Moq who are interested in using KnockOff.

**Key Goals:**
- Target audience: Senior C# developers using Moq
- Highlight: Different syntax, standalone vs inline stubs, compile-time vs runtime breaking changes
- Emphasize: Source generation nature (lightly mention performance)
- Approach: Treat KnockOff as brand new (no migration focus)
- Requirements: All samples must be compiled and tested via docs-snippets skill

---

## Solution

Create comprehensive, production-quality documentation using:
- **Sonnet model** for writing documentation
- **Opus model** for reviewing key documents
- **doc-writing skill** for professional technical writing guidance
- **docs-snippets skill** for compiled, tested code examples

---

## Tasks

### Phase 1: Discovery ✓
- [x] Understand documentation scope and requirements
- [x] Confirm target audience and key messages

### Phase 2: Codebase Exploration ✓
- [x] Launch code-explorer agents for user API, generator architecture, and test patterns
- [x] Read key files identified by agents
- [x] Build comprehensive understanding of KnockOff

### Phase 3: Clarifying Questions ✓
- [x] Ask questions about documentation structure and organization
- [x] Clarify table of contents and depth of coverage
- [x] Determine examples and comparison approach with Moq
- [x] Get approval on documentation scope

### Phase 4: Architecture Design
- [ ] Design documentation structure and organization
- [ ] Plan folder structure (guides, reference, etc.)
- [ ] Identify all documentation files needed
- [ ] Map code samples to documentation sections

### Phase 5: Implementation
- [ ] Write README.md
- [ ] Write getting started guide
- [ ] Write core guides (stub patterns, interceptor API, etc.)
- [ ] Write reference documentation
- [ ] Create all code samples with snippet markers
- [ ] Verify all snippets compile and pass tests

### Phase 6: Quality Review
- [ ] Review documentation with Opus model
- [ ] Address review feedback
- [ ] Final polish and consistency check

### Phase 7: Summary
- [ ] Finalize documentation
- [ ] Summary of what was created
- [ ] Update project status

---

## Progress Log

### 2026-01-17 - Initial Setup

**Completed:**
1. Created todo tracking file
2. Launched 3 parallel code-explorer agents:
   - User API exploration (agent a16fbe1)
   - Source generator architecture (agent a64d762)
   - Test patterns and examples (agent a818496)
3. Read key files:
   - KnockOffAttribute.cs - attribute definitions
   - GettingStartedSamples.cs - basic usage patterns
   - InlineStubsSamples.cs - inline stub patterns
   - InterceptorApiSamples.cs - complete API reference
   - BasicTests.cs - core functionality tests

**Key Findings:**
- **Four stub patterns**: Standalone, inline interface, inline class, delegate
- **Duality pattern**: `stub.Member` for interceptors, `stub`/`stub.Object` for interface/class instance
- **Interceptor API**: Methods (OnCall, CallCount, LastCallArg), Properties (OnGet/OnSet, GetCount/SetCount), Events (Raise, AddCount), Generics (Of<T>())
- **Source generation**: Roslyn incremental generator with Model-Builder-Renderer architecture
- **Smart defaults**: Task/ValueTask handling, compile-time safety

**Decisions Made:**
1. **Structure**: Traditional README → Getting Started → Guides → Reference
2. **Files**: Separate file per topic (methods, properties, events, generics, etc.)
3. **README**: Moderate scope + installation + 2-3 examples + quick Moq comparison
4. **Moq Comparison**: Dedicated comparison page ONLY (not in guides except README)
5. **Examples**: Progressive approach (start simple, build to complex)
6. **Advanced Features**:
   - Generics: Full explanation of type-specific tracking
   - Method overloads: CONFIRMED - numbered suffix pattern is GONE
   - Sequence API: Flow from basic to full
   - Strict mode: Full comprehensive guide
7. **File Organization**: Approved as proposed

**Method Overload API - VERIFIED:**
- Single interceptor per method name (e.g., `stub.GetByIdAsync`)
- Multiple `OnCall` overloads resolved by compiler based on lambda signature
- Each overload returns separate tracking object
- Aggregate tracking: `CallCount` and `WasCalled` across all overloads
- NO numbered suffixes (Process1, Process2) - that pattern is REMOVED

**Numbered Suffix Pattern - CLARIFIED:**
- Only appears when user-defined method collides with interface member
- Example: Interface has `GetValue`, stub has protected `GetValue()`, generates `GetValue2` interceptor
- NOT used for method overloads

**Next Steps:**
- Design documentation architecture (Phase 4)
- Begin implementation (Phase 5)

---

## Key Decisions

### Target Audience
- Senior C# developers currently using Moq
- Assume DDD terminology familiarity (per project conventions)
- Focus on practical usage, not academic explanations

### Key Messages to Emphasize
1. **Compile-time safety** - Breaking changes at compile time vs runtime
2. **Source generation** - How it works and benefits
3. **Syntax differences** from Moq - Clear comparisons
4. **Standalone vs inline** stubs - When to use each
5. **Performance** - Mentioned lightly (inconsequential vs test overhead)

### Documentation Standards
- All code examples must compile and be tested
- Use MarkdownSnippets for sync between docs and compiled code
- Follow doc-writing skill guidance for professional quality
- No migration content (treat as brand new)

---

## Results / Conclusions

*To be filled in upon completion*
