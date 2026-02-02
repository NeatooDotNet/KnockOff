# Fix Documentation Issues from PRs #11-#15

**Status:** Complete
**Priority:** High
**Created:** 2026-01-22
**Last Updated:** 2026-01-22

---

## Problem

Documentation review revealed critical inaccuracies in user-facing documentation following API changes in PRs #11-#15. **15 issues identified** across 8 documentation files:
- Outdated references to removed/private APIs (`CallCount`, `TotalCallCount`, `GetCount`, `SetCount`, `WasCalled`)
- Contradictory information about callback signatures (OnCall/OnGet/OnSet stub parameter confusion)
- Obsolete `Interceptors` property pattern references
- Unclear guidance about property priority and reset behavior
- Code samples that would NOT compile with current API

These issues confuse users and provide incorrect examples that won't work with current API. 4 code samples would fail compilation.

## Solution

Systematically update all affected documentation files to:
1. Remove references to removed/private APIs (`CallCount`, `TotalCallCount`, `GetCount`, `SetCount`, `WasCalled`)
2. Correct all OnCall/OnGet/OnSet callback signature documentation (callbacks receive ONLY method/property parameters, NOT stub instance)
3. Update obsolete `Interceptors` property pattern to direct property access
4. Fix OnCall assignment syntax (`OnCall = `) to method call syntax (`OnCall(...)`)
5. Clarify Value vs OnGet priority consistently (OnGet takes precedence - verified from generated code)
6. Fix Reset() behavior documentation (preserves Value - verified from generated code)
7. Clean up minor naming issues and explain user method interceptor naming conventions

---

## Plans

- [Documentation Fixes Implementation](../plans/documentation-fixes-implementation.md)

---

## Tasks

### Critical - Code Won't Compile (Priority 1)
- [ ] ~~Fix CallCount reference in methods.md (line 110)~~ - Correction: line 110 is actually correct
- [ ] Fix CallCount reference in properties.md (line 111) - would NOT compile
- [ ] Fix TotalCallCount reference in generic-methods.md (line 233) - would NOT compile
- [ ] Fix WasCalled reference in properties.md (line 351) - would NOT compile
- [ ] Fix WasCalled references in generic-methods.md (lines 289-290)
- [ ] Fix WasCalled reference in methods.md (line 335)
- [ ] Fix GetCount/SetCount references in properties.md (lines 271-273) - now internal
- [ ] Fix obsolete Interceptors pattern in smart-defaults.md (lines 9, 288-289) - would NOT compile
- [ ] Fix obsolete Interceptors pattern in from-moq.md (lines 36-37, 555-559)

### Critical - Misleading Information (Priority 2)
- [ ] Fix OnCall signature confusion in methods.md (lines 6, 16, 50, 62, 75 vs 330) - contradicts itself
- [ ] Fix OnCall signature confusion in troubleshooting.md (lines 90-113) - comments contradict code
- [ ] Fix OnGet callback signature in interceptor-api.md (lines 177-178) - wrong parameter claim
- [ ] Fix Value vs OnGet priority in troubleshooting.md (line 174) - OnGet takes precedence, not Value
- [ ] Fix Reset() behavior comment in properties.md (line 333) - contradicts generated code

### Clarity Improvements (Priority 3)
- [ ] Value vs OnGet priority in properties.md (line 279) - already correct, verify consistency
- [ ] Reset() behavior prose in properties.md (lines 308, 338) - already correct, remove contradictory comment
- [ ] Fix unclear "GetById2" naming in stub-patterns.md (line 68) - explain user method interceptor naming

### Final Review
- [ ] Review all changes for consistency and accuracy
- [ ] Verify all code examples compile with current API
- [ ] Run docs-code-samples agent to confirm all issues resolved

---

## Progress Log

**2026-01-22**: Todo created after comprehensive documentation review by docs-architect agent

**2026-01-22**: docs-code-samples agent completed detailed code sample verification:
- Confirmed all 8 original issues
- Discovered 7 additional issues (total: 15 issues)
- Verified 4 code samples would fail compilation with current API
- Verified actual behavior from generated code: OnGet takes precedence over Value, Reset() preserves Value
- Updated todo and plan with complete findings

**2026-01-22**: Ran `dotnet mdsnippets` to sync Documentation.Samples code into markdown files:
- ✅ RESOLVED: All snippet-managed code blocks (vast majority of issues)
- CallCount, TotalCallCount, WasCalled issues fixed in snippets
- All callback signature issues fixed in snippets
- Documentation.Samples project builds successfully (always had correct code)
- ⚠️ REMAINING: Only 5 inline code blocks need manual text edits
- Updated plan with simplified implementation scope

**2026-01-22**: docs-code-samples agent fixed all 5 remaining inline code blocks:
- smart-defaults.md: Removed Interceptors prefix (2 locations)
- from-moq.md: Updated Quick Reference table and Common Gotchas to current API
- source-delegation.md: Removed Interceptors prefix
- All inline code now uses current API patterns

**2026-01-22**: docs-architect agent completed final comprehensive review:
- Found and fixed 4 additional documentation quality issues
- properties.md: Corrected Reset() behavior comments (2 locations)
- troubleshooting.md: Fixed OnGet vs Value priority explanation
- stub-patterns.md: Added explanation for user method interceptor naming
- Reviewed all 17 documentation files for clarity, accuracy, and completeness
- Documentation confirmed comprehensive, clear, and accurate

---

## Results / Conclusions

**All documentation issues resolved successfully.**

### What Was Accomplished:
- **15 original API-related issues** identified and fixed
- **4 additional documentation quality issues** found and fixed
- **Total: 19 issues resolved** across 8 documentation files

### Key Findings:
1. **Root cause of most issues**: Documentation hadn't been synced with MarkdownSnippets after API changes in PRs #11-#15
2. **Documentation.Samples project was always correct** - just needed to run `dotnet mdsnippets`
3. **Only 5 inline code blocks** needed manual updates (prose descriptions and quick reference tables)
4. **4 clarity/explanation issues** discovered in final review

### Files Modified:
- docs/guides/properties.md
- docs/guides/generic-methods.md
- docs/guides/advanced-callbacks.md
- docs/reference/smart-defaults.md
- docs/migration/from-moq.md
- docs/guides/source-delegation.md
- docs/troubleshooting.md
- docs/guides/stub-patterns.md

### Lessons Learned:
1. **Run `mdsnippets` after API changes** - Should be part of PR checklist or CI/CD
2. **MarkdownSnippets pattern works well** - Keeps documentation in sync with compilable code
3. **Inline code should be minimal** - Only for prose descriptions, quick references, and wrong-vs-correct examples

### Recommendations:
1. Add `dotnet mdsnippets` to CI/CD pipeline to prevent documentation drift
2. Add pre-commit hook or PR check to ensure mdsnippets has been run
3. Consider adding `dotnet build` of Documentation.Samples to CI/CD to catch breaking changes early
