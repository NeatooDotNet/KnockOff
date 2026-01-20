# Unified Agent Workflow Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the unified agent workflow integrating plan mode, knockoff-architect, knockoff-developer, and project-todos skill for systematic, complete implementations.

**Architecture:** Enhance existing skill and agent markdown files with new sections defining workflow behavior, verification checklists, and handoff mechanisms. No new code files required - purely documentation and process changes.

**Tech Stack:** Markdown documentation, YAML frontmatter

---

## Task 1: Add Role-Specific Guidance to project-todos Skill

**Files:**
- Modify: `C:\Users\keith\.claude\skills\project-todos\skill.md:443` (after Summary section)

**Step 1: Add "For Architects" section to skill**

Insert after the "Summary" section (around line 443):

```markdown

---

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
```

**Step 2: Add "For Developers" section to skill**

Continue inserting:

```markdown

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

**Step 3: Verify the changes**

Run: `grep -n "For Architects" C:\Users\keith\.claude\skills\project-todos\skill.md`

Expected: Shows line number where section was added

**Step 4: Commit**

```bash
git add .claude/skills/project-todos/skill.md
git commit -m "docs: add role-specific guidance for architects and developers to project-todos skill"
```

---

## Task 2: Add Enhanced Plan Template Sections to project-todos Skill

**Files:**
- Modify: `C:\Users\keith\.claude\skills\project-todos\skill.md` (in "Creating a Plan" section)

**Step 1: Find plan template documentation**

Run: `grep -n "Step 4: Write the Plan File" C:\Users\keith\.claude\skills\project-todos\skill.md`

Expected: Shows line number (around 165)

**Step 2: Add new template sections documentation**

After the existing plan template fields documentation (around line 180), insert:

```markdown

**New Template Sections (for workflow integration):**

Plans created by knockoff-architect should include these additional sections:

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
```\`\`\`
```

**Step 3: Add plan status documentation**

Insert after the new template sections:

```markdown

**Plan Status Values:**

Use these status values to track workflow progress:
- `Draft` - Initial plan creation (default)
- `Draft (Architect)` - Architect working on design
- `Under Review (Developer)` - Developer reviewing architect's design
- `Concerns Raised` - Developer found issues, awaiting user decision
- `Ready for Implementation` - Approved, implementation contract created
- `In Progress` - Developer implementing
- `Complete` - All evidence provided, moved to completed/

**Status field location:** In the YAML-style header at the top of plan files.
```

**Step 4: Verify the changes**

Run: `grep -n "Plan Status Values" C:\Users\keith\.claude\skills\project-todos\skill.md`

Expected: Shows line number where section was added

**Step 5: Commit**

```bash
git add .claude/skills/project-todos/skill.md
git commit -m "docs: add enhanced plan template sections and status values to project-todos skill"
```

---

## Task 3: Add Workflow Integration to knockoff-architect Agent

**Files:**
- Modify: `C:\Users\keith\source\repos\neatoodotnet\KnockOff\.claude\agents\knockoff-architect.md:271` (after "Remember" section)

**Step 1: Add Workflow Integration section**

Insert after the "Remember" section (around line 271):

```markdown

---

## Workflow Integration

### When Invoked After Plan Mode

You will receive a plan file that plan mode created. Your job:

1. **Read the existing plan** - Understand the initial design
2. **Read the linked todo** - Understand the user's core request
3. **Perform deep codebase analysis** - Study relevant files, patterns
4. **Enhance the plan** with KnockOff-specific architecture:
   - Complete "Architectural Verification" section
   - Analyze all three stub patterns (Standalone, Inline Interface, Inline Class)
   - Assess breaking changes
   - Check pattern consistency
   - Define test strategy
   - Document edge cases
   - List files examined

5. **Update plan status** to "Under Review (Developer)"
6. **Update todo Last Updated** date
7. **Hand off to knockoff-developer** - Automatically invoke using Task tool

### Architectural Verification Checklist

Before handing off, you MUST complete:
- [ ] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [ ] Breaking changes assessment completed
- [ ] Pattern consistency verified
- [ ] Diagnostic requirements identified
- [ ] Test strategy defined
- [ ] Edge cases documented
- [ ] Codebase deep-dive completed (document files examined)

### After Developer Raises Concerns

If developer finds issues and user asks you to address them:
1. Read "Developer Review" section of the plan
2. Address each concern with architectural solutions
3. Update the plan with resolutions
4. Clear or mark concerns as addressed
5. Hand back to developer for re-review using Task tool

### Handoff Mechanism

**To invoke knockoff-developer:**

```markdown
I've completed the architectural design and verification checklist.

[Use Task tool]
- subagent_type: knockoff-developer
- prompt: "Review the plan at docs/plans/[name].md. Perform deep analysis and document concerns or create implementation contract if ready."
```

**When returning from developer concerns:**

```markdown
I've addressed all developer concerns in the plan.

[Use Task tool]
- subagent_type: knockoff-developer
- prompt: "Re-review docs/plans/[name].md after architect addressed concerns in Developer Review section."
```
```

**Step 2: Verify the changes**

Run: `grep -n "Workflow Integration" C:\Users\keith\source\repos\neatoodotnet\KnockOff\.claude\agents\knockoff-architect.md`

Expected: Shows line number where section was added

**Step 3: Commit**

```bash
git add .claude/agents/knockoff-architect.md
git commit -m "docs: add workflow integration section to knockoff-architect agent"
```

---

## Task 4: Add Workflow Integration to knockoff-developer Agent

**Files:**
- Modify: `C:\Users\keith\source\repos\neatoodotnet\KnockOff\.claude\agents\knockoff-developer.md:347` (after "Questions to Ask" section)

**Step 1: Add Workflow Integration section**

Insert after the "Questions to Ask" section (around line 347):

```markdown

---

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
   - If concerns found: Add to "Developer Review" section of plan
   - Update plan status to "Concerns Raised"
   - Ask user: "Should I send back to architect?"
   - Wait for user decision

3. **If No Concerns**:
   - Add "Implementation Contract" section to plan
   - List every file change and test as checklist items
   - List what is explicitly OUT OF SCOPE
   - Update plan status to "Ready for Implementation"
   - Ask user: "Shall I proceed with implementation?"

### Implementation Phase

When user approves implementation:

1. **Checklist-Driven**: Work through contract items, check off each as you complete it
2. **Milestone Verification**: Run tests after each phase, document results in Implementation Progress
3. **STOP Conditions**:
   - Out-of-scope test failures → STOP immediately, report, ask user
   - Architectural discoveries that contradict design → STOP, document, ask user
4. **Evidence Collection**: Capture test output, code snippets showing feature works
5. **Completion**:
   - All checklist items checked
   - Evidence documented in Completion Evidence section
   - Move todo and plan to completed/ directories
   - Update both statuses to "Complete"

### Review Phase Checklist

Before creating implementation contract:
- [ ] Architectural verification checklist is complete
- [ ] All three patterns (Standalone, Inline Interface, Inline Class) are addressed
- [ ] Breaking changes are assessed
- [ ] Test strategy is defined
- [ ] No obvious gaps or missing considerations
- [ ] Design is implementable without major architectural changes

### Handoff Mechanism

**To send back to architect (when concerns found):**

```markdown
I've identified concerns in the Developer Review section:
[List concerns]

Should I send this back to the architect?

[If user approves, use Task tool]
- subagent_type: knockoff-architect
- prompt: "Address developer concerns in 'Developer Review' section of docs/plans/[name].md"
```

**To request implementation approval:**

```markdown
I've reviewed the plan and created an implementation contract with N tasks.

No concerns found. Shall I proceed with implementation?

[Wait for user approval before implementing]
```
```

**Step 2: Verify the changes**

Run: `grep -n "Workflow Integration" C:\Users\keith\source\repos\neatoodotnet\KnockOff\.claude\agents\knockoff-developer.md`

Expected: Shows line number where section was added

**Step 3: Commit**

```bash
git add .claude/agents/knockoff-developer.md
git commit -m "docs: add workflow integration section to knockoff-developer agent"
```

---

## Task 5: Add Plan Mode Integration to CLAUDE.md

**Files:**
- Modify: `C:\Users\keith\source\repos\neatoodotnet\KnockOff\CLAUDE.md:17` (after "TODOs and Plans" section)

**Step 1: Add Plan Mode and Project Todos section**

Insert after the "TODOs and Plans" section (around line 17):

```markdown

## Plan Mode and Project Todos

**When plan mode completes:**
1. Plan mode creates the design document through brainstorming conversation
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
- Developer reviews and either raises concerns or creates implementation contract
- After user approval, developer implements with milestone verification

**The automatic pipeline:**
```
Plan Mode → knockoff-architect → knockoff-developer → Implementation
```

This creates a seamless flow: brainstorming → formalization → architectural design → implementation planning → implementation.

**Key verification gates:**
- Architect must complete 7-item verification checklist before handoff
- Developer must create implementation contract before coding
- Developer must provide evidence (test output, code snippets) before completion
```

**Step 2: Verify the changes**

Run: `grep -n "Plan Mode and Project Todos" C:\Users\keith\source\repos\neatoodotnet\KnockOff\CLAUDE.md`

Expected: Shows line number where section was added

**Step 3: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: add plan mode and project todos workflow to CLAUDE.md"
```

---

## Task 6: Test the Complete Workflow

**Files:**
- Test: All modified documentation files
- Create: Test todo and plan files (will be deleted after testing)

**Step 1: Verify all skill and agent files are valid markdown**

Run: `Get-ChildItem -Path ".claude" -Filter "*.md" -Recurse | ForEach-Object { Write-Host $_.FullName }`

Expected: Lists all .md files in .claude directory

**Step 2: Test project-todos skill structure**

Create a test todo to verify templates work:

```bash
# This is a manual test - verify the skill can be invoked
# You would normally use /project-todos to test
```

Run: `grep -n "For Architects" .claude/skills/project-todos/skill.md`

Expected: Shows the new section exists

**Step 3: Test knockoff-architect agent structure**

Run: `grep -n "Workflow Integration" .claude/agents/knockoff-architect.md`

Expected: Shows the new section exists

**Step 4: Test knockoff-developer agent structure**

Run: `grep -n "Workflow Integration" .claude/agents/knockoff-developer.md`

Expected: Shows the new section exists

**Step 5: Test CLAUDE.md changes**

Run: `grep -n "Plan Mode and Project Todos" CLAUDE.md`

Expected: Shows the new section exists

**Step 6: Verify plan template sections in skill**

Run: `grep -n "Architectural Verification" .claude/skills/project-todos/skill.md`

Expected: Shows the enhanced template documentation

**Step 7: Update unified-agent-workflow plan status**

Edit `docs/plans/unified-agent-workflow.md`:
- Change status from "Draft" to "Ready for Implementation"
- Update Last Updated to today's date

**Step 8: Update unified-agent-workflow todo**

Edit `docs/todos/unified-agent-workflow.md`:
- Check off all 6 tasks as completed
- Update Last Updated to today's date
- Update progress log with completion note

**Step 9: Commit test verification**

```bash
git add docs/plans/unified-agent-workflow.md
git add docs/todos/unified-agent-workflow.md
git commit -m "docs: mark unified agent workflow implementation complete"
```

**Step 10: Final validation**

Run: `git log --oneline -10`

Expected: Shows all 6 commits from this implementation:
1. Add role-specific guidance to project-todos skill
2. Add enhanced plan template sections to project-todos skill
3. Add workflow integration to knockoff-architect agent
4. Add workflow integration to knockoff-developer agent
5. Add plan mode integration to CLAUDE.md
6. Mark unified agent workflow implementation complete

---

## Completion Checklist

- [ ] project-todos skill has "For Architects" section with 7-item checklist
- [ ] project-todos skill has "For Developers" section with STOP conditions
- [ ] project-todos skill has enhanced plan template sections documented
- [ ] project-todos skill has plan status values documented
- [ ] knockoff-architect agent has "Workflow Integration" section
- [ ] knockoff-architect agent has handoff mechanism to developer
- [ ] knockoff-developer agent has "Workflow Integration" section
- [ ] knockoff-developer agent has handoff mechanism to architect
- [ ] CLAUDE.md has "Plan Mode and Project Todos" section
- [ ] All changes committed to git
- [ ] unified-agent-workflow plan updated to "Ready for Implementation"
- [ ] unified-agent-workflow todo tasks checked off

---

## Edge Cases Handled

1. **User skips plan mode**: Architect agent will detect missing todo/plan and create them using project-todos skill
2. **Architectural problems during implementation**: Developer STOPS, documents issue, asks user for guidance
3. **Multiple iteration cycles**: After 2-3 cycles, developer suggests alternatives to continue refining
4. **Out-of-scope test failures**: Developer STOPS immediately, reports failures, waits for user approval
5. **User modifies in-progress plan**: Agent updates plan, logs changes, asks if re-review needed

---

## Next Steps

After implementing this plan:

1. **Test with real feature**: Use the workflow on an actual KnockOff feature request
2. **Iterate based on experience**: Adjust checklists and requirements based on what works
3. **Document lessons learned**: Update plan with any discoveries during first real usage
4. **Consider automation**: Explore ways to automate handoffs further while keeping user control
