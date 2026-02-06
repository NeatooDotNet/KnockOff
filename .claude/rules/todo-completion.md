---
paths:
  - "docs/todos/**"
  - "docs/plans/**"
---

# Todo/Plan Completion Gate

## Who May Mark Complete

**Only the architect agent (during post-implementation verification) or the orchestrator (after architect verification passes) may change a todo or plan status to "Complete".**

The developer agent may NOT:
- Change todo status to "Complete"
- Change plan status to "Complete"
- Move files to `completed/` directories
- Classify test failures as "pre-existing" to bypass verification

When the developer finishes implementation, they set plan status to "Awaiting Verification" and STOP. The architect independently verifies before completion.

## Mandatory Verification Steps (Architect Runs These)

The architect must independently run all of these — not trust the developer's reported results:

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
   All tests must pass. **Zero failures — no exceptions.**

6. **Document verification** in the plan's Architect Verification section:
   ```markdown
   ## Architect Verification

   **Verified:** [date]
   **Verdict:** VERIFIED

   **Independent test results:**
   - Design.Stubs: Build succeeded
   - Design.Tests: X tests passed, 0 failed
   - Production code: Build succeeded
   - Documentation.Samples: Build succeeded
   - All tests: X passed, 0 failed
   ```

## If Verification Fails

**Do NOT mark the todo or plan as Complete.**

Instead:
1. Report EVERY failure — do not classify any as "pre-existing" or "acceptable"
2. Set plan status to "Sent Back"
3. Document issues in the plan's Architect Verification section
4. Report to orchestrator for developer to fix

## Why This Gate Exists

The agent that does the work must NOT be the agent that certifies the work is complete. Independent verification catches:

- Test failures the developer dismissed as "pre-existing"
- Implementation that doesn't match the original design
- Regressions in unrelated features
- Incomplete work marked as done to "finish up"

A feature marked "Complete" that breaks any verification step is worse than incomplete work — it creates false confidence and hidden bugs.
