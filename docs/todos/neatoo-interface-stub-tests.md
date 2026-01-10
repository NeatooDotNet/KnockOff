# Neatoo Interface Stub Tests Plan

## Goal

Test all Neatoo framework interfaces using KnockOff to validate that the generator correctly handles domain-driven design patterns. This serves dual purposes:

1. **Regression testing** - Ensure KnockOff handles Neatoo's interface patterns correctly
2. **Dogfooding** - Use KnockOff as intended for Neatoo-based applications

## Reference Location

Neatoo interfaces are defined in: `/home/keithvoels/neatoodotnet/Neatoo/src/Neatoo/`

## Task List

### Phase 1: Core Domain Interfaces

- [ ] INeatooObject (marker interface)
  - [ ] Inline stub
  - [ ] Standalone stub
  - [ ] Tests: marker interface compiles with no interceptors

- [ ] IValidateBase
  - [ ] Inline stub
  - [ ] Tests: Parent property tracking, IsPaused, GetProperty/TryGetProperty
  - Note: Complex interface with validation meta-properties

- [ ] IEntityBase
  - [ ] Inline stub
  - [ ] Tests: Root property, ModifiedProperties, Delete/UnDelete, Save, indexer
  - Note: Extends IValidateBase; test inheritance handling

### Phase 2: Validation & Rules

- [ ] IRule
  - [ ] Inline stub
  - [ ] Tests: Executed flag, RuleOrder, UniqueIndex, Messages collection, TriggerProperties, RunRule/OnRuleAdded methods

- [ ] IRule<T>
  - [ ] Inline stub with generic target type
  - [ ] Tests: Strongly-typed RunRule with CancellationToken

- [ ] IRuleManager
  - [ ] Inline stub
  - [ ] Tests: Rules collection, RunRules, AddRule<T>, AddRules<T>, RunRule

- [ ] IRuleManager<T>
  - [ ] Inline stub
  - [ ] Tests: AddAction, AddValidation, AddActionAsync, AddValidationAsync (multiple overloads)
  - Note: Heavy use of method overloads

- [ ] ITriggerProperty
  - [ ] Inline stub
  - [ ] Tests: PropertyName, IsMatch method

- [ ] ITriggerProperty<T>
  - [ ] Inline stub
  - [ ] Tests: GetValue with typed target

- [ ] IRuleMessage
  - [ ] Inline stub
  - [ ] Tests: RuleIndex, PropertyName, Message properties

- [ ] IRuleMessages (inherits IList<IRuleMessage>)
  - [ ] Inline stub
  - [ ] Tests: Collection operations from IList inheritance
  - Note: Tests interface inheritance flattening

### Phase 3: Built-in Rule Interfaces

- [ ] IRequiredRule
  - [ ] Inline stub
  - [ ] Tests: ErrorMessage property

- [ ] IMaxLengthRule
  - [ ] Inline stub
  - [ ] Tests: ErrorMessage, Length properties

- [ ] IMinLengthRule
  - [ ] Inline stub
  - [ ] Tests: ErrorMessage, Length properties

- [ ] IStringLengthRule
  - [ ] Inline stub
  - [ ] Tests: ErrorMessage, MinimumLength, MaximumLength properties

- [ ] IEmailAddressRule
  - [ ] Inline stub
  - [ ] Tests: ErrorMessage property

- [ ] IRegularExpressionRule
  - [ ] Inline stub
  - [ ] Tests: ErrorMessage, Pattern properties

- [ ] IRangeRule
  - [ ] Inline stub
  - [ ] Tests: ErrorMessage, Minimum, Maximum properties

- [ ] IAttributeToRule
  - [ ] Inline stub
  - [ ] Tests: GetRule<T> generic method

- [ ] IAllRequiredRulesExecuted (inherits IRule<IValidateBase>)
  - [ ] Inline stub
  - [ ] Tests: Combined interface behavior

### Phase 4: Property Interfaces

- [ ] IValidateProperty
  - [ ] Inline stub
  - [ ] Tests: Name, Value, SetValue, Task, IsBusy, IsReadOnly, AddMarkedBusy, RemoveMarkedBusy, LoadValue, WaitForTasks, GetAwaiter, Type, StringValue, IsSelfValid, IsValid, RunRules, PropertyMessages
  - Note: Large interface with many members

- [ ] IValidateProperty<T>
  - [ ] Inline stub
  - [ ] Tests: Typed Value property

- [ ] IEntityProperty
  - [ ] Inline stub
  - [ ] Tests: IsPaused, IsModified, IsSelfModified, MarkSelfUnmodified, DisplayName, ApplyPropertyInfo

- [ ] IEntityProperty<T>
  - [ ] Inline stub
  - [ ] Tests: Combined IEntityProperty + IValidateProperty<T>
  - Note: Multiple interface inheritance

- [ ] IPropertyInfo
  - [ ] Inline stub
  - [ ] Tests: PropertyInfo, Name, Type, Key, IsPrivateSetter, GetCustomAttribute<T>, GetCustomAttributes

- [ ] IPropertyInfoList
  - [ ] Inline stub
  - [ ] Tests: GetPropertyInfo, Properties, HasProperty

- [ ] IPropertyInfoList<T>
  - [ ] Inline stub
  - [ ] Tests: Inherits IPropertyInfoList

- [ ] IPropertyMessage
  - [ ] Inline stub
  - [ ] Tests: Property, Message properties

### Phase 5: Property Manager Interfaces

- [ ] IValidatePropertyManager<P>
  - [ ] Inline stub
  - [ ] Tests: IsBusy, WaitForTasks, HasProperty, GetProperty, indexer, SetProperties, IsSelfValid, IsValid, RunRules, PropertyMessages, IsPaused, PauseAllActions, ResumeAllActions, ClearAllMessages, ClearSelfMessages
  - Note: Generic interface with many members

- [ ] IEntityPropertyManager
  - [ ] Inline stub
  - [ ] Tests: IsModified, IsSelfModified, ModifiedProperties, MarkSelfUnmodified

### Phase 6: Service Interfaces

- [ ] IValidateBaseServices<T>
  - [ ] Inline stub
  - [ ] Tests: PropertyInfoList, ValidatePropertyManager, CreateRuleManager method
  - Note: Core DI service interface

- [ ] IEntityBaseServices<T>
  - [ ] Inline stub
  - [ ] Tests: EntityPropertyManager, Factory (nullable IFactorySave<T>)
  - Note: Extends IValidateBaseServices

### Phase 7: Meta Property Interfaces

- [ ] IValidateMetaProperties
  - [ ] Inline stub
  - [ ] Tests: IsBusy, WaitForTasks (with and without CancellationToken), IsValid, IsSelfValid, PropertyMessages, RunRules, ClearAllMessages, ClearSelfMessages

- [ ] IEntityMetaProperties
  - [ ] Inline stub
  - [ ] Tests: IsChild, IsModified, IsSelfModified, IsMarkedModified, IsSavable

### Phase 8: Collection Interfaces

- [ ] IValidateListBase (non-generic)
  - [ ] Inline stub
  - [ ] Tests: Parent property

- [ ] IValidateListBase<I>
  - [ ] Inline stub
  - [ ] Tests: Parent, plus inherited IList<I> and IValidateMetaProperties members
  - Note: Multiple interface inheritance chain

- [ ] IEntityListBase (non-generic)
  - [ ] Inline stub
  - [ ] Tests: Root property

- [ ] IEntityListBase<I>
  - [ ] Inline stub
  - [ ] Tests: RemoveAt overloads, plus inherited members
  - Note: Entity list with deleted item management

### Phase 9: Notification Interfaces

- [ ] INotifyNeatooPropertyChanged
  - [ ] Inline stub
  - [ ] Tests: NeatooPropertyChanged event subscription, firing with breadcrumb path
  - Note: Custom delegate type (NeatooPropertyChanged)

### Phase 10: Internal/Factory Interfaces

- [ ] IFactory (Neatoo.Internal)
  - [ ] Inline stub
  - [ ] Tests: CreateValidateProperty<P>, CreateEntityProperty<P> generic methods
  - Note: Internal factory, lower priority

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

**Structure: One test class per Neatoo interface** - Each interface gets its own file to keep tests focused and discoverable.

```
src/Tests/KnockOff.NeatooInterfaceTests/
├── KnockOff.NeatooInterfaceTests.csproj
├── CoreDomain/
│   ├── INeatooObjectTests.cs
│   ├── IValidateBaseTests.cs
│   └── IEntityBaseTests.cs
├── ValidationRules/
│   ├── IRuleTests.cs
│   ├── IRuleOfTTests.cs
│   ├── IRuleManagerTests.cs
│   ├── IRuleManagerOfTTests.cs
│   ├── ITriggerPropertyTests.cs
│   ├── ITriggerPropertyOfTTests.cs
│   ├── IRuleMessageTests.cs
│   └── IRuleMessagesTests.cs
├── BuiltInRules/
│   ├── IRequiredRuleTests.cs
│   ├── IMaxLengthRuleTests.cs
│   ├── IMinLengthRuleTests.cs
│   ├── IStringLengthRuleTests.cs
│   ├── IEmailAddressRuleTests.cs
│   ├── IRegularExpressionRuleTests.cs
│   ├── IRangeRuleTests.cs
│   ├── IAttributeToRuleTests.cs
│   └── IAllRequiredRulesExecutedTests.cs
├── Properties/
│   ├── IValidatePropertyTests.cs
│   ├── IValidatePropertyOfTTests.cs
│   ├── IEntityPropertyTests.cs
│   ├── IEntityPropertyOfTTests.cs
│   ├── IPropertyInfoTests.cs
│   ├── IPropertyInfoListTests.cs
│   ├── IPropertyInfoListOfTTests.cs
│   └── IPropertyMessageTests.cs
├── PropertyManagers/
│   ├── IValidatePropertyManagerOfPTests.cs
│   └── IEntityPropertyManagerTests.cs
├── Services/
│   ├── IValidateBaseServicesOfTTests.cs
│   └── IEntityBaseServicesOfTTests.cs
├── MetaProperties/
│   ├── IValidateMetaPropertiesTests.cs
│   └── IEntityMetaPropertiesTests.cs
├── Collections/
│   ├── IValidateListBaseTests.cs
│   ├── IValidateListBaseOfITests.cs
│   ├── IEntityListBaseTests.cs
│   └── IEntityListBaseOfITests.cs
├── Notifications/
│   └── INotifyNeatooPropertyChangedTests.cs
├── Internal/
│   └── IFactoryTests.cs
└── Generated/
    └── (source-generated stubs)
```

**Naming conventions:**
- Generic interfaces use `OfT` suffix: `IRuleOfTTests.cs` for `IRule<T>`
- Generic with named parameter uses parameter name: `IValidatePropertyManagerOfPTests.cs` for `IValidatePropertyManager<P>`
- Each test file contains both inline stub definition and tests for that interface

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

## Estimated Scope

- Neatoo interfaces: ~40 interfaces
- Average members per interface: ~5
- Tests per interface: ~4-6
- **Total: ~160-240 test cases**

---

## Current Status (2026-01-09)

**Build Status:** ~234 errors

### Blockers

1. **Generator Bug: Inherited Interface Methods with Different Parameter Types**
   - See `docs/todos/bug-inherited-interface-method-parameter-types.md`
   - Affects `IRule<T>` and similar patterns where generic and non-generic interfaces have same method name with different parameter types
   - ~84 errors in generated files

2. **Test Code Issues**
   - Tests reference properties that don't exist on interfaces (e.g., `IsPaused` on `IValidateListBase`)
   - Tests use wrong interceptor names (e.g., `IntIndexer` vs `Int32Indexer`)
   - Tests use wrong constructor signatures (e.g., `PropertyMessage`)
   - ~150 errors in test files
   - **Root cause:** Tests written without fully understanding Neatoo interface hierarchy

### Completed Fixes

1. **Generic Method Constraints Bug** - Fixed in `KnockOffGenerator.cs`
   - Generator now emits `where T : class` for explicit interface implementations when return type is `T?` and T has type constraints
   - See `docs/todos/completed/bug-generic-method-constraints-not-preserved.md`

### Next Steps

1. Fix generator bug for inherited interface methods (creates separate bug file)
2. Rewrite test files with correct understanding of Neatoo interface hierarchy
3. Verify each interface's members before writing tests

---

## Implementation Order

1. Start with **Phase 1 (Core Domain)** - foundational interfaces
2. Then **Phase 6 (Services)** - most commonly stubbed for testing
3. Then **Phase 2-3 (Rules)** - core validation behavior
4. Then **Phase 4-5 (Properties)** - property management
5. Finally **Phase 7-10** - specialized interfaces

This order ensures the most valuable stubs are available first for Neatoo application testing.
