# Skill Documentation Gaps from External Usage Feedback

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-06
**Last Updated:** 2026-02-06

---

## Problem

When the KnockOff skill (`skills/knockoff/SKILL.md`) was used outside of the KnockOff repository to guide Claude in writing tests, Claude found 6 areas confusing or missing. These gaps cause incorrect test code to be generated, requiring manual fixes.

The skill is distributed to other projects where Design projects don't exist, so it must be self-contained and clear enough for Claude to write correct KnockOff tests without access to the generator source.

## Feedback Items

### 1. Method Overload Handling (Critical Gap)

**What's missing:** For overloaded methods, you MUST use `.OnCall()` with a lambda to disambiguate. The compiler selects the correct overload based on lambda parameter count/types. `.Returns()` directly on the interceptor is ambiguous for overloads.

```csharp
// WRONG for overloads:
stub.GetEvents.Returns(result);  // Which overload?

// CORRECT for overloads:
stub.GetEvents.OnCall(() => result);                    // No-param overload
stub.GetEvents.OnCall((attributes) => result);          // With-param overload
```

### 2. When() for Overloads

**What's missing:** `.When()` also resolves overloads by parameter types.

```csharp
stub.GetEvents.When(attributes).Returns(result);
stub.GetEditor.When(editorBaseType).Returns(result);
```

### 3. Abstract Class Stub Constructors

**What's missing:** Stubs for abstract classes with constructors require parameters matching the base class constructor signature.

```csharp
var eventStub = new Stubs.EventDescriptor("Event", new Attribute[0]);
var propertyStub = new Stubs.PropertyDescriptor("Property", new Attribute[0]);
```

### 4. Verification Scope: OnCall Return vs Interceptor

**What's missing:** `.OnCall()` returns a builder that tracks only that specific overload. Calling `.Verify()` on the interceptor counts all overloads combined.

```csharp
var tracking = stub.GetEvents.OnCall(() => result);
tracking.Verify(Called.Once);        // Verifies only no-param calls
stub.GetEvents.Verify(Called.Once);  // Verifies all GetEvents calls (any overload)
```

### 5. No .Object for Interface Stubs (Moq Migration Confusion)

**What's partially missing:** The skill covers `.Object` for class stubs (gotcha #4) but doesn't clearly state that interface stubs use implicit conversion — no `.Object` needed. Users migrating from Moq expect `.Object` everywhere.

```csharp
// Moq pattern (muscle memory):
var descriptor = new SubCustomTypeDescriptor(stub.Object);

// KnockOff interface pattern:
var descriptor = new SubCustomTypeDescriptor(stub);  // Implicit conversion
```

### 6. Verifiable() Returns Builder, Not Mock

**What's missing:** In Moq, `.Verifiable()` returns the mock for chaining more setups. In KnockOff, `.Verifiable()` returns the builder (tracking object). This difference affects chaining patterns.

## Solution

Update the KnockOff skill (`skills/knockoff/`) AND the broader documentation (`docs/`) to address all 6 gaps. Both the skill and the docs should clearly explain these behaviors.

1. Adding an "Overloaded Methods" section covering items #1, #2, and #4
2. Adding a "Class Stub Constructors" note to the Inline Class Pattern section
3. Enhancing the Moq Migration table for items #5 and #6
4. Adding overload-related entries to the Common Mistakes section
5. Checking existing docs (`docs/`, `skills/knockoff/references/`) for the same gaps

All 6 items are confirmed-correct behavior — the goal is better explanation, not code changes.

Before writing documentation, verify each claim against the actual codebase (Design projects and generator code).

---

## Plans

---

## Tasks

- [ ] Verify all 6 claims against actual codebase behavior
- [ ] Check existing docs (`docs/`, `skills/knockoff/references/`) for same gaps
- [ ] Add "Overloaded Methods" section with OnCall/When disambiguation
- [ ] Add constructor parameter guidance for class stubs
- [ ] Add verification scope clarification (tracking vs interceptor)
- [ ] Enhance Moq migration table with .Object and .Verifiable() differences
- [ ] Add overload-related common mistakes
- [ ] Update broader docs (references, guides) where applicable
- [ ] Create MarkdownSnippet samples in Documentation.Samples project
- [ ] Run MarkdownSnippets to sync samples into skill doc

---

## Progress Log

### 2026-02-06
- Received external usage feedback identifying 6 documentation gaps
- Created todo to track the work

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass
- [ ] Skill documentation addresses all 6 feedback items
- [ ] All code samples in skill are backed by MarkdownSnippets (compile-verified)

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]

---

## Results / Conclusions

