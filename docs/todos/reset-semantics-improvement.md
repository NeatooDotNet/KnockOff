# Reset Semantics Improvement

## Summary

Currently `Reset()` clears both tracking state AND callbacks. Sometimes you want to reset tracking but keep callback behavior.

## Task List

- [ ] Add `ResetTracking()` - clears call history only
- [ ] Add `ResetCallback()` - clears OnCall/OnGet/OnSet only
- [ ] Keep `Reset()` as convenience that does both
- [ ] Update documentation

## Current Behavior

```csharp
knockOff.GetUser.OnCall = (ko, id) => new User { Id = id };
service.GetUser(1);
service.GetUser(2);

knockOff.GetUser.Reset();
// CallCount = 0
// LastCallArg = default
// OnCall = null  <-- callback also cleared!
```

## Proposed API

```csharp
// Clear tracking only, keep callbacks
knockOff.GetUser.ResetTracking();
// CallCount = 0, LastCallArg = default
// OnCall still set!

// Clear callbacks only, keep tracking
knockOff.GetUser.ResetCallback();
// CallCount unchanged
// OnCall = null

// Clear everything (existing behavior)
knockOff.GetUser.Reset();
```

## Impact

- Non-breaking (additive API)
- Applies to all interceptor types: methods, properties, indexers, delegates

## Priority

Medium - improves test ergonomics without breaking changes.
