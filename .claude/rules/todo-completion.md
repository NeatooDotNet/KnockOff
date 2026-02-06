---
paths:
  - "docs/todos/**"
  - "docs/plans/**"
---

# Todo/Plan Completion Gate

**Before changing status to "Complete" on any todo or plan, you MUST verify:**

## Mandatory Verification Steps

1. **Build Design.Stubs**
   ```bash
   dotnet build src/Design/Design.Stubs
   ```
   Must succeed with no errors.

2. **Run Design.Tests**
   ```bash
   dotnet test src/Design/Design.Tests
   ```
   Must pass with no failures.

3. **Build production code**
   ```bash
   dotnet build src/Generator/KnockOff.Generator.csproj
   dotnet build src/Library/KnockOff.csproj
   ```
   Must succeed with no errors.

4. **Build Documentation.Samples**
   ```bash
   dotnet build src/Tests/KnockOff.Documentation.Samples
   ```
   Must succeed with no errors.

5. **Run all tests**
   ```bash
   dotnet test src/KnockOff.sln
   ```
   All tests must pass.

6. **Document verification** in the todo's Completion Verification section:
   ```markdown
   ## Completion Verification

   - [x] Design project builds successfully
   - [x] Design project tests pass

   **Verification results:**
   - Design.Stubs: Build succeeded
   - Design.Tests: X tests passed
   - Production code: Build succeeded
   - Documentation.Samples: Build succeeded
   - All tests: X passed, 0 failed
   ```

## If Verification Fails

**Do NOT mark the todo or plan as Complete.**

Instead:
1. Report the failure: "Build failed with X errors in [project]" or "Y tests failed in [project]"
2. Keep status as "In Progress"
3. Ask the user how to proceed

## Why This Gate Exists

The solution must be in a healthy state before marking work complete:

- **Production code** - The generator and library must compile
- **Design projects** - The source of truth for KnockOff's API must work
- **Documentation samples** - Code shown in docs must actually compile
- **All tests** - Existing functionality must not be broken

A feature marked "Complete" that breaks any of these is worse than incomplete work - it creates false confidence and hidden bugs.
