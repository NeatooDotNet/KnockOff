---
paths:
  - "src/Design/**"
---

# Design Projects - Source of Truth

The `src/Design/` directory is the authoritative reference for KnockOff's API.

## When Answering Questions About KnockOff

1. Read the relevant `Design.Stubs/` file first
2. Trust the code and comments as ground truth
3. Look for these comment markers:
   - `DESIGN DECISION` - Why the API works this way
   - `DID NOT DO THIS` - Rejected alternatives and why
   - `GENERATOR BEHAVIOR` - What code is generated
   - `COMMON MISTAKE` - Pitfalls to avoid

## Before Modifying Design Files

```bash
cd src/Design
dotnet build
dotnet test
```

Changes that break tests indicate documentation was accurate - investigate carefully.
