# Unified Agent Workflow Design

**Date:** 2026-01-19
**Related Todo:** [Implement Unified Agent Workflow](../todos/unified-agent-workflow.md)
**Status:** Complete
**Last Updated:** 2026-01-19

---

## Overview

Formalize the integration between Claude Code plan mode, knockoff-architect agent, knockoff-developer agent, and the project-todos skill to create a systematic workflow that produces quality, production-ready implementations.

**Current Problem:**
- Plans sometimes incomplete (missing pattern analysis, breaking change assessment)
- Features sometimes partially implemented
- No systematic handoff between design and implementation phases

**Solution:**
Create an automatic pipeline with multiple verification gates, clear handoffs, and evidence-based completion requirements.

---

## Approach

**Automatic Pipeline:**
```
Plan Mode → knockoff-architect → knockoff-developer → Implementation
```

**Key Principles:**
1. Automatic progression through phases (seamless handoffs)
2. User stays in control of iteration decisions
3. Multiple verification gates prevent incomplete work
4. Single source of truth in project-todos skill for mechanics
5. Agents provide domain expertise, skill provides structure

---

## Design

### 1. Overall Workflow Architecture

```
User Request
    ↓
[Plan Mode]
  - Brainstorming conversation
  - Explores approaches, clarifies requirements
  - Creates design document
  - AT END: Creates todo + plan (using project-todos)
    ↓
[Automatic Handoff to knockoff-architect]
  - Reviews plan mode's design
  - Enhances with KnockOff-specific architecture
  - Must complete verification checklist before proceeding
  - Updates plan with architectural details
  - Hands off to knockoff-developer
    ↓
[knockoff-developer Review]
  - Reviews architect's design
  - Performs deep codebase analysis
  - Identifies concerns/gaps/risks
  - IF concerns found → Asks user: "Should I send back to architect?"
  - IF no concerns → Creates implementation contract
  - Asks user: "Ready to implement?"
    ↓
[Implementation Phase]
  - Developer implements according to plan
  - Checklist-driven with milestone verification
  - STOPS if: architectural discovery OR out-of-scope test failure
  - Updates progress log after each milestone
  - Provides evidence for completion
    ↓
[Completion]
  - All checklist items verified
  - Evidence documented
  - Todo and plan moved to completed/
```

### 2. Project-Todos Skill Enhancements

**New Section: Role-Specific Guidance**

```markdown
## For Architects (knockoff-architect agent)

When creating or enhancing a plan, you must:

1. **Complete the Architectural Verification Checklist** (see below)
2. **Use project-todos skill for structure** - templates, file paths, linking
3. **Apply your architectural expertise for content** - design decisions, trade-offs
4. **Document codebase analysis** - which files you examined, patterns found

### Architectural Verification Checklist
Before handing off to developer, verify:
- [ ] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [ ] Breaking changes assessment completed
- [ ] Pattern consistency check (follows or intentionally deviates)
- [ ] Diagnostic requirements identified
- [ ] Test strategy defined
- [ ] Edge cases documented
- [ ] Codebase deep-dive completed (document files examined)

## For Developers (knockoff-developer agent)

When reviewing and implementing a plan, you must:

1. **Review Phase**: Analyze architect's design for completeness
2. **Concern Documentation**: If issues found, document in "Developer Review" section
3. **Implementation Contract**: Before coding, list exactly what will be implemented
4. **Checklist-Driven**: Every file change and test is a checklist item
5. **Milestone Verification**: Run tests and verify after each phase
6. **Evidence-Based Completion**: Provide proof (test output, code snippets)

### When to STOP and Ask User
- **ALWAYS STOP**: Out-of-scope test failures
- **ALWAYS STOP**: Architectural discoveries that contradict the design
- **Document and continue**: Minor implementation adjustments (note in progress log)
```

**Enhanced Plan Template Sections:**

```markdown
## Architectural Verification
[Architect completes this checklist before handoff]

**Three Patterns Analysis:**
- Standalone: [How this applies or N/A]
- Inline Interface: [How this applies or N/A]
- Inline Class: [How this applies or N/A]

**Breaking Changes:** Yes/No - [Explanation]

**Pattern Consistency:** [How design follows existing patterns or intentional deviation]

**Codebase Analysis:** [Files examined, patterns found]

---

## Developer Review
[Developer adds concerns/questions here during review phase]

**Status:** [Not Started / Under Review / Concerns Raised / Approved]

**Concerns:** [List any issues found, or "None - ready for implementation"]

---

## Implementation Contract
[Developer fills before starting implementation]

**In Scope:**
- [ ] File 1: Specific changes
- [ ] File 2: Specific changes
- [ ] Test cases to add

**Out of Scope:**
[Explicitly list what will NOT be changed]

---

## Implementation Progress

**Phase 1:** [Name]
- [ ] Step 1
- [ ] Step 2
- [ ] **Verification**: [Test results, evidence]

[Continue for each phase]

---

## Completion Evidence
[Required before marking complete]

- **Tests Passing:** [Output or screenshot]
- **Generated Code Sample:** [Snippet showing feature works]
- **All Checklist Items:** [Confirmed 100% complete]
```

**Plan Status Values:**

- `Draft (Architect)` - Architect working on design
- `Under Review (Developer)` - Developer reviewing
- `Concerns Raised` - Developer found issues, awaiting user decision
- `Ready for Implementation` - Approved, implementation contract created
- `In Progress` - Developer implementing
- `Complete` - All evidence provided, moved to completed/

### 3. Plan Mode Integration

**Changes to CLAUDE.md:**

```markdown
## Plan Mode and Project Todos

**When plan mode completes:**
1. Plan mode creates the design document as usual
2. Plan mode then uses project-todos skill to:
   - Create a todo in docs/todos/ capturing the user request
   - Create a plan in docs/plans/ with the design content
   - Link them together
   - Set todo status: "In Progress"
   - Set plan status: "Draft (Architect)"

**After plan mode creates todo+plan:**
- Automatically invoke knockoff-architect agent to enhance the plan
- Architect reviews, adds KnockOff-specific architecture, completes verification checklist
- Architect hands off to knockoff-developer for review

This creates a seamless flow: brainstorming → formalization → architectural design → implementation planning → implementation.
```

**Plan Mode Behavior:**

At the end of plan mode (after design validated):
1. Say: "I'll now formalize this into a todo and plan using the project-todos workflow"
2. Gather any missing todo metadata (title, priority, problem statement)
3. Use project-todos skill to create todo file
4. Use project-todos skill to create plan file with the design content
5. Link them together
6. Report: "Created docs/todos/[name].md and docs/plans/[name].md"
7. Say: "Now handing off to knockoff-architect to enhance the plan"
8. Automatically invoke knockoff-architect agent

### 4. Agent Behavior Specifications

**knockoff-architect enhancements:**

```markdown
## Workflow Integration

### When Invoked After Plan Mode

You will receive a plan file that plan mode created. Your job:

1. **Read the existing plan** - Understand the initial design
2. **Read the linked todo** - Understand the user's core request
3. **Perform deep codebase analysis** - Study relevant files, patterns
4. **Enhance the plan** with KnockOff-specific architecture:
   - Complete "Architectural Verification" section
   - Analyze all three stub patterns
   - Assess breaking changes
   - Check pattern consistency
   - Define test strategy
   - Document edge cases
   - List files examined

5. **Update plan status** to "Under Review (Developer)"
6. **Update todo Last Updated** date
7. **Hand off to knockoff-developer** - Automatically invoke

### Architectural Verification Checklist

Before handing off, you MUST complete:
- [ ] All three patterns analyzed
- [ ] Breaking changes assessment completed
- [ ] Pattern consistency verified
- [ ] Diagnostic requirements identified
- [ ] Test strategy defined
- [ ] Edge cases documented
- [ ] Codebase deep-dive completed

### After Developer Raises Concerns

If developer finds issues and user asks you to address them:
1. Read "Developer Review" section
2. Address each concern
3. Update the plan
4. Clear or mark concerns as addressed
5. Hand back to developer for re-review
```

**knockoff-developer enhancements:**

```markdown
## Workflow Integration

### When Invoked After Architect

You will receive an enhanced plan. Your job:

1. **Review Phase**:
   - Read plan thoroughly
   - Verify architectural verification checklist complete
   - Perform your own deep codebase analysis
   - Look for gaps, missing considerations, issues
   - Check test impact

2. **Document Findings**:
   - If concerns: Add to "Developer Review" section
   - Update status to "Concerns Raised"
   - Ask user: "Should I send back to architect?"
   - Wait for user decision

3. **If No Concerns**:
   - Create "Implementation Contract" section
   - List every file change and test
   - List what is OUT OF SCOPE
   - Update status to "Ready for Implementation"
   - Ask user: "Shall I proceed?"

### Implementation Phase

1. **Checklist-Driven**: Work through contract items, check off each
2. **Milestone Verification**: Run tests after each phase, document results
3. **STOP Conditions**: Out-of-scope test failures, architectural discoveries
4. **Evidence Collection**: Capture test output, code snippets
5. **Completion**: All items checked, evidence documented, move to completed/
```

### 5. Automatic Handoff Mechanism

**Architect → Developer:**
```markdown
I've completed the architectural design and verification checklist.

[Invokes Task tool with knockoff-developer agent]
Prompt: "Review the plan at docs/plans/[name].md. Perform deep analysis and document concerns or create implementation contract if ready."
```

**Developer → Architect (when concerns raised):**
```markdown
I've identified concerns: [lists]

Should I send this back to the architect?

[If user approves:]
[Invokes Task tool with knockoff-architect agent]
Prompt: "Address developer concerns in 'Developer Review' section of docs/plans/[name].md"
```

**Plan Mode → Architect:**
```markdown
Created todo and plan. Now handing off to knockoff-architect.

[Invokes Task tool with knockoff-architect agent]
Prompt: "Enhance docs/plans/[name].md with KnockOff-specific architecture. Complete verification checklist."
```

### 6. Error Handling and Edge Cases

**Edge Case 1: User skips plan mode**
- Architect detects no todo/plan exists
- Creates them using project-todos skill
- Continues with architectural design

**Edge Case 2: Architectural problems during implementation**
- Developer STOPS immediately
- Documents problem
- Asks user: Stop and send to architect / Propose solution / Discuss

**Edge Case 3: Multiple iteration cycles**
- After 2-3 cycles, developer detects pattern
- Suggests: Continue refining / Have conversation / Simplify scope

**Edge Case 4: Out-of-scope tests fail**
- STOPS immediately (sacred rule)
- Reports which tests and what they cover
- Asks: Fix now / Add to bug list / Investigate further
- Never proceeds without user approval

**Edge Case 5: User modifies in-progress plan**
- Agent updates plan and progress log
- Updates "Last Updated" date
- Asks if re-review needed with changes

---

## Implementation Steps

1. **Update project-todos skill**
   - Add "Role-Specific Guidance" section
   - Enhance plan template with new sections
   - Add plan status value documentation

2. **Update knockoff-architect agent**
   - Add "Workflow Integration" section
   - Add verification checklist requirements
   - Add handoff behavior specifications

3. **Update knockoff-developer agent**
   - Add "Workflow Integration" section
   - Add review phase requirements
   - Add implementation contract requirements
   - Add evidence collection requirements

4. **Update CLAUDE.md**
   - Add "Plan Mode and Project Todos" section
   - Document the automatic pipeline

5. **Test the workflow**
   - Use simple feature request
   - Verify each phase works correctly
   - Verify handoffs are automatic
   - Verify STOP conditions trigger
   - Verify evidence collection works

---

## Acceptance Criteria

- [ ] project-todos skill has role-specific guidance
- [ ] Plan template has all new sections
- [ ] knockoff-architect agent follows verification checklist
- [ ] knockoff-developer agent creates implementation contracts
- [ ] Handoffs between agents work automatically
- [ ] STOP conditions halt work appropriately
- [ ] Evidence-based completion prevents partial work
- [ ] CLAUDE.md documents the workflow
- [ ] Test workflow with sample feature request succeeds

---

## Dependencies

- Existing project-todos skill
- Existing knockoff-architect agent
- Existing knockoff-developer agent
- CLAUDE.md project instructions

---

## Risks / Considerations

- **Agent autonomy balance**: Too automatic might feel out of control, too manual defeats the purpose. Mitigation: User controls iteration decisions, agents control phase progression.
- **Handoff context preservation**: Must pass enough context between agents. Mitigation: Agents read todo+plan files, all context persisted there.
- **Workflow complexity**: Multiple phases might be overwhelming. Mitigation: Each phase has clear purpose, automatic progression minimizes friction.
- **Edge case coverage**: Can't predict all scenarios. Mitigation: Agents ask when uncertain, workflow is flexible.
