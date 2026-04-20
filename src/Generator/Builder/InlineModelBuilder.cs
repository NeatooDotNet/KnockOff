// src/Generator/Builder/InlineModelBuilder.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using KnockOff.Model.Inline;
using KnockOff.Model.Shared;
using Microsoft.CodeAnalysis;

namespace KnockOff.Builder;

/// <summary>
/// Transforms InlineStubClassInfo into InlineGenerationUnit.
/// All decision logic for "what to generate" lives here.
/// </summary>
internal static class InlineModelBuilder
{
    /// <summary>
    /// Suffix added to generic interceptor property names when a method group has both generic and non-generic overloads.
    /// </summary>
    private const string GenericSuffix = "Generic";

    public static InlineGenerationUnit Build(InlineStubClassInfo info)
    {
        var interfaceStubs = new List<InlineInterfaceStubModel>();
        var delegateStubs = new List<InlineDelegateStubModel>();
        var classStubs = new List<InlineClassStubModel>();
        var partialProperties = new List<InlinePartialPropertyModel>();
        var hasGenericMethods = false;

        // Build interface stubs
        foreach (var iface in info.Interfaces)
        {
            var stubModel = BuildInterfaceStub(iface);
            interfaceStubs.Add(stubModel);
            if (stubModel.HasGenericMethods)
                hasGenericMethods = true;
        }

        // Build delegate stubs
        foreach (var del in info.Delegates)
        {
            delegateStubs.Add(BuildDelegateStub(del));
        }

        // Build class stubs
        foreach (var cls in info.Classes)
        {
            var classStub = BuildClassStub(cls);
            classStubs.Add(classStub);
            if (classStub.GenericMethodHandlers.Count > 0)
                hasGenericMethods = true;
        }

        // Build partial properties
        foreach (var prop in info.PartialProperties)
        {
            var fieldName = $"__{prop.PropertyName}__Backing";
            partialProperties.Add(new InlinePartialPropertyModel(
                PropertyName: prop.PropertyName,
                StubTypeName: prop.StubTypeName,
                AccessModifier: prop.AccessModifier,
                BackingFieldName: fieldName));
        }

        // Build containing types
        var containingTypes = info.ContainingTypes.Select(ct => new ContainingTypeModel(
            Keyword: ct.Keyword,
            Name: ct.Name,
            AccessModifier: ct.AccessibilityModifier)).ToEquatableArray();

        return new InlineGenerationUnit(
            ClassName: info.ClassName,
            Namespace: info.Namespace,
            ContainingTypes: containingTypes,
            InterfaceStubs: interfaceStubs.ToEquatableArray(),
            DelegateStubs: delegateStubs.ToEquatableArray(),
            ClassStubs: classStubs.ToEquatableArray(),
            PartialProperties: partialProperties.ToEquatableArray(),
            HasGenericMethods: hasGenericMethods);
    }

    #region Interface Stub Building

    private static InlineInterfaceStubModel BuildInterfaceStub(InterfaceInfo iface)
    {
        var stubClassName = iface.StubClassName;
        var typeParamList = SymbolHelpers.FormatTypeParameterList(iface.TypeParameters);
        var constraints = SymbolHelpers.FormatTypeConstraints(iface.TypeParameters);
        var constraintClause = string.IsNullOrEmpty(constraints) ? "" : $" {constraints}";

        // For open generic, replace the empty <> in FullName with actual type params
        var baseType = iface.IsOpenGeneric && typeParamList.Length > 0
            ? SymbolHelpers.ReplaceUnboundGeneric(iface.FullName, typeParamList)
            : iface.FullName;

        // Group methods by name for overload handling
        var (methodGroups, memberKeyToGroupName) = GroupMethodsByName(iface.Members.Where(m => !m.IsProperty && !m.IsIndexer));

        // Deduplicate property/indexer members for interceptor class generation.
        // Properties dedup by name; indexers dedup by key type (all share interceptor name "Indexer").
        // For properties: when `new`-shadowed declarations share a name, the interceptor's accessor set
        // must be the UNION of accessors across all shadowed declarations (interceptor-as-property
        // principle: one interceptor per property name). Explicit interface implementations continue to
        // use each declaration's own accessor set.
        var propertyIndexByName = new Dictionary<string, int>();
        var processedIndexerKeys = new HashSet<string>();
        var deduplicatedPropertyMembers = new List<InterfaceMemberInfo>();
        foreach (var member in iface.Members)
        {
            if (member.IsProperty || member.IsIndexer)
            {
                if (member.IsIndexer)
                {
                    // Deduplicate indexers by their key type signature (different key types = different indexers)
                    var indexerKey = string.Join(",", member.IndexerParameters.Select(p => p.Type));
                    if (processedIndexerKeys.Add(indexerKey))
                    {
                        deduplicatedPropertyMembers.Add(member);
                    }
                }
                else
                {
                    if (propertyIndexByName.TryGetValue(member.Name, out var existingIndex))
                    {
                        var existing = deduplicatedPropertyMembers[existingIndex];
                        deduplicatedPropertyMembers[existingIndex] = existing with
                        {
                            HasGetter = existing.HasGetter || member.HasGetter,
                            HasSetter = existing.HasSetter || member.HasSetter,
                        };
                    }
                    else
                    {
                        propertyIndexByName[member.Name] = deduplicatedPropertyMembers.Count;
                        deduplicatedPropertyMembers.Add(member);
                    }
                }
            }
        }

        // Deduplicate events by name
        var processedEventNames = new HashSet<string>();
        var deduplicatedEvents = new List<EventMemberInfo>();
        foreach (var evt in iface.Events)
        {
            if (processedEventNames.Add(evt.Name))
            {
                deduplicatedEvents.Add(evt);
            }
        }

        // Build models for interceptor classes
        var properties = new List<InlinePropertyModel>();
        var indexers = new List<InlineIndexerModel>();
        var methods = new List<UnifiedMethodInterceptorModel>();
        var genericHandlers = new List<InlineGenericMethodHandlerModel>();
        var events = new List<InlineEventModel>();

        // Build type parameters model
        var typeParameters = iface.TypeParameters.Select(tp => new TypeParameterModel(
            Name: tp.Name,
            Constraints: string.Join(", ", tp.Constraints))).ToEquatableArray();

        // Build property and indexer models
        foreach (var member in deduplicatedPropertyMembers)
        {
            if (member.IsIndexer)
            {
                indexers.Add(BuildIndexerModel(member, stubClassName, typeParamList, constraintClause));
            }
            else
            {
                properties.Add(BuildPropertyModel(member, stubClassName, typeParamList, constraintClause));
            }
        }

        // Build method models (handling mixed groups)
        foreach (var group in methodGroups.Values)
        {
            if (IsMixedMethodGroup(group))
            {
                var (nonGenericGroup, genericGroup) = SplitMixedGroup(group);
                if (nonGenericGroup is not null)
                {
                    methods.Add(BuildMethodModel(nonGenericGroup, stubClassName, typeParamList, constraintClause, iface));
                }
                if (genericGroup is not null)
                {
                    genericHandlers.Add(BuildGenericMethodHandlerModel(genericGroup, stubClassName, typeParamList, constraintClause, iface.TypeParameters));
                }
            }
            else if (group.Overloads.Any(o => o.IsGenericMethod))
            {
                genericHandlers.Add(BuildGenericMethodHandlerModel(group, stubClassName, typeParamList, constraintClause, iface.TypeParameters));
            }
            else
            {
                methods.Add(BuildMethodModel(group, stubClassName, typeParamList, constraintClause, iface));
            }
        }

        // Build event models
        foreach (var evt in deduplicatedEvents)
        {
            events.Add(BuildEventModel(evt, stubClassName, typeParamList, constraintClause));
        }

        // Build indexer access map: interceptor name -> property access expression (no container)
        var indexerAccessMap = new Dictionary<string, string>();
        foreach (var idx in indexers)
        {
            if (!indexerAccessMap.ContainsKey(idx.IndexerName))
                indexerAccessMap[idx.IndexerName] = idx.IndexerName;
        }

        // Build interceptor properties
        var interceptorProperties = BuildInterceptorProperties(
            deduplicatedPropertyMembers, methodGroups, deduplicatedEvents,
            stubClassName, typeParamList, indexers);

        // Build implementations with indexer access map
        var implementations = BuildImplementations(iface, methodGroups, memberKeyToGroupName, baseType, typeParamList, indexerAccessMap);

        var hasGenericMethods = genericHandlers.Count > 0 || iface.Members.Any(m => m.IsGenericMethod);

        // Build source providers for Source(T) methods
        var sourceProviders = BuildSourceProviders(iface, properties, indexers, methods, methodGroups, stubClassName, typeParamList);

        return new InlineInterfaceStubModel(
            StubClassName: stubClassName,
            InterfaceFullName: iface.FullName,
            BaseType: baseType,
            TypeParameters: typeParameters,
            IsOpenGeneric: iface.IsOpenGeneric,
            Strict: iface.Strict,
            HasGenericMethods: hasGenericMethods,
            Properties: properties.ToEquatableArray(),
            Indexers: indexers.ToEquatableArray(),
            Methods: methods.ToEquatableArray(),
            GenericMethodHandlers: genericHandlers.ToEquatableArray(),
            Events: events.ToEquatableArray(),
            InterceptorProperties: interceptorProperties,
            Implementations: implementations,
            SourceProviders: sourceProviders,
            Accessibility: iface.Accessibility);
    }

    private static InlinePropertyModel BuildPropertyModel(
        InterfaceMemberInfo member,
        string stubClassName,
        string typeParamList,
        string constraintClause)
    {
        var interceptClassName = $"{stubClassName}_{member.Name}Interceptor";

        // For open generics, replace <> with actual type parameters
        var declaringInterface = typeParamList.Length > 0
            ? SymbolHelpers.ReplaceUnboundGeneric(member.DeclaringInterfaceFullName, typeParamList)
            : member.DeclaringInterfaceFullName;

        return new InlinePropertyModel(
            InterceptorClassName: interceptClassName,
            PropertyName: member.Name,
            ReturnType: member.ReturnType,
            NullableReturnType: MakeNullable(member.ReturnType),
            HasGetter: member.HasGetter,
            HasSetter: member.HasSetter,
            IsInitOnly: member.IsInitOnly,
            StubClassName: $"Stubs.{stubClassName}{typeParamList}",
            TypeParameterList: typeParamList,
            ConstraintClauses: constraintClause,
            DeclaringInterface: declaringInterface,
            ReturnsByRef: member.ReturnsByRef,
            ReturnsByRefReadonly: member.ReturnsByRefReadonly,
            IsRefStructType: member.IsRefStructReturn);
    }

    private static InlineIndexerModel BuildIndexerModel(
        InterfaceMemberInfo member,
        string stubClassName,
        string typeParamList,
        string constraintClause)
    {
        // All indexers share a single interceptor class named "Indexer"
        var indexerName = "Indexer";
        var interceptClassName = $"{stubClassName}_{indexerName}Interceptor";

        var keyType = member.IndexerParameters.Count == 1
            ? member.IndexerParameters.GetArray()![0].Type
            : $"({string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"))})";

        var paramSig = string.Join(", ", member.IndexerParameters.Select(p => $"{GetRefKindPrefix(p.RefKind)}{p.Type} {p.Name}"));
        var paramTypes = string.Join(", ", member.IndexerParameters.Select(p => p.Type));
        var keyExpr = member.IndexerParameters.Count == 1
            ? member.IndexerParameters.GetArray()![0].Name
            : $"({string.Join(", ", member.IndexerParameters.Select(p => p.Name))})";

        // For open generics, replace <> with actual type parameters
        var declaringInterface = typeParamList.Length > 0
            ? SymbolHelpers.ReplaceUnboundGeneric(member.DeclaringInterfaceFullName, typeParamList)
            : member.DeclaringInterfaceFullName;

        // Compute KeyTypeFriendlyName for type-suffixed invoke methods in multi-indexer
        var keyTypeFriendlyName = UnifiedInterceptorBuilder.GetIndexerKeyTypeFriendlyName(member.IndexerParameters);

        return new InlineIndexerModel(
            InterceptorClassName: interceptClassName,
            IndexerName: indexerName,
            ReturnType: member.ReturnType,
            KeyType: keyType,
            NullableKeyType: MakeNullable(keyType),
            HasGetter: member.HasGetter,
            HasSetter: member.HasSetter,
            ParameterSignature: paramSig,
            ParameterTypes: paramTypes,
            KeyExpression: keyExpr,
            StubClassName: $"Stubs.{stubClassName}{typeParamList}",
            TypeParameterList: typeParamList,
            ConstraintClauses: constraintClause,
            DeclaringInterface: declaringInterface,
            KeyTypeFriendlyName: keyTypeFriendlyName,
            IsInitOnly: member.IsInitOnly,
            ReturnsByRef: member.ReturnsByRef,
            ReturnsByRefReadonly: member.ReturnsByRefReadonly);
    }

    private static UnifiedMethodInterceptorModel BuildMethodModel(
        MethodGroupInfo group,
        string stubClassName,
        string typeParamList,
        string constraintClause,
        InterfaceInfo iface)
    {
        var interceptClassName = $"{stubClassName}_{group.Name}Interceptor";
        var stubClassRef = $"Stubs.{stubClassName}{typeParamList}";

        // Find the declaring interface for this method group (from the first non-generic member)
        var declaringInterface = "";
        var member = iface.Members.FirstOrDefault(m =>
            !m.IsProperty && !m.IsIndexer && m.Name == group.Name && !m.IsGenericMethod);
        if (member != null)
        {
            // For open generics, replace <> with actual type parameters
            declaringInterface = typeParamList.Length > 0
                ? SymbolHelpers.ReplaceUnboundGeneric(member.DeclaringInterfaceFullName, typeParamList)
                : member.DeclaringInterfaceFullName;
        }

        // Build MethodSignatureInfo for each overload
        var signatures = new List<MethodSignatureInfo>();
        foreach (var overload in group.Overloads)
        {
            // Skip generic overloads - they're handled separately
            if (overload.IsGenericMethod)
                continue;

            var parameters = (overload.Parameters.GetArray() ?? Array.Empty<ParameterInfo>())
                .Select(p => new ParameterModel(
                    Name: p.Name,
                    EscapedName: EscapeIdentifier(p.Name),
                    Type: p.Type,
                    NullableType: MakeNullable(p.Type),
                    RefKind: p.RefKind,
                    RefPrefix: GetRefKindPrefix(p.RefKind),
                    XmlDoc: p.XmlDoc,
                    IsRefStruct: p.IsRefStruct,
                    IsScoped: p.IsScoped))
                .ToEquatableArray();

            var trackableParams = UnifiedInterceptorBuilder.GetTrackableParameters(parameters);
            var hasRefOrOut = parameters.Any(p => p.RefKind == RefKind.Ref || p.RefKind == RefKind.Out);
            var hasRefStruct = parameters.Any(p => p.IsRefStruct) || overload.IsRefStructReturn;

            // Determine default expression - use per-overload return type for mixed return type groups
            var defaultExpr = overload.IsVoid ? "" : GetDefaultExpressionForReturn(overload.ReturnType, overload.IsNullable);

            // Determine ThrowsOnDefault from the member's DefaultValueStrategy.
            // Only throw when defaultExpr is "default!" (no sensible default available).
            // Async return types (Task<T>, ValueTask<T>) have meaningful defaults via Task.FromResult,
            // so they should NOT throw even if the inner type has ThrowException strategy.
            // Match the overload to its InterfaceMemberInfo by name, parameter count, and types.
            bool throwsOnDefault;
            if (overload.IsVoid || overload.IsNullable || defaultExpr != "default!")
            {
                throwsOnDefault = false;
            }
            else
            {
                var overloadParams = overload.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
                var matchingMember = iface.Members.FirstOrDefault(m =>
                    !m.IsProperty && !m.IsIndexer && m.Name == group.Name && !m.IsGenericMethod &&
                    (m.Parameters.GetArray()?.Length ?? 0) == overloadParams.Length &&
                    (overloadParams.Length == 0 || m.Parameters.Zip(overload.Parameters, (a, b) => a.Type == b.Type).All(x => x)));
                throwsOnDefault = matchingMember != null
                    ? matchingMember.DefaultStrategy == DefaultValueStrategy.ThrowException
                    : IsUninstantiableType(overload.ReturnType);
            }

            signatures.Add(new MethodSignatureInfo(
                Parameters: parameters,
                TrackableParameters: trackableParams,
                ParameterDeclarations: UnifiedInterceptorBuilder.BuildParameterDeclarations(parameters),
                ReturnType: overload.ReturnType,
                IsVoid: overload.IsVoid,
                HasRefOrOutParams: hasRefOrOut,
                DefaultExpression: defaultExpr,
                ThrowsOnDefault: throwsOnDefault,
                ReturnsByRef: overload.ReturnsByRef,
                ReturnsByRefReadonly: overload.ReturnsByRefReadonly,
                HasRefStructParameter: hasRefStruct));
        }

        // If no non-generic overloads, create empty model
        if (signatures.Count == 0)
        {
            return UnifiedInterceptorBuilder.BuildMethodInterceptor(
                interceptorClassName: interceptClassName,
                methodName: group.Name,
                declaringInterface: declaringInterface,
                ownerClassName: stubClassRef,
                ownerTypeParameters: "",
                overloads: new List<MethodSignatureInfo>
                {
                    new MethodSignatureInfo(
                        Parameters: EquatableArray<ParameterModel>.Empty,
                        TrackableParameters: EquatableArray<ParameterModel>.Empty,
                        ParameterDeclarations: "",
                        ReturnType: group.ReturnType,
                        IsVoid: group.IsVoid,
                        HasRefOrOutParams: false,
                        DefaultExpression: group.IsVoid ? "" : "default!",
                        ThrowsOnDefault: false)
                });
        }

        return UnifiedInterceptorBuilder.BuildMethodInterceptor(
            interceptorClassName: interceptClassName,
            methodName: group.Name,
            declaringInterface: declaringInterface,
            ownerClassName: stubClassRef,
            ownerTypeParameters: "",
            overloads: signatures);
    }

    private static string GetDefaultExpressionForReturn(string returnType, bool isNullable)
    {
        // Task types return completed task (check before isNullable because Task is treated as void-like)
        if (returnType == "global::System.Threading.Tasks.Task")
            return "global::System.Threading.Tasks.Task.CompletedTask";
        if (returnType == "global::System.Threading.Tasks.ValueTask")
            return "default";

        // Task<T> types return Task.FromResult(default!)
        if (returnType.StartsWith("global::System.Threading.Tasks.Task<"))
        {
            var innerType = returnType.Substring("global::System.Threading.Tasks.Task<".Length, returnType.Length - "global::System.Threading.Tasks.Task<".Length - 1);
            return $"global::System.Threading.Tasks.Task.FromResult<{innerType}>(default!)";
        }

        // ValueTask<T> types return ValueTask with default result
        if (returnType.StartsWith("global::System.Threading.Tasks.ValueTask<"))
        {
            var innerType = returnType.Substring("global::System.Threading.Tasks.ValueTask<".Length, returnType.Length - "global::System.Threading.Tasks.ValueTask<".Length - 1);
            return $"new global::System.Threading.Tasks.ValueTask<{innerType}>(({innerType})default!)";
        }

        if (isNullable)
            return "default!";

        return "default!";
    }

    private static bool IsUninstantiableType(string returnType)
    {
        // Interface or abstract types that can't have a default instance
        // Check for interface types (IEnumerator<T>, IEnumerable<T>, etc.)
        if (returnType.StartsWith("global::System.Collections.Generic.IEnumerable<") ||
            returnType.StartsWith("global::System.Collections.Generic.IEnumerator<") ||
            returnType.StartsWith("global::System.Collections.Generic.IAsyncEnumerable<") ||
            returnType.StartsWith("global::System.Collections.Generic.IAsyncEnumerator<") ||
            returnType.StartsWith("global::System.Collections.IEnumerable") ||
            returnType.StartsWith("global::System.Collections.IEnumerator") ||
            returnType.StartsWith("global::System.IFormattable") ||
            returnType.StartsWith("global::System.ICustomFormatter") ||
            returnType.StartsWith("global::System.IDisposable") ||
            returnType.StartsWith("global::System.Data.IDataReader"))
        {
            return true;
        }
        // Note: Task/ValueTask types are NOT included here - they have sensible defaults
        // (Task.CompletedTask, default(ValueTask), Task.FromResult, etc.)
        return false;
    }

    private static InlineGenericMethodHandlerModel BuildGenericMethodHandlerModel(
        MethodGroupInfo group,
        string stubClassName,
        string ifaceTypeParamList,
        string ifaceConstraintClause,
        EquatableArray<TypeParameterInfo> interfaceTypeParams)
    {
        var interceptClassName = $"{stubClassName}_{group.Name}Interceptor";
        var stubClassRef = $"Stubs.{stubClassName}{ifaceTypeParamList}";

        // Group generic overloads by type parameter count to support mixed arities
        // (e.g., Run<T>() and Run<TIn, TOut>(TIn input))
        var genericOverloads = group.Overloads.Where(o => o.IsGenericMethod).ToArray();
        var arityGroupsMap = genericOverloads
            .GroupBy(o => o.TypeParameters.Count)
            .OrderBy(g => g.Key);

        var arityGroups = new List<InlineGenericTypeArityGroup>();
        foreach (var arityGroup in arityGroupsMap)
        {
            // Use the first overload in this arity group as representative
            var representative = arityGroup.First();
            var typeParams = representative.TypeParameters.GetArray()!;
            var typeParamNames = string.Join(", ", typeParams.Select(tp => tp.Name));
            var typeParamCount = typeParams.Length;

            // Build constraint clauses for method type parameters
            var methodConstraintClauses = GetConstraintClauses(typeParams);

            // Get non-generic parameters
            var typeParamSet = new HashSet<string>(typeParams.Select(tp => tp.Name));
            var allParams = representative.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
            var nonGenericParams = allParams
                .Where(p => p.RefKind != RefKind.Out)
                .Where(p => !IsGenericParameterType(p.Type, typeParamSet))
                .ToArray();

            // Build the dictionary key type
            var keyType = typeParamCount == 1
                ? "global::System.Type"
                : $"({string.Join(", ", typeParams.Select(_ => "global::System.Type"))})";

            var keyConstruction = typeParamCount == 1
                ? $"typeof({typeParams[0].Name})"
                : $"({string.Join(", ", typeParams.Select(tp => $"typeof({tp.Name})"))})";

            // Build delegate signature (no stub parameter)
            var delegateReturnType = representative.IsVoid ? "void" : representative.ReturnType;
            var delegateParams = new List<string>();
            foreach (var p in allParams)
            {
                delegateParams.Add($"{GetRefKindPrefix(p.RefKind)}{p.Type} {p.Name}");
            }
            var delegateParamList = string.Join(", ", delegateParams);
            var delegateSignature = representative.IsVoid
                ? $"public delegate void {group.Name}Delegate({delegateParamList});"
                : $"public delegate {delegateReturnType} {group.Name}Delegate({delegateParamList});";

            // LastCallArg/Args types
            string? lastCallArgType = null;
            string? lastCallArgsType = null;
            if (nonGenericParams.Length == 1)
            {
                lastCallArgType = MakeNullable(nonGenericParams[0].Type);
            }
            else if (nonGenericParams.Length > 1)
            {
                lastCallArgsType = "(" + string.Join(", ", nonGenericParams.Select(p => $"{p.Type} {p.Name}")) + ")?";
            }

            // Build non-generic parameters model
            var nonGenericParamModels = nonGenericParams.Select(p => new ParameterModel(
                Name: p.Name,
                EscapedName: EscapeIdentifier(p.Name),
                Type: p.Type,
                NullableType: MakeNullable(p.Type),
                RefKind: p.RefKind,
                RefPrefix: GetRefKindPrefix(p.RefKind),
                XmlDoc: p.XmlDoc,
                IsRefStruct: p.IsRefStruct,
                IsScoped: p.IsScoped)).ToEquatableArray();

            // Typed handler class name: append arity count for arities > 1 when multiple arities exist
            var typedHandlerClassName = $"{group.Name}TypedHandler";
            if (typeParamCount > 1)
            {
                typedHandlerClassName = $"{group.Name}TypedHandler{typeParamCount}";
            }

            arityGroups.Add(new InlineGenericTypeArityGroup(
                TypeParameterNames: typeParamNames,
                TypeParameterCount: typeParamCount,
                KeyType: keyType,
                KeyConstruction: keyConstruction,
                ConstraintClauses: methodConstraintClauses,
                TypedHandlerClassName: typedHandlerClassName,
                DelegateSignature: delegateSignature,
                IsVoid: representative.IsVoid,
                ReturnType: representative.ReturnType,
                NonGenericParameters: nonGenericParamModels,
                LastCallArgType: lastCallArgType,
                LastCallArgsType: lastCallArgsType));
        }

        return new InlineGenericMethodHandlerModel(
            InterceptorClassName: interceptClassName,
            MethodName: group.Name,
            StubClassName: stubClassRef,
            InterfaceTypeParameterList: ifaceTypeParamList,
            InterfaceConstraintClauses: ifaceConstraintClause,
            ArityGroups: arityGroups.ToEquatableArray());
    }

    private static InlineEventModel BuildEventModel(
        EventMemberInfo evt,
        string stubClassName,
        string typeParamList,
        string constraintClause)
    {
        var interceptClassName = $"{stubClassName}_{evt.Name}Interceptor";
        // Strip trailing ? from delegate type
        var delegateType = evt.FullDelegateTypeName.TrimEnd('?');
        var (raiseParams, raiseArgs, raiseReturnType, raiseReturnsValue, usesDynamicInvoke) =
            EventBuilderHelpers.GetRaiseMethodInfo(evt);

        return new InlineEventModel(
            InterceptorClassName: interceptClassName,
            EventName: evt.Name,
            DelegateType: delegateType,
            TypeParameterList: typeParamList,
            ConstraintClauses: constraintClause,
            RaiseParameters: raiseParams,
            RaiseArguments: raiseArgs,
            RaiseReturnType: raiseReturnType,
            RaiseReturnsValue: raiseReturnsValue,
            UsesDynamicInvoke: usesDynamicInvoke);
    }

    private static EquatableArray<InlineInterceptorPropertyModel> BuildInterceptorProperties(
        List<InterfaceMemberInfo> propertyMembers,
        Dictionary<string, MethodGroupInfo> methodGroups,
        List<EventMemberInfo> events,
        string stubClassName,
        string typeParamList,
        List<InlineIndexerModel> indexers)
    {
        var properties = new List<InlineInterceptorPropertyModel>();
        var renderedIndexerProperties = new HashSet<string>();

        // Property interceptors (non-indexer properties)
        foreach (var member in propertyMembers.Where(m => !m.IsIndexer))
        {
            var interceptorType = $"{stubClassName}_{member.Name}Interceptor{typeParamList}";

            properties.Add(new InlineInterceptorPropertyModel(
                PropertyName: member.Name,
                InterceptorTypeName: interceptorType,
                NeedsNewKeyword: NeedsNewKeyword(member.Name),
                Description: $"Interceptor for {member.Name}."));
        }

        // Indexer interceptors - each gets its own property (no container)
        foreach (var indexer in indexers)
        {
            if (!renderedIndexerProperties.Add(indexer.IndexerName))
                continue;
            properties.Add(new InlineInterceptorPropertyModel(
                PropertyName: indexer.IndexerName,
                InterceptorTypeName: $"{indexer.InterceptorClassName}{typeParamList}",
                NeedsNewKeyword: false,
                Description: $"Interceptor for indexer."));
        }

        // Method interceptors
        foreach (var group in methodGroups.Values)
        {
            if (IsMixedMethodGroup(group))
            {
                // Non-generic
                var interceptorType = $"{stubClassName}_{group.Name}Interceptor{typeParamList}";
                properties.Add(new InlineInterceptorPropertyModel(
                    PropertyName: group.Name,
                    InterceptorTypeName: interceptorType,
                    NeedsNewKeyword: NeedsNewKeyword(group.Name),
                    Description: $"Interceptor for {group.Name} (non-generic overloads)."));

                // Generic
                var genericName = group.Name + GenericSuffix;
                var genericInterceptorType = $"{stubClassName}_{genericName}Interceptor{typeParamList}";
                properties.Add(new InlineInterceptorPropertyModel(
                    PropertyName: genericName,
                    InterceptorTypeName: genericInterceptorType,
                    NeedsNewKeyword: NeedsNewKeyword(genericName),
                    Description: $"Interceptor for {group.Name} (generic overloads, use .Of<T>())."));
            }
            else
            {
                var interceptorType = $"{stubClassName}_{group.Name}Interceptor{typeParamList}";
                properties.Add(new InlineInterceptorPropertyModel(
                    PropertyName: group.Name,
                    InterceptorTypeName: interceptorType,
                    NeedsNewKeyword: NeedsNewKeyword(group.Name),
                    Description: $"Interceptor for {group.Name}."));
            }
        }

        // Event interceptors (bare event names)
        foreach (var evt in events)
        {
            var interceptorType = $"{stubClassName}_{evt.Name}Interceptor{typeParamList}";
            properties.Add(new InlineInterceptorPropertyModel(
                PropertyName: evt.Name,
                InterceptorTypeName: interceptorType,
                NeedsNewKeyword: false,
                Description: $"Interceptor for {evt.Name} event."));
        }

        return properties.ToEquatableArray();
    }

    private static EquatableArray<InlineInterfaceImplementation> BuildImplementations(
        InterfaceInfo iface,
        Dictionary<string, MethodGroupInfo> methodGroups,
        Dictionary<string, string> memberKeyToGroupName,
        string baseType,
        string typeParamList,
        Dictionary<string, string> indexerAccessMap)
    {
        var implementations = new List<InlineInterfaceImplementation>();

        foreach (var member in iface.Members)
        {
            // For open generics, replace <> with actual type params in the declaring interface name
            var memberIfaceName = iface.IsOpenGeneric && typeParamList.Length > 0
                ? SymbolHelpers.ReplaceUnboundGeneric(member.DeclaringInterfaceFullName, typeParamList)
                : member.DeclaringInterfaceFullName;

            var simpleIfaceName = ExtractSimpleTypeName(memberIfaceName);

            if (member.IsIndexer)
            {
                implementations.Add(BuildIndexerImplementation(member, memberIfaceName, simpleIfaceName, iface, indexerAccessMap));
            }
            else if (member.IsProperty)
            {
                // Check for property delegation
                var delegationTarget = FindPropertyDelegationTarget(member, iface);
                implementations.Add(BuildPropertyImplementation(member, memberIfaceName, simpleIfaceName, delegationTarget));
            }
            else
            {
                // Check for method delegation
                var delegationTarget = FindDelegationTarget(member, iface);
                if (delegationTarget != null)
                {
                    implementations.Add(BuildMethodDelegationImplementation(member, memberIfaceName, simpleIfaceName, delegationTarget, baseType));
                }
                else
                {
                    // Look up the group for this specific member
                    var memberKey = GetMemberKey(member);
                    var groupName = memberKeyToGroupName[memberKey];
                    var group = methodGroups[groupName];

                    // For mixed groups, use the appropriate sub-group
                    if (IsMixedMethodGroup(group))
                    {
                        var (nonGenericGroup, genericGroup) = SplitMixedGroup(group);
                        var effectiveGroup = member.IsGenericMethod ? genericGroup! : nonGenericGroup!;
                        implementations.Add(BuildMethodImplementation(member, memberIfaceName, simpleIfaceName, effectiveGroup, iface.StubClassName));
                    }
                    else
                    {
                        implementations.Add(BuildMethodImplementation(member, memberIfaceName, simpleIfaceName, group, iface.StubClassName));
                    }
                }
            }
        }

        // Event implementations
        foreach (var evt in iface.Events)
        {
            var evtIfaceName = iface.IsOpenGeneric && typeParamList.Length > 0
                ? SymbolHelpers.ReplaceUnboundGeneric(evt.DeclaringInterfaceFullName, typeParamList)
                : evt.DeclaringInterfaceFullName;

            implementations.Add(BuildEventImplementation(evt, evtIfaceName));
        }

        return implementations.ToEquatableArray();
    }

    private static InlineInterfaceImplementation BuildPropertyImplementation(
        InterfaceMemberInfo member,
        string interfaceFullName,
        string simpleIfaceName,
        InterfaceMemberInfo? delegationTarget)
    {
        InlineDelegationTarget? delegation = null;
        if (delegationTarget != null)
        {
            delegation = new InlineDelegationTarget(
                TargetInterfaceFullName: delegationTarget.DeclaringInterfaceFullName,
                TargetMemberName: delegationTarget.Name,
                TargetReturnType: delegationTarget.ReturnType,
                CastArguments: "");
        }

        var pragmaDisable = GetSetterNullabilityAttribute(member);
        var pragmaRestore = GetSetterNullabilityRestore(member);

        return new InlineInterfaceImplementation(
            Kind: InlineMemberKind.Property,
            InterfaceFullName: interfaceFullName,
            SimpleInterfaceName: simpleIfaceName,
            MemberName: member.Name,
            ReturnType: member.ReturnType,
            IsVoid: false,
            IsInitOnly: member.IsInitOnly,
            HasGetter: member.HasGetter,
            HasSetter: member.HasSetter,
            InterceptorName: member.Name,
            ParameterDeclarations: "",
            ArgumentList: "",
            InvokeSuffix: "",  // Properties don't use invoke suffix
            RecordCallArgs: "",
            CallArgs: "this",
            DefaultExpression: "default!",
            DefaultStrategy: member.DefaultStrategy,
            IsNullable: member.IsNullable,
            ConcreteTypeForNew: member.ConcreteTypeForNew,
            SetterPragmaDisable: string.IsNullOrEmpty(pragmaDisable) ? null : pragmaDisable,
            SetterPragmaRestore: string.IsNullOrEmpty(pragmaRestore) ? null : pragmaRestore,
            TypeParameterDecl: "",
            ConstraintClauses: "",
            OfTypeAccess: "",
            NonGenericArgList: "",
            IsGenericMethod: false,
            KeyArg: null,
            DelegationTarget: delegation,
            OutParameterInitializations: EquatableArray<string>.Empty,
            ReturnsByRef: member.ReturnsByRef,
            ReturnsByRefReadonly: member.ReturnsByRefReadonly);
    }

    private static InlineInterfaceImplementation BuildIndexerImplementation(
        InterfaceMemberInfo member,
        string interfaceFullName,
        string simpleIfaceName,
        InterfaceInfo iface,
        Dictionary<string, string> indexerAccessMap)
    {
        // All indexers share a single interceptor named "Indexer"
        var indexerName = "Indexer";
        var paramList = string.Join(", ", member.IndexerParameters.Select(p => $"{GetRefKindPrefix(p.RefKind)}{p.Type} {p.Name}"));
        var argList = string.Join(", ", member.IndexerParameters.Select(p => p.Name));
        var keyArg = member.IndexerParameters.Count == 1
            ? member.IndexerParameters.GetArray()![0].Name
            : $"({argList})";

        var defaultExpr = member.IsNullable
            ? "default"
            : GetDefaultForType(member.ReturnType, member.DefaultStrategy, member.ConcreteTypeForNew);

        // Use access map to get correct interceptor path
        var interceptorAccessPath = indexerAccessMap.TryGetValue(indexerName, out var path)
            ? path
            : indexerName;

        // Compute invoke suffix for multi-indexer (type-suffixed InvokeGet/InvokeSet)
        var indexerCount = SymbolHelpers.CountIndexers(iface.Members);
        var invokeSuffix = indexerCount > 1
            ? $"_{UnifiedInterceptorBuilder.GetIndexerKeyTypeFriendlyName(member.IndexerParameters)}"
            : "";

        return new InlineInterfaceImplementation(
            Kind: InlineMemberKind.Indexer,
            InterfaceFullName: interfaceFullName,
            SimpleInterfaceName: simpleIfaceName,
            MemberName: "this",
            ReturnType: member.ReturnType,
            IsVoid: false,
            IsInitOnly: member.IsInitOnly,
            HasGetter: member.HasGetter,
            HasSetter: member.HasSetter,
            InterceptorName: interceptorAccessPath,
            ParameterDeclarations: paramList,
            ArgumentList: argList,
            InvokeSuffix: invokeSuffix,
            RecordCallArgs: argList,
            CallArgs: argList,
            DefaultExpression: defaultExpr,
            DefaultStrategy: member.DefaultStrategy,
            IsNullable: member.IsNullable,
            ConcreteTypeForNew: member.ConcreteTypeForNew,
            SetterPragmaDisable: null,
            SetterPragmaRestore: null,
            TypeParameterDecl: "",
            ConstraintClauses: "",
            OfTypeAccess: "",
            NonGenericArgList: "",
            IsGenericMethod: false,
            KeyArg: keyArg,
            DelegationTarget: null,
            OutParameterInitializations: EquatableArray<string>.Empty,
            ReturnsByRef: member.ReturnsByRef,
            ReturnsByRefReadonly: member.ReturnsByRefReadonly);
    }

    private static InlineInterfaceImplementation BuildMethodImplementation(
        InterfaceMemberInfo member,
        string interfaceFullName,
        string simpleIfaceName,
        MethodGroupInfo group,
        string stubClassName)
    {
        if (member.IsGenericMethod)
        {
            return BuildGenericMethodImplementation(member, interfaceFullName, simpleIfaceName, group, stubClassName);
        }
        else
        {
            return BuildNonGenericMethodImplementation(member, interfaceFullName, simpleIfaceName, group, stubClassName);
        }
    }

    private static InlineInterfaceImplementation BuildNonGenericMethodImplementation(
        InterfaceMemberInfo member,
        string interfaceFullName,
        string simpleIfaceName,
        MethodGroupInfo group,
        string stubClassName)
    {
        var paramList = string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
        var argList = string.Join(", ", member.Parameters.Select(p => FormatArgument(p)));

        var defaultExpr = GetDefaultForType(member.ReturnType, member.DefaultStrategy, member.ConcreteTypeForNew);

        // Build out parameter initializations
        var outParamInits = member.Parameters
            .Where(p => p.RefKind == RefKind.Out)
            .Select(p => $"{p.Name} = default!;")
            .ToEquatableArray();

        // Compute invoke suffix for multi-overload groups
        // Count unique signatures (excluding generic methods)
        var nonGenericOverloads = group.Overloads.Where(o => !o.IsGenericMethod).ToArray();
        var isMultiOverload = GetUniqueSignatureCount(nonGenericOverloads) > 1;

        var invokeSuffix = "";
        if (isMultiOverload)
        {
            // Build ParameterModel array for suffix computation
            var paramModels = member.Parameters
                .Select(p => new ParameterModel(
                    Name: p.Name,
                    EscapedName: EscapeIdentifier(p.Name),
                    Type: p.Type,
                    NullableType: MakeNullable(p.Type),
                    RefKind: p.RefKind,
                    RefPrefix: GetRefKindPrefix(p.RefKind),
                    XmlDoc: p.XmlDoc,
                    IsRefStruct: p.IsRefStruct,
                    IsScoped: p.IsScoped))
                .ToEquatableArray();
            // Use member.ReturnType (not group.ReturnType) - each member needs its own suffix
            // when overloads have different return types
            invokeSuffix = "_" + UnifiedInterceptorBuilder.GetSignatureSuffix(paramModels, member.ReturnType);
        }

        return new InlineInterfaceImplementation(
            Kind: InlineMemberKind.Method,
            InterfaceFullName: interfaceFullName,
            SimpleInterfaceName: simpleIfaceName,
            MemberName: member.Name,
            ReturnType: member.ReturnType,
            IsVoid: member.ReturnType == "void",
            IsInitOnly: false,
            HasGetter: false,
            HasSetter: false,
            InterceptorName: group.Name,
            ParameterDeclarations: paramList,
            ArgumentList: argList,
            InvokeSuffix: invokeSuffix,
            RecordCallArgs: "",  // No longer used with new Invoke pattern
            CallArgs: "",      // No longer used with new Invoke pattern
            DefaultExpression: defaultExpr,
            DefaultStrategy: member.DefaultStrategy,
            IsNullable: member.IsNullable,
            ConcreteTypeForNew: member.ConcreteTypeForNew,
            SetterPragmaDisable: null,
            SetterPragmaRestore: null,
            TypeParameterDecl: "",
            ConstraintClauses: "",
            OfTypeAccess: "",
            NonGenericArgList: "",
            IsGenericMethod: false,
            KeyArg: null,
            DelegationTarget: null,
            OutParameterInitializations: outParamInits,
            ReturnsByRef: member.ReturnsByRef,
            ReturnsByRefReadonly: member.ReturnsByRefReadonly);
    }

    private static int GetUniqueSignatureCount(MethodOverloadInfo[] overloads)
    {
        var seen = new HashSet<string>();
        foreach (var overload in overloads)
        {
            // Count unique signatures based on PARAMETERS ONLY (ignore return type).
            // Same-params-different-return (like ISet<T>.Add) count as ONE signature.
            var paramTypes = (overload.Parameters.GetArray() ?? Array.Empty<ParameterInfo>())
                .Select(p => UnifiedInterceptorBuilder.GetTypeSuffix(p.Type));
            var paramKey = paramTypes.Any() ? string.Join("_", paramTypes) : "NoParams";
            seen.Add(paramKey);
        }
        return seen.Count;
    }

    private static InlineInterfaceImplementation BuildGenericMethodImplementation(
        InterfaceMemberInfo member,
        string interfaceFullName,
        string simpleIfaceName,
        MethodGroupInfo group,
        string stubClassName)
    {
        var typeParams = member.TypeParameters.GetArray()!;
        var typeParamNames = string.Join(", ", typeParams.Select(tp => tp.Name));
        var typeParamDecl = $"<{typeParamNames}>";

        // For explicit interface implementations, only class/struct constraints are allowed (CS0460)
        var constraintClauses = GetConstraintsForExplicitImpl(typeParams, member.ReturnType);

        // Determine if explicit impl needs #nullable disable because of unconstrained nullable
        // type parameters (T? without where T : class).
        var paramArray = member.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
        var needsNullableDisable = HasUnconstrainedNullableTypeParams(typeParams, member.ReturnType, paramArray);
        var implReturnType = needsNullableDisable ? StripUnconstrainedNullableAnnotations(member.ReturnType, typeParams) : member.ReturnType;

        var paramList = needsNullableDisable
            ? string.Join(", ", member.Parameters.Select(p =>
            {
                var strippedType = StripUnconstrainedNullableAnnotations(p.Type, typeParams);
                return $"{GetRefKindPrefix(p.RefKind)}{strippedType} {p.Name}";
            }))
            : string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
        var argList = string.Join(", ", member.Parameters.Select(p => FormatArgument(p)));

        // Get non-generic parameters for RecordCall
        var typeParamSet = new HashSet<string>(typeParams.Select(tp => tp.Name));
        var nonGenericParams = member.Parameters
            .Where(p => !IsGenericParameterType(p.Type, typeParamSet))
            .ToArray();
        var nonGenericArgList = string.Join(", ", nonGenericParams.Select(p => p.Name));

        var defaultExpr = member.IsNullable ? "default!" : $"SmartDefault<{member.ReturnType.TrimEnd('?')}>(\"{member.Name}\")";

        // Build out parameter initializations
        var outParamInits = member.Parameters
            .Where(p => p.RefKind == RefKind.Out)
            .Select(p => $"{p.Name} = default!;")
            .ToEquatableArray();

        return new InlineInterfaceImplementation(
            Kind: InlineMemberKind.Method,
            InterfaceFullName: interfaceFullName,
            SimpleInterfaceName: simpleIfaceName,
            MemberName: member.Name,
            ReturnType: implReturnType,
            IsVoid: member.ReturnType == "void",
            IsInitOnly: false,
            HasGetter: false,
            HasSetter: false,
            InterceptorName: group.Name,
            ParameterDeclarations: paramList,
            ArgumentList: argList,
            InvokeSuffix: "",  // Generic methods use Of<T>() pattern, not invoke suffix
            RecordCallArgs: nonGenericArgList,
            CallArgs: argList,
            DefaultExpression: defaultExpr,
            DefaultStrategy: member.DefaultStrategy,
            IsNullable: member.IsNullable,
            ConcreteTypeForNew: member.ConcreteTypeForNew,
            SetterPragmaDisable: null,
            SetterPragmaRestore: null,
            TypeParameterDecl: typeParamDecl,
            ConstraintClauses: constraintClauses,
            OfTypeAccess: $".Of<{typeParamNames}>()",
            NonGenericArgList: nonGenericArgList,
            IsGenericMethod: true,
            KeyArg: null,
            DelegationTarget: null,
            OutParameterInitializations: outParamInits,
            ReturnsByRef: member.ReturnsByRef,
            ReturnsByRefReadonly: member.ReturnsByRefReadonly,
            NeedsNullableDisable: needsNullableDisable);
    }

    private static InlineInterfaceImplementation BuildMethodDelegationImplementation(
        InterfaceMemberInfo member,
        string interfaceFullName,
        string simpleIfaceName,
        InterfaceMemberInfo target,
        string primaryInterfaceFullName)
    {
        var baseParams = member.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
        var targetParams = target.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
        var paramList = string.Join(", ", baseParams.Select(p => FormatParameter(p)));

        // Build cast arguments
        var castArgs = new List<string>();
        for (int i = 0; i < baseParams.Length; i++)
        {
            var baseName = baseParams[i].Name;
            var baseType = baseParams[i].Type;
            var targetType = targetParams[i].Type;

            if (baseType == targetType)
                castArgs.Add(baseName);
            else
                castArgs.Add($"({targetType}){baseName}");
        }

        var delegation = new InlineDelegationTarget(
            TargetInterfaceFullName: primaryInterfaceFullName,
            TargetMemberName: target.Name,
            TargetReturnType: target.ReturnType,
            CastArguments: string.Join(", ", castArgs));

        return new InlineInterfaceImplementation(
            Kind: InlineMemberKind.Method,
            InterfaceFullName: interfaceFullName,
            SimpleInterfaceName: simpleIfaceName,
            MemberName: member.Name,
            ReturnType: member.ReturnType,
            IsVoid: member.ReturnType == "void",
            IsInitOnly: false,
            HasGetter: false,
            HasSetter: false,
            InterceptorName: "",
            ParameterDeclarations: paramList,
            ArgumentList: "",
            InvokeSuffix: "",  // Delegation methods don't use invoke suffix
            RecordCallArgs: "",
            CallArgs: "",
            DefaultExpression: "",
            DefaultStrategy: DefaultValueStrategy.Default,
            IsNullable: member.IsNullable,
            ConcreteTypeForNew: null,
            SetterPragmaDisable: null,
            SetterPragmaRestore: null,
            TypeParameterDecl: "",
            ConstraintClauses: "",
            OfTypeAccess: "",
            NonGenericArgList: "",
            IsGenericMethod: false,
            KeyArg: null,
            DelegationTarget: delegation,
            OutParameterInitializations: EquatableArray<string>.Empty,
            ReturnsByRef: member.ReturnsByRef,
            ReturnsByRefReadonly: member.ReturnsByRefReadonly);
    }

    private static InlineInterfaceImplementation BuildEventImplementation(
        EventMemberInfo evt,
        string interfaceFullName)
    {
        var delegateType = evt.FullDelegateTypeName.TrimEnd('?');

        return new InlineInterfaceImplementation(
            Kind: InlineMemberKind.Event,
            InterfaceFullName: interfaceFullName,
            SimpleInterfaceName: ExtractSimpleTypeName(interfaceFullName),
            MemberName: evt.Name,
            ReturnType: delegateType,
            IsVoid: true,
            IsInitOnly: false,
            HasGetter: false,
            HasSetter: false,
            InterceptorName: evt.Name,
            ParameterDeclarations: "",
            ArgumentList: "",
            InvokeSuffix: "",  // Events don't use invoke suffix
            RecordCallArgs: "",
            CallArgs: "",
            DefaultExpression: "",
            DefaultStrategy: DefaultValueStrategy.Default,
            IsNullable: false,
            ConcreteTypeForNew: null,
            SetterPragmaDisable: null,
            SetterPragmaRestore: null,
            TypeParameterDecl: "",
            ConstraintClauses: "",
            OfTypeAccess: "",
            NonGenericArgList: "",
            IsGenericMethod: false,
            KeyArg: null,
            DelegationTarget: null,
            OutParameterInitializations: EquatableArray<string>.Empty);
    }

    #endregion

    #region Delegate Stub Building

    private static InlineDelegateStubModel BuildDelegateStub(DelegateInfo del)
    {
        var stubClassName = del.Name;
        var interceptClassName = $"{del.Name}Interceptor";

        var typeParamList = SymbolHelpers.FormatTypeParameterList(del.TypeParameters);
        var constraints = SymbolHelpers.FormatTypeConstraints(del.TypeParameters);
        var constraintClause = string.IsNullOrEmpty(constraints) ? "" : $" {constraints}";

        var delegateType = del.IsOpenGeneric && typeParamList.Length > 0
            ? SymbolHelpers.ReplaceUnboundGeneric(del.FullName, typeParamList)
            : del.FullName;

        var typeParameters = del.TypeParameters.Select(tp => new TypeParameterModel(
            Name: tp.Name,
            Constraints: string.Join(", ", tp.Constraints))).ToEquatableArray();

        var invokeParamList = string.Join(", ", del.Parameters.Select(p => $"{p.Type} {p.Name}"));
        var invokeArgList = string.Join(", ", del.Parameters.Select(p => p.Name));

        // Call type - no stub parameter
        string callType;
        if (del.IsVoid)
        {
            callType = del.Parameters.Count == 0
                ? "global::System.Action"
                : $"global::System.Action<{string.Join(", ", del.Parameters.Select(p => p.Type))}>";
        }
        else
        {
            callType = del.Parameters.Count == 0
                ? $"global::System.Func<{del.ReturnType}>"
                : $"global::System.Func<{string.Join(", ", del.Parameters.Select(p => p.Type))}, {del.ReturnType}>";
        }

        // LastCallArg/Args types
        string? lastCallArgType = null;
        string? lastCallArgsType = null;
        if (del.Parameters.Count == 1)
        {
            lastCallArgType = MakeNullable(del.Parameters.GetArray()![0].Type);
        }
        else if (del.Parameters.Count > 1)
        {
            lastCallArgsType = $"({string.Join(", ", del.Parameters.Select(p => $"{MakeNullable(p.Type)} {p.Name}"))})?";
        }

        var parameters = del.Parameters.Select(p => new ParameterModel(
            Name: p.Name,
            EscapedName: EscapeIdentifier(p.Name),
            Type: p.Type,
            NullableType: MakeNullable(p.Type),
            RefKind: p.RefKind,
            RefPrefix: GetRefKindPrefix(p.RefKind),
            IsRefStruct: p.IsRefStruct)).ToEquatableArray();

        var defaultExpr = GetDefaultForType(del.ReturnType, DefaultValueStrategy.Default, null);

        return new InlineDelegateStubModel(
            StubClassName: stubClassName,
            InterceptorClassName: interceptClassName,
            DelegateType: delegateType,
            ReturnType: del.ReturnType,
            IsVoid: del.IsVoid,
            IsOpenGeneric: del.IsOpenGeneric,
            TypeParameters: typeParameters,
            TypeParameterList: typeParamList,
            ConstraintClauses: constraintClause,
            Parameters: parameters,
            InvokeParameterDeclarations: invokeParamList,
            InvokeArgumentList: invokeArgList,
            CallType: callType,
            LastCallArgType: lastCallArgType,
            LastCallArgsType: lastCallArgsType,
            DefaultExpression: defaultExpr,
            Accessibility: del.Accessibility);
    }

    #endregion

    #region Class Stub Building

    private static InlineClassStubModel BuildClassStub(ClassStubInfo cls)
    {
        // Delegate to ClassModelBuilder for full implementation
        return ClassModelBuilder.Build(cls);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a unique key for a method member based on its name, return type, and parameter types.
    /// </summary>
    private static string GetMemberKey(InterfaceMemberInfo member)
    {
        var paramTypes = string.Join(",", member.Parameters.Select(p => p.Type));
        return $"method:{member.ReturnType}:{member.Name}({paramTypes})";
    }

    /// <summary>
    /// Groups methods by name. All overloads share a single interceptor with multiple Call signatures.
    /// Returns both the groups dictionary (keyed by group name) and a member-to-group-name mapping.
    /// </summary>
    private static (Dictionary<string, MethodGroupInfo> Groups, Dictionary<string, string> MemberKeyToGroupName) GroupMethodsByName(IEnumerable<InterfaceMemberInfo> methods)
    {
        var groups = new Dictionary<string, MethodGroupInfo>();
        var memberKeyToGroupName = new Dictionary<string, string>();

        foreach (var g in methods.Where(m => !m.IsProperty).GroupBy(m => m.Name))
        {
            var methodName = g.Key;
            var overloads = g.ToList();

            var group = BuildMethodGroup(methodName, overloads);
            groups[methodName] = group;

            foreach (var overload in overloads)
            {
                memberKeyToGroupName[GetMemberKey(overload)] = methodName;
            }
        }

        return (groups, memberKeyToGroupName);
    }

    /// <summary>
    /// Builds a MethodGroupInfo from a list of overloads sharing the same name.
    /// </summary>
    private static MethodGroupInfo BuildMethodGroup(string groupName, List<InterfaceMemberInfo> overloads)
    {
        var first = overloads[0];

        // Build combined parameters
        var allParamNames = new Dictionary<string, (string Type, int Count, RefKind RefKind)>();
        var totalOverloads = overloads.Count;

        foreach (var overload in overloads)
        {
            foreach (var param in overload.Parameters)
            {
                if (allParamNames.TryGetValue(param.Name, out var existing))
                {
                    allParamNames[param.Name] = (existing.Type, existing.Count + 1, existing.RefKind);
                }
                else
                {
                    allParamNames[param.Name] = (param.Type, 1, param.RefKind);
                }
            }
        }

        var combinedParams = new List<CombinedParameterInfo>();
        foreach (var kvp2 in allParamNames)
        {
            var paramName = kvp2.Key;
            var paramType = kvp2.Value.Type;
            var count = kvp2.Value.Count;
            var refKind = kvp2.Value.RefKind;
            var isNullable = count < totalOverloads;
            var nullableType = isNullable ? MakeNullable(paramType) : paramType;

            combinedParams.Add(new CombinedParameterInfo(paramName, paramType, nullableType, isNullable, refKind));
        }

        var overloadInfos = overloads
            .Select(o => new MethodOverloadInfo(
                o.Parameters,
                o.ReturnType,
                o.ReturnType == "void",
                o.IsNullable,
                o.IsGenericMethod,
                o.TypeParameters,
                ReturnsByRef: o.ReturnsByRef,
                ReturnsByRefReadonly: o.ReturnsByRefReadonly,
                IsRefStructReturn: o.IsRefStructReturn))
            .ToArray();

        return new MethodGroupInfo(
            Name: groupName,
            ReturnType: first.ReturnType,
            IsVoid: first.ReturnType == "void",
            IsNullable: first.IsNullable,
            Overloads: new EquatableArray<MethodOverloadInfo>(overloadInfos),
            CombinedParameters: new EquatableArray<CombinedParameterInfo>(combinedParams.ToArray()));
    }

    private static bool IsMixedMethodGroup(MethodGroupInfo group)
    {
        var overloads = group.Overloads.GetArray() ?? Array.Empty<MethodOverloadInfo>();
        var hasGeneric = overloads.Any(o => o.IsGenericMethod);
        var hasNonGeneric = overloads.Any(o => !o.IsGenericMethod);
        return hasGeneric && hasNonGeneric;
    }

    private static (MethodGroupInfo? NonGeneric, MethodGroupInfo? Generic) SplitMixedGroup(MethodGroupInfo group)
    {
        var overloads = group.Overloads.GetArray() ?? Array.Empty<MethodOverloadInfo>();
        var nonGenericOverloads = overloads.Where(o => !o.IsGenericMethod).ToArray();
        var genericOverloads = overloads.Where(o => o.IsGenericMethod).ToArray();

        if (nonGenericOverloads.Length == 0 || genericOverloads.Length == 0)
            return (null, null);

        var nonGenericCombinedParams = BuildCombinedParametersForOverloads(nonGenericOverloads);
        var nonGenericGroup = new MethodGroupInfo(
            Name: group.Name,
            ReturnType: group.ReturnType,
            IsVoid: group.IsVoid,
            IsNullable: group.IsNullable,
            Overloads: new EquatableArray<MethodOverloadInfo>(nonGenericOverloads),
            CombinedParameters: new EquatableArray<CombinedParameterInfo>(nonGenericCombinedParams));

        var genericCombinedParams = BuildCombinedParametersForOverloads(genericOverloads);
        var genericGroup = new MethodGroupInfo(
            Name: group.Name + GenericSuffix,
            ReturnType: group.ReturnType,
            IsVoid: group.IsVoid,
            IsNullable: group.IsNullable,
            Overloads: new EquatableArray<MethodOverloadInfo>(genericOverloads),
            CombinedParameters: new EquatableArray<CombinedParameterInfo>(genericCombinedParams));

        return (nonGenericGroup, genericGroup);
    }

    private static CombinedParameterInfo[] BuildCombinedParametersForOverloads(MethodOverloadInfo[] overloads)
    {
        var allParamNames = new Dictionary<string, (string Type, int Count, RefKind RefKind)>();
        var totalOverloads = overloads.Length;

        foreach (var overload in overloads)
        {
            var parameters = overload.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
            foreach (var param in parameters)
            {
                if (allParamNames.TryGetValue(param.Name, out var existing))
                {
                    allParamNames[param.Name] = (existing.Type, existing.Count + 1, existing.RefKind);
                }
                else
                {
                    allParamNames[param.Name] = (param.Type, 1, param.RefKind);
                }
            }
        }

        var combinedParams = new List<CombinedParameterInfo>();
        foreach (var kvp in allParamNames)
        {
            var paramName = kvp.Key;
            var paramType = kvp.Value.Type;
            var count = kvp.Value.Count;
            var refKind = kvp.Value.RefKind;
            var isNullable = count < totalOverloads;
            var nullableType = isNullable ? MakeNullable(paramType) : paramType;

            combinedParams.Add(new CombinedParameterInfo(paramName, paramType, nullableType, isNullable, refKind));
        }

        return combinedParams.ToArray();
    }

    private static IEnumerable<ParameterInfo> GetInputParameters(EquatableArray<ParameterInfo> parameters) =>
        parameters.Where(p => p.RefKind != RefKind.Out);

    private static IEnumerable<CombinedParameterInfo> GetInputCombinedParameters(EquatableArray<CombinedParameterInfo> parameters) =>
        parameters.Where(p => p.RefKind != RefKind.Out);

    private static string FormatParameter(ParameterInfo p) =>
        $"{(p.IsScoped ? "scoped " : "")}{GetRefKindPrefix(p.RefKind)}{p.Type} {p.Name}";

    private static string FormatArgument(ParameterInfo p) =>
        $"{GetRefKindPrefix(p.RefKind)}{p.Name}";

    private static string GetRefKindPrefix(RefKind kind) => kind switch
    {
        RefKind.Out => "out ",
        RefKind.Ref => "ref ",
        RefKind.In => "in ",
        RefKind.RefReadOnlyParameter => "ref readonly ",
        _ => ""
    };

    private static string MakeNullable(string type)
    {
        if (type.EndsWith("?"))
            return type;
        return type + "?";
    }

    private static string EscapeIdentifier(string name)
    {
        var keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while", "value"
        };

        return keywords.Contains(name) ? $"@{name}" : name;
    }

    private static bool IsGenericParameterType(string type, HashSet<string> typeParams)
    {
        foreach (var tp in typeParams)
        {
            if (type == tp || type.Contains($"<{tp}>") || type.Contains($"<{tp},") ||
                type.Contains($", {tp}>") || type.Contains($", {tp},"))
                return true;
        }
        return false;
    }

    private static string GetConstraintClauses(TypeParameterInfo[] typeParams)
    {
        var clauses = new List<string>();
        foreach (var tp in typeParams)
        {
            if (tp.Constraints.Count > 0)
            {
                var constraintStr = string.Join(", ", tp.Constraints);
                clauses.Add($" where {tp.Name} : {constraintStr}");
            }
        }
        return string.Join("", clauses);
    }

    /// <summary>
    /// Returns true if the method has unconstrained type parameters that appear with nullable
    /// annotations (T?) in the return type or parameters. These need special handling in
    /// explicit interface implementations: the ? must be stripped and nullable context disabled.
    /// </summary>
    private static bool HasUnconstrainedNullableTypeParams(TypeParameterInfo[] typeParams, string returnType, ParameterInfo[] parameters)
    {
        foreach (var tp in typeParams)
        {
            if (tp.IsKnownReferenceType)
                continue;

            var constraintArray = tp.Constraints.GetArray() ?? Array.Empty<string>();
            if (constraintArray.Contains("struct") || constraintArray.Contains("class"))
                continue;

            // Check if this unconstrained type parameter appears with ? in return type or parameters
            var nullableForm = $"{tp.Name}?";
            if (returnType.Contains(nullableForm))
                return true;
            foreach (var p in parameters)
            {
                if (p.Type.Contains(nullableForm))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Strips nullable annotations (?) from type parameter references that are NOT known to be
    /// reference types. For unconstrained T?, C# 9+ uses T? as "default value" annotation in
    /// interface declarations, but in explicit implementations without "where T : class" the
    /// compiler interprets T? as Nullable&lt;T&gt; which is invalid. Stripping the ? makes the
    /// explicit impl match the interface correctly.
    /// </summary>
    private static string StripUnconstrainedNullableAnnotations(string typeString, TypeParameterInfo[] typeParams)
    {
        var result = typeString;
        foreach (var tp in typeParams)
        {
            if (tp.IsKnownReferenceType)
                continue;

            var constraintArray = tp.Constraints.GetArray() ?? Array.Empty<string>();
            if (constraintArray.Contains("struct") || constraintArray.Contains("class"))
                continue;

            // Replace T? with T for this unconstrained type parameter
            result = result.Replace($"{tp.Name}?", tp.Name);
        }
        return result;
    }

    private static string GetConstraintsForExplicitImpl(TypeParameterInfo[] typeParams, string returnType)
    {
        var clauses = new List<string>();
        foreach (var tp in typeParams)
        {
            var constraintArray = tp.Constraints.GetArray() ?? Array.Empty<string>();

            if (constraintArray.Contains("struct"))
            {
                clauses.Add($" where {tp.Name} : struct");
                continue;
            }

            if (constraintArray.Contains("class"))
            {
                clauses.Add($" where {tp.Name} : class");
                continue;
            }

            // If type parameter is known to be a reference type (e.g., where T : Attribute)
            // and the return type uses T?, we need "where T : class" for explicit impl.
            // Interface-only constraints (e.g., where T : IDisposable) do NOT qualify.
            if (tp.IsKnownReferenceType
                && (returnType.Contains($"{tp.Name}?") || returnType.EndsWith($"{tp.Name}?")))
            {
                clauses.Add($" where {tp.Name} : class");
            }
        }
        return string.Join("", clauses);
    }

    private static string ExtractSimpleTypeName(string fullName)
    {
        var name = fullName;
        if (name.StartsWith("global::"))
            name = name.Substring(8);

        var lastDot = name.LastIndexOf('.');
        if (lastDot >= 0)
            return name.Substring(lastDot + 1);

        return name;
    }

    private static readonly HashSet<string> ObjectMemberNames = new(StringComparer.Ordinal)
    {
        "Equals", "GetHashCode", "ToString", "GetType"
    };

    private static bool NeedsNewKeyword(string interceptorName) =>
        ObjectMemberNames.Contains(interceptorName);

    private static string GetSetterNullabilityAttribute(InterfaceMemberInfo member)
    {
        if (member.SetterHasDisallowNull || member.SetterHasAllowNull)
            return "#pragma warning disable CS8769\n";
        return "";
    }

    private static string GetSetterNullabilityRestore(InterfaceMemberInfo member)
    {
        if (member.SetterHasDisallowNull || member.SetterHasAllowNull)
            return "\n#pragma warning restore CS8769";
        return "";
    }

    private static InterfaceMemberInfo? FindPropertyDelegationTarget(
        InterfaceMemberInfo member,
        InterfaceInfo iface)
    {
        if (!member.IsProperty)
            return null;

        var memberType = member.ReturnType.TrimEnd('?');
        if (memberType != "object" && memberType != "System.Object" &&
            memberType != "global::System.Object")
        {
            return null;
        }

        foreach (var candidate in iface.Members)
        {
            if (candidate.DeclaringInterfaceFullName == member.DeclaringInterfaceFullName &&
                candidate.Name == member.Name &&
                candidate.ReturnType == member.ReturnType)
                continue;

            if (!candidate.IsProperty || candidate.Name != member.Name)
                continue;

            if (candidate.ReturnType == member.ReturnType)
                continue;

            return candidate;
        }

        return null;
    }

    private static InterfaceMemberInfo? FindDelegationTarget(
        InterfaceMemberInfo member,
        InterfaceInfo iface)
    {
        if (member.DeclaringInterfaceFullName == iface.FullName)
            return null;

        foreach (var candidate in iface.Members)
        {
            if (candidate.DeclaringInterfaceFullName != iface.FullName)
                continue;

            if (candidate.Name != member.Name)
                continue;

            if (candidate.Parameters.Count != member.Parameters.Count)
                continue;

            var candidateParams = candidate.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
            var memberParams = member.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();

            bool sameParamNames = true;
            for (int i = 0; i < candidateParams.Length; i++)
            {
                if (candidateParams[i].Name != memberParams[i].Name)
                {
                    sameParamNames = false;
                    break;
                }
            }

            if (!sameParamNames)
                continue;

            bool signaturesMatch = member.ReturnType == candidate.ReturnType;
            if (signaturesMatch)
            {
                for (int i = 0; i < candidateParams.Length; i++)
                {
                    if (candidateParams[i].Type != memberParams[i].Type)
                    {
                        signaturesMatch = false;
                        break;
                    }
                }
            }

            if (signaturesMatch)
                continue;

            return candidate;
        }

        return null;
    }

    private static string GetDefaultForType(string typeName, DefaultValueStrategy strategy, string? concreteType)
    {
        if (typeName == "global::System.Threading.Tasks.ValueTask" || typeName == "ValueTask")
            return "default";

        if (typeName == "global::System.Threading.Tasks.Task" || typeName == "Task")
            return "global::System.Threading.Tasks.Task.CompletedTask";

        if (typeName.Contains("ValueTask<") || typeName.Contains("global::System.Threading.Tasks.ValueTask<"))
        {
            var innerType = ExtractGenericArg(typeName);
            if (!string.IsNullOrEmpty(innerType))
            {
                if (strategy == DefaultValueStrategy.NewInstance)
                {
                    var innerTypeToNew = concreteType ?? innerType;
                    return $"new global::System.Threading.Tasks.ValueTask<{innerType}>(new {innerTypeToNew}())";
                }
                return "default";
            }
            return "default";
        }

        if (typeName.Contains("Task<") || typeName.Contains("global::System.Threading.Tasks.Task<"))
        {
            var innerType = ExtractGenericArg(typeName);
            if (!string.IsNullOrEmpty(innerType))
            {
                var innerTypeToNew = concreteType ?? innerType;
                var innerValue = strategy switch
                {
                    DefaultValueStrategy.NewInstance => $"new {innerTypeToNew}()",
                    DefaultValueStrategy.Default => "default!",
                    _ => "default!"
                };
                return $"global::System.Threading.Tasks.Task.FromResult<{innerType}>({innerValue})";
            }
            return "global::System.Threading.Tasks.Task.CompletedTask";
        }

        var typeToNew = concreteType ?? typeName;
        return strategy switch
        {
            DefaultValueStrategy.NewInstance => $"new {typeToNew}()",
            DefaultValueStrategy.Default => "default!",
            _ => "default!"
        };
    }

    private static string ExtractGenericArg(string typeName)
    {
        var start = typeName.IndexOf('<');
        var end = typeName.LastIndexOf('>');
        if (start >= 0 && end > start)
        {
            return typeName.Substring(start + 1, end - start - 1);
        }
        return "";
    }

    #endregion

    #region Source Provider Building

    /// <summary>
    /// Builds SourceProviderInfo for inline interface stubs.
    /// For inline stubs, we build source providers for the primary interface and all its base interfaces.
    /// </summary>
    private static EquatableArray<SourceProviderInfo> BuildSourceProviders(
        InterfaceInfo iface,
        List<InlinePropertyModel> properties,
        List<InlineIndexerModel> indexers,
        List<UnifiedMethodInterceptorModel> methods,
        Dictionary<string, MethodGroupInfo> methodGroups,
        string stubClassName,
        string typeParamList)
    {
        // Helper to convert unbound generic form to bound form
        string ToBindGeneric(string interfaceName) =>
            typeParamList.Length > 0
                ? SymbolHelpers.ReplaceUnboundGeneric(interfaceName, typeParamList)
                : interfaceName;

        // Collect all unique interceptor names with their declaring interfaces
        var interceptorToDeclaringInterface = new Dictionary<string, string>();

        foreach (var prop in properties)
        {
            // Skip init-only properties - they can't be delegated to source
            if (prop.IsInitOnly)
                continue;
            var interceptorName = prop.PropertyName;
            // Find the declaring interface from the original member
            var member = iface.Members.FirstOrDefault(m => m.IsProperty && !m.IsIndexer && m.Name == prop.PropertyName);
            if (member != null && !interceptorToDeclaringInterface.ContainsKey(interceptorName))
            {
                interceptorToDeclaringInterface[interceptorName] = ToBindGeneric(member.DeclaringInterfaceFullName);
            }
        }

        // For indexers, use the indexer name directly (no container indirection)
        foreach (var indexer in indexers)
        {
            var member = iface.Members.FirstOrDefault(m => m.IsIndexer);
            if (member != null && !interceptorToDeclaringInterface.ContainsKey(indexer.IndexerName))
            {
                interceptorToDeclaringInterface[indexer.IndexerName] = ToBindGeneric(member.DeclaringInterfaceFullName);
            }
        }

        foreach (var method in methods)
        {
            var interceptorName = method.MethodName;
            // Find the declaring interface from method groups
            if (methodGroups.TryGetValue(interceptorName, out var group))
            {
                var firstOverload = group.Overloads.FirstOrDefault();
                if (firstOverload != null)
                {
                    // Get declaring interface from the first parameter in the group
                    var member = iface.Members.FirstOrDefault(m =>
                        !m.IsProperty && !m.IsIndexer && m.Name == interceptorName && !m.IsGenericMethod);
                    if (member != null && !interceptorToDeclaringInterface.ContainsKey(interceptorName))
                    {
                        interceptorToDeclaringInterface[interceptorName] = ToBindGeneric(member.DeclaringInterfaceFullName);
                    }
                }
            }
        }

        // Build the interface hierarchy - the primary interface plus all its base interfaces
        // Convert to bound generic form for proper matching
        var primaryInterface = ToBindGeneric(iface.FullName);
        var allInterfaces = new List<string> { primaryInterface };
        allInterfaces.AddRange(iface.BaseInterfaces.Select(bi => ToBindGeneric(bi)));

        // Build inheritance lookup from the interface hierarchy
        // This maps each interface to the set of interfaces it inherits from (including itself)
        var inheritanceLookup = BuildInheritanceLookup(iface.InterfaceHierarchy, typeParamList);

        // Build source providers for each interface in the hierarchy
        var sourceProviders = new List<SourceProviderInfo>();
        var allInterceptorNames = interceptorToDeclaringInterface.Keys.ToList();

        foreach (var sourceIfaceFullName in allInterfaces)
        {
            var memberMappings = new List<SourceMemberMapping>();

            // Get the set of interfaces that this source interface inherits from (including itself)
            var inheritedInterfaces = inheritanceLookup.TryGetValue(sourceIfaceFullName, out var inherited)
                ? inherited
                : new HashSet<string> { sourceIfaceFullName };

            foreach (var interceptorName in allInterceptorNames)
            {
                var declaringInterface = interceptorToDeclaringInterface[interceptorName];

                // Set source if the declaring interface is inherited by the source interface
                // For Source(IList<T>): includes IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
                // For Source(IEnumerable<T>): includes only IEnumerable<T> and IEnumerable
                var setSource = inheritedInterfaces.Contains(declaringInterface);

                // For properties/indexers: use the member declaration on the SOURCE interface itself
                // (not the unioned/shared declaration). When `new`-shadowing narrows or widens accessors,
                // the fallback must only bind to accessors declared on the source face.
                bool? sourceHasGetter = null;
                bool? sourceHasSetter = null;
                if (setSource)
                {
                    // Resolve the source interface to its unbound form to match hierarchy entries.
                    var sourceMember = iface.Members.FirstOrDefault(m =>
                        (m.IsProperty || m.IsIndexer)
                        && ((m.IsIndexer && interceptorName == "Indexer") || (!m.IsIndexer && m.Name == interceptorName))
                        && ToBindGeneric(m.DeclaringInterfaceFullName) == sourceIfaceFullName);
                    if (sourceMember != null)
                    {
                        sourceHasGetter = sourceMember.HasGetter;
                        sourceHasSetter = sourceMember.HasSetter;
                    }
                }

                memberMappings.Add(new SourceMemberMapping(
                    InterceptorName: interceptorName,
                    SourceInterfaceType: setSource ? declaringInterface : "",
                    SetSource: setSource,
                    SourceHasGetter: sourceHasGetter,
                    SourceHasSetter: sourceHasSetter));
            }

            sourceProviders.Add(new SourceProviderInfo(
                InterfaceType: sourceIfaceFullName,
                MethodName: "Source",
                MemberMappings: memberMappings.ToEquatableArray()));
        }

        return sourceProviders.ToEquatableArray();
    }

    /// <summary>
    /// Builds a lookup table from interface to the set of interfaces it inherits from (including itself).
    /// </summary>
    private static Dictionary<string, HashSet<string>> BuildInheritanceLookup(
        EquatableArray<InterfaceHierarchyEntry> hierarchy,
        string typeParamList)
    {
        var lookup = new Dictionary<string, HashSet<string>>();

        // Helper to convert unbound generic form to bound form
        string ToBindGeneric(string interfaceName) =>
            typeParamList.Length > 0
                ? SymbolHelpers.ReplaceUnboundGeneric(interfaceName, typeParamList)
                : interfaceName;

        foreach (var entry in hierarchy)
        {
            var ifaceName = ToBindGeneric(entry.InterfaceName);
            var inheritedSet = new HashSet<string> { ifaceName };

            foreach (var baseIface in entry.BaseInterfaces)
            {
                inheritedSet.Add(ToBindGeneric(baseIface));
            }

            lookup[ifaceName] = inheritedSet;
        }

        return lookup;
    }

    #endregion
}
