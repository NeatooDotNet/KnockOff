---
paths:
  - "src/Generator/**"
  - "src/KnockOff/**"
---

# Production Code - Keep Design in Sync

When modifying the generator or KnockOff library, the Design projects may need updates.

## After API Changes

If you changed:
- Generated interceptor APIs
- Attribute behavior
- Stub patterns
- Method/property/indexer/event handling

Update the corresponding `src/Design/Design.Stubs/` file to reflect the change.

## Verification

```bash
cd src/Design
dotnet build
dotnet test
```

Failing Design tests indicate the documentation is now out of sync with the implementation.
