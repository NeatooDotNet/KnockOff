# Set-Only Properties Verification

## Summary

The generator appears to support set-only properties (has `HasGetter`/`HasSetter` logic), but there are **no tests** verifying this works correctly.

## Task List

- [ ] Add test interface with set-only property
- [ ] Verify generator produces correct interceptor (only `OnSet`, `SetCount`, `LastSetValue`)
- [ ] Verify no `Value` backing field is generated (nothing to get)
- [ ] Add to properties.md documentation with real snippet (currently uses `pseudo:` marker)

## Test Case

```csharp
public interface ISetOnlyService
{
    string Output { set; }
}

[KnockOff]
public partial class SetOnlyServiceKnockOff : ISetOnlyService { }
```

## Expected Behavior

- `knockOff.Output.SetCount` - tracks setter calls
- `knockOff.Output.LastSetValue` - last value set
- `knockOff.Output.OnSet` - setter callback
- NO `knockOff.Output.Value` (no backing field)
- NO `knockOff.Output.OnGet` (no getter)

## Priority

Low - edge case, but should work correctly.
