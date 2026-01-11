# Neatoo Interface Stub Tests Plan

## Goal

Test all Neatoo framework interfaces using KnockOff to validate that the generator correctly handles domain-driven design patterns. This serves dual purposes:

1. **Regression testing** - Ensure KnockOff handles Neatoo's interface patterns correctly
2. **Dogfooding** - Use KnockOff as intended for Neatoo-based applications

## Reference Location

Neatoo interfaces are defined in: `/home/keithvoels/neatoodotnet/Neatoo/src/Neatoo/`

## Task List

### Phase 1: Core Domain Interfaces

- [x] INeatooObject (marker interface)
  - [x] Inline stub
  - [x] Standalone stub
  - [x] Tests: marker interface compiles with no interceptors

- [ ] IValidateBase
  - [ ] Inline stub
  - [ ] Tests: Parent property tracking, IsPaused, GetProperty/TryGetProperty
  - Note: Complex interface with validation meta-properties. Currently used as base interface but no dedicated tests.

- [ ] IEntityBase
  - [ ] Inline stub
  - [ ] Tests: Root property, ModifiedProperties, Delete/UnDelete, Save, indexer
  - Note: Extends IValidateBase; test inheritance handling. No dedicated tests yet.

### Phase 2: Validation & Rules

- [x] IRule
  - [x] Inline stub
  - [x] Tests: Executed flag, RuleOrder, UniqueIndex, Messages collection, TriggerProperties, RunRule/OnRuleAdded methods

- [x] IRule<T>
  - [x] Inline stub with generic target type
  - [x] Tests: Strongly-typed RunRule with CancellationToken

- [x] IRuleManager
  - [x] Inline stub
  - [x] Tests: Rules collection, RunRules, AddRule<T>, AddRules<T>, RunRule

- [ ] IRuleManager<T>
  - [ ] Inline stub
  - [ ] Tests: AddAction, AddValidation, AddActionAsync, AddValidationAsync (multiple overloads)
  - Note: Heavy use of method overloads. Not yet implemented.

- [x] ITriggerProperty
  - [x] Inline stub
  - [x] Tests: PropertyName, IsMatch method

- [ ] ITriggerProperty<T>
  - [ ] Inline stub
  - [ ] Tests: GetValue with typed target
  - Note: Generic variant not yet implemented separately.

- [x] IRuleMessage
  - [x] Inline stub
  - [x] Tests: RuleIndex, PropertyName, Message properties

- [x] IRuleMessages (inherits IList<IRuleMessage>)
  - [x] Inline stub
  - [x] Tests: Collection operations from IList inheritance
  - Note: Tests interface inheritance flattening

### Phase 3: Built-in Rule Interfaces

All built-in rules consolidated in `OtherBuiltInRuleTests.cs`:

- [x] IRequiredRule
  - [x] Inline stub (in IRequiredRuleTests.cs)
  - [x] Tests: ErrorMessage property, base IRule members

- [x] IMaxLengthRule
  - [x] Inline stub
  - [x] Standalone stub
  - [x] Tests: ErrorMessage, Length properties

- [x] IMinLengthRule
  - [x] Inline stub
  - [x] Standalone stub
  - [x] Tests: ErrorMessage, Length properties

- [x] IStringLengthRule
  - [x] Inline stub
  - [x] Standalone stub
  - [x] Tests: ErrorMessage, MinimumLength, MaximumLength properties

- [x] IEmailAddressRule
  - [x] Inline stub
  - [x] Standalone stub
  - [x] Tests: ErrorMessage property

- [x] IRegularExpressionRule
  - [x] Inline stub
  - [x] Standalone stub
  - [x] Tests: ErrorMessage, Pattern properties

- [x] IRangeRule
  - [x] Inline stub
  - [x] Standalone stub
  - [x] Tests: ErrorMessage, Minimum, Maximum properties

- [x] IAttributeToRule
  - [x] Inline stub
  - [x] Standalone stub
  - [x] Tests: GetRule<T> generic method

- [ ] IAllRequiredRulesExecuted (inherits IRule<IValidateBase>)
  - [ ] Inline stub
  - [ ] Tests: Combined interface behavior
  - Note: Not yet implemented.

### Phase 4: Property Interfaces

- [x] IValidateProperty
  - [x] Inline stub
  - [x] Tests: Name, Value, SetValue, Task, IsBusy, IsReadOnly, AddMarkedBusy, RemoveMarkedBusy, LoadValue, WaitForTasks, GetAwaiter, Type, StringValue, IsSelfValid, IsValid, RunRules, PropertyMessages
  - Note: Large interface with many members

- [ ] IValidateProperty<T>
  - [ ] Inline stub
  - [ ] Tests: Typed Value property
  - Note: Covered by IValidatePropertyOfStringStub in IValidatePropertyTests but no dedicated generic tests.

- [x] IEntityProperty
  - [x] Inline stub
  - [x] Tests: IsPaused, IsModified, IsSelfModified, MarkSelfUnmodified, DisplayName, ApplyPropertyInfo

- [ ] IEntityProperty<T>
  - [ ] Inline stub
  - [ ] Tests: Combined IEntityProperty + IValidateProperty<T>
  - Note: Has generated stub (EntityPropertyOfStringStub) but limited test coverage.

- [x] IPropertyInfo
  - [x] Inline stub
  - [x] Tests: PropertyInfo, Name, Type, Key, IsPrivateSetter, GetCustomAttribute<T>, GetCustomAttributes

- [ ] IPropertyInfoList
  - [ ] Inline stub
  - [ ] Tests: GetPropertyInfo, Properties, HasProperty
  - Note: Has generated stub but no dedicated test file.

- [ ] IPropertyInfoList<T>
  - [ ] Inline stub
  - [ ] Tests: Inherits IPropertyInfoList
  - Note: Has generated stub but no dedicated test file.

- [ ] IPropertyMessage
  - [ ] Inline stub
  - [ ] Tests: Property, Message properties
  - Note: Not yet implemented.

### Phase 5: Property Manager Interfaces

- [x] IValidatePropertyManager<P>
  - [x] Inline stub
  - [x] Tests: IsBusy, WaitForTasks, HasProperty, GetProperty, indexer, SetProperties, IsSelfValid, IsValid, RunRules, PropertyMessages, IsPaused, PauseAllActions, ResumeAllActions, ClearAllMessages, ClearSelfMessages
  - Note: Generic interface with many members

- [x] IEntityPropertyManager
  - [x] Inline stub
  - [x] Tests: IsModified, IsSelfModified, ModifiedProperties, MarkSelfUnmodified

### Phase 6: Service Interfaces

- [ ] IValidateBaseServices<T>
  - [x] Generated stub exists
  - [ ] Tests: PropertyInfoList, ValidatePropertyManager, CreateRuleManager method
  - Note: Core DI service interface. Stub generated, tests not written.

- [ ] IEntityBaseServices<T>
  - [x] Generated stub exists
  - [ ] Tests: EntityPropertyManager, Factory (nullable IFactorySave<T>)
  - Note: Extends IValidateBaseServices. Stub generated, tests not written.

### Phase 7: Meta Property Interfaces

- [x] IValidateMetaProperties
  - [x] Inline stub
  - [x] Tests: IsBusy, WaitForTasks (with and without CancellationToken), IsValid, IsSelfValid, PropertyMessages, RunRules, ClearAllMessages, ClearSelfMessages

- [x] IEntityMetaProperties
  - [x] Inline stub
  - [x] Tests: IsChild, IsModified, IsSelfModified, IsMarkedModified, IsSavable

### Phase 8: Collection Interfaces

- [x] IValidateListBase (non-generic)
  - [x] Inline stub
  - [x] Tests: Parent property

- [ ] IValidateListBase<I>
  - [x] Generated stub exists
  - [ ] Tests: Parent, plus inherited IList<I> and IValidateMetaProperties members
  - Note: Multiple interface inheritance chain. Stub generated, limited test coverage.

- [x] IEntityListBase (non-generic)
  - [x] Inline stub
  - [x] Tests: Root property

- [ ] IEntityListBase<I>
  - [x] Generated stub exists
  - [ ] Tests: RemoveAt overloads, plus inherited members
  - Note: Entity list with deleted item management. Stub generated, limited test coverage.

### Phase 9: Notification Interfaces

- [x] INotifyNeatooPropertyChanged
  - [x] Inline stub
  - [x] Tests: NeatooPropertyChanged event subscription, firing with breadcrumb path
  - Note: Custom delegate type (NeatooPropertyChanged)

### Phase 10: Internal/Factory Interfaces

- [ ] IFactory (Neatoo.Internal)
  - [ ] Inline stub
  - [ ] Tests: CreateValidateProperty<P>, CreateEntityProperty<P> generic methods
  - Note: Internal factory, lower priority. Not yet implemented.

---

## Special Considerations

### Complex Patterns to Test

1. **Deep interface inheritance** - IEntityBase -> IValidateBase -> INeatooObject -> IValidateMetaProperties
2. **Generic constraints** - Many interfaces have `where T : IValidateBase` or similar
3. **Nullable properties** - Factory property on IEntityBaseServices is nullable
4. **Custom delegate types** - INotifyNeatooPropertyChanged uses NeatooPropertyChanged delegate
5. **Multiple generic parameters** - Some interfaces like IRuleManager<T> with method overloads
6. **Collection inheritance** - IRuleMessages extends IList<IRuleMessage>

### Generator Capabilities Being Tested

| Pattern | Interfaces |
|---------|------------|
| Marker interfaces | INeatooObject |
| Properties (get/set) | Most interfaces |
| Methods with return values | GetProperty, CreateRuleManager, etc. |
| Generic methods | GetRule<T>, AddRule<T>, GetCustomAttribute<T> |
| Indexers | IValidateBase[propertyName], IValidatePropertyManager[propertyName] |
| Events | INotifyNeatooPropertyChanged.NeatooPropertyChanged |
| Interface inheritance | Most interfaces extend others |
| Nullable reference types | Factory in IEntityBaseServices |
| Task/async members | WaitForTasks, async validation methods |

### Test Project Location

**Structure: One test class per Neatoo interface** - Each interface gets its own file to keep tests focused and discoverable. Some related interfaces are consolidated (e.g., built-in rules in `OtherBuiltInRuleTests.cs`).

```
src/Tests/KnockOff.NeatooInterfaceTests/
├── KnockOff.NeatooInterfaceTests.csproj
├── CoreDomain/
│   └── INeatooObjectTests.cs
├── ValidationRules/
│   ├── IRuleTests.cs
│   ├── IRuleOfTTests.cs
│   ├── IRuleManagerTests.cs
│   ├── ITriggerPropertyTests.cs
│   ├── IRuleMessageTests.cs
│   └── IRuleMessagesTests.cs
├── BuiltInRules/
│   ├── IRequiredRuleTests.cs
│   └── OtherBuiltInRuleTests.cs  (consolidates IMaxLengthRule, IMinLengthRule, etc.)
├── Properties/
│   ├── IValidatePropertyTests.cs
│   ├── IEntityPropertyTests.cs
│   └── IPropertyInfoTests.cs
├── PropertyManagers/
│   ├── IValidatePropertyManagerTests.cs
│   └── IEntityPropertyManagerTests.cs
├── MetaProperties/
│   ├── IValidateMetaPropertiesTests.cs
│   └── IEntityMetaPropertiesTests.cs
├── Collections/
│   ├── IValidateListBaseTests.cs
│   └── IEntityListBaseTests.cs
├── Notifications/
│   └── INotifyNeatooPropertyChangedTests.cs
└── Generated/
    └── (source-generated stubs)
```

### Dependencies

```xml
<PackageReference Include="Neatoo" Version="x.y.z" />
```

Or project reference to local Neatoo:
```xml
<ProjectReference Include="..\..\..\..\Neatoo\src\Neatoo\Neatoo.csproj" />
```

---

## Acceptance Criteria

For each interface:

1. **Compilation** - Stub compiles without errors
2. **Interceptor generation** - All members have interceptors (except marker interfaces)
3. **Call tracking** - CallCount, WasCalled work correctly
4. **Argument capture** - LastCallArgs captures method arguments
5. **Callbacks** - OnCall/OnGet/OnSet delegate to custom behavior
6. **Reset** - Reset() clears tracking state

---

## Current Status (2026-01-10)

**Build Status:** ✅ 0 errors, 0 warnings

**Test Status:** ✅ 473 tests passing

### Completed

- Generator bug for inherited interface methods with different parameter types - **Fixed**
- Generator bug for generic method constraints not preserved - **Fixed**
- All existing tests compile and pass
- Built-in rule interfaces consolidated in `OtherBuiltInRuleTests.cs`

### Remaining Work

1. **Core Domain** - IValidateBase and IEntityBase need dedicated tests
2. **Generic Variants** - IRuleManager<T>, ITriggerProperty<T>, IValidateProperty<T>, IEntityProperty<T> need tests
3. **Service Interfaces** - IValidateBaseServices<T>, IEntityBaseServices<T> have stubs but no tests
4. **Collection Generics** - IValidateListBase<I>, IEntityListBase<I> need dedicated tests
5. **Missing Interfaces** - IAllRequiredRulesExecuted, IPropertyInfoList, IPropertyMessage, IFactory

### Progress Summary

| Phase | Total Interfaces | Implemented | Remaining |
|-------|-----------------|-------------|-----------|
| 1. Core Domain | 3 | 1 | 2 |
| 2. Validation & Rules | 8 | 6 | 2 |
| 3. Built-in Rules | 9 | 8 | 1 |
| 4. Property Interfaces | 8 | 4 | 4 |
| 5. Property Managers | 2 | 2 | 0 |
| 6. Service Interfaces | 2 | 0 | 2 |
| 7. Meta Properties | 2 | 2 | 0 |
| 8. Collections | 4 | 2 | 2 |
| 9. Notifications | 1 | 1 | 0 |
| 10. Internal | 1 | 0 | 1 |
| **Total** | **40** | **26** | **14** |

---

## Implementation Order

1. Start with **Phase 1 (Core Domain)** - foundational interfaces
2. Then **Phase 6 (Services)** - most commonly stubbed for testing
3. Then **Phase 2-3 (Rules)** - core validation behavior
4. Then **Phase 4-5 (Properties)** - property management
5. Finally **Phase 7-10** - specialized interfaces

This order ensures the most valuable stubs are available first for Neatoo application testing.
