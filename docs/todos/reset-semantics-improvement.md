# Reset Semantics Improvement

## Summary

Currently `Reset()` clears both tracking state AND callbacks. This may not match what Moq developers expect.

## Task List

- [ ] Research what Moq's `Reset()` does
- [ ] Update KnockOff's `Reset()` to match Moq developer expectations
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

## Proposed Solution

Match what a Moq developer would expect from `Reset()`.

## Impact

- May be breaking if current behavior changes
- Applies to all interceptor types: methods, properties, indexers, delegates

## Priority

Medium - improves familiarity for Moq developers migrating to KnockOff.
