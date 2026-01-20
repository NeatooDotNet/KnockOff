# Implement Unified Agent Workflow

**Status:** Complete
**Priority:** High
**Created:** 2026-01-19
**Last Updated:** 2026-01-19

---

## Problem

Current workflow has gaps that lead to incomplete implementations:
- Plans sometimes missing key areas (pattern analysis, breaking change assessment, test strategy)
- Features sometimes partially implemented without evidence of completion
- No systematic handoff between design and implementation phases
- Agents and plan mode not formally integrated

## Solution

Formalize the integration between Claude Code plan mode, knockoff-architect agent, knockoff-developer agent, and the project-todos skill to create an automatic pipeline with multiple verification gates and evidence-based completion.

---

## Plans

- [Unified Agent Workflow Design](../plans/unified-agent-workflow.md)
- [Unified Agent Workflow Implementation Plan](../plans/2026-01-19-unified-agent-workflow-implementation.md)

---

## Tasks

- [x] Update project-todos skill with role-specific guidance
- [x] Update project-todos skill with enhanced plan template sections
- [x] Update knockoff-architect agent with workflow integration
- [x] Update knockoff-developer agent with workflow integration
- [x] Update CLAUDE.md with plan mode directive
- [x] Test the complete workflow with a sample feature request

---

## Progress Log

**2026-01-19**: Created todo and linked to design plan. Ready to begin implementation.

**2026-01-19**: Created detailed implementation plan with 6 tasks broken into bite-sized steps.

**2026-01-19**: All 6 tasks completed successfully:
- Tasks 1-2: Updated global project-todos skill with role-specific guidance and enhanced plan template sections
- Tasks 3-4: Updated knockoff-architect and knockoff-developer agents with workflow integration sections
- Task 5: Updated CLAUDE.md with plan mode directive
- Task 6: Verified all changes through systematic testing

---

## Results / Conclusions

**Workflow Successfully Integrated:**

The unified agent workflow is now fully operational. All components are properly connected:

1. **Global project-todos skill** (C:\Users\keith\.claude\skills\project-todos\SKILL.md):
   - Role-specific guidance for architects and developers (line 522)
   - Enhanced plan template with Architectural Verification, Developer Review, Implementation Contract, and Completion Evidence sections (line 186)
   - Clear plan status values and workflow documentation

2. **knockoff-architect agent** (.claude/agents/knockoff-architect.md):
   - Workflow Integration section at line 274
   - Architectural Verification Checklist requirements
   - Automatic handoff to knockoff-developer specified

3. **knockoff-developer agent** (.claude/agents/knockoff-developer.md):
   - Workflow Integration section at line 350
   - Review phase, Implementation Contract, and Evidence Collection requirements
   - STOP conditions and concern documentation specified

4. **CLAUDE.md project instructions**:
   - Plan Mode and Project Todos section at line 19
   - Documents the automatic pipeline from plan mode through implementation
   - Clarifies the seamless handoff flow

**Verification Results:**
All 6 verification commands passed successfully, confirming proper integration across global configuration and project-specific files.

**Next Steps:**
The workflow is ready for use. Future feature requests can now follow the automatic pipeline: Plan Mode → knockoff-architect → knockoff-developer → Implementation, with verification gates and evidence-based completion at each stage.
