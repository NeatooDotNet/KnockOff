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
            classStubs.Add(BuildClassStub(cls));
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

        // Count indexers for naming strategy
        var indexerCount = SymbolHelpers.CountIndexers(iface.Members);

        // Deduplicate property/indexer members by name for interceptor class generation
        var processedPropertyNames = new HashSet<string>();
        var deduplicatedPropertyMembers = new List<InterfaceMemberInfo>();
        foreach (var member in iface.Members)
        {
            if (member.IsProperty || member.IsIndexer)
            {
                var memberName = member.IsIndexer
                    ? SymbolHelpers.GetIndexerName(indexerCount, member.IndexerTypeSuffix)
                    : member.Name;
                if (processedPropertyNames.Add(memberName))
                {
                    deduplicatedPropertyMembers.Add(member);
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
                indexers.Add(BuildIndexerModel(member, stubClassName, indexerCount, typeParamList, constraintClause));
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
                    methods.Add(BuildMethodModel(nonGenericGroup, stubClassName, typeParamList, constraintClause));
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
                methods.Add(BuildMethodModel(group, stubClassName, typeParamList, constraintClause));
            }
        }

        // Build event models
        foreach (var evt in deduplicatedEvents)
        {
            events.Add(BuildEventModel(evt, stubClassName, typeParamList, constraintClause));
        }

        // Build interceptor properties
        var interceptorProperties = BuildInterceptorProperties(
            deduplicatedPropertyMembers, methodGroups, deduplicatedEvents,
            stubClassName, indexerCount, typeParamList);

        // Build implementations
        var implementations = BuildImplementations(iface, methodGroups, memberKeyToGroupName, indexerCount, baseType, typeParamList);

        var hasGenericMethods = genericHandlers.Count > 0 || iface.Members.Any(m => m.IsGenericMethod);

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
            Implementations: implementations);
    }

    private static InlinePropertyModel BuildPropertyModel(
        InterfaceMemberInfo member,
        string stubClassName,
        string typeParamList,
        string constraintClause)
    {
        var interceptClassName = $"{stubClassName}_{member.Name}Interceptor";

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
            ConstraintClauses: constraintClause);
    }

    private static InlineIndexerModel BuildIndexerModel(
        InterfaceMemberInfo member,
        string stubClassName,
        int indexerCount,
        string typeParamList,
        string constraintClause)
    {
        var indexerName = SymbolHelpers.GetIndexerName(indexerCount, member.IndexerTypeSuffix);
        var interceptClassName = $"{stubClassName}_{indexerName}Interceptor";

        var keyType = member.IndexerParameters.Count == 1
            ? member.IndexerParameters.GetArray()![0].Type
            : $"({string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"))})";

        var singleKeyType = member.IndexerParameters.Count == 1
            ? member.IndexerParameters.GetArray()![0].Type
            : keyType;

        var paramSig = string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"));
        var paramTypes = string.Join(", ", member.IndexerParameters.Select(p => p.Type));
        var keyExpr = member.IndexerParameters.Count == 1
            ? member.IndexerParameters.GetArray()![0].Name
            : $"({string.Join(", ", member.IndexerParameters.Select(p => p.Name))})";

        return new InlineIndexerModel(
            InterceptorClassName: interceptClassName,
            IndexerName: indexerName,
            ReturnType: member.ReturnType,
            KeyType: keyType,
            SingleKeyType: singleKeyType,
            NullableKeyType: MakeNullable(keyType),
            HasGetter: member.HasGetter,
            HasSetter: member.HasSetter,
            ParameterSignature: paramSig,
            ParameterTypes: paramTypes,
            KeyExpression: keyExpr,
            StubClassName: $"Stubs.{stubClassName}{typeParamList}",
            TypeParameterList: typeParamList,
            ConstraintClauses: constraintClause);
    }

    private static UnifiedMethodInterceptorModel BuildMethodModel(
        MethodGroupInfo group,
        string stubClassName,
        string typeParamList,
        string constraintClause)
    {
        var interceptClassName = $"{stubClassName}_{group.Name}Interceptor";
        var stubClassRef = $"Stubs.{stubClassName}{typeParamList}";

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
                    RefPrefix: GetRefKindPrefix(p.RefKind)))
                .ToEquatableArray();

            var trackableParams = UnifiedInterceptorBuilder.GetTrackableParameters(parameters);
            var hasRefOrOut = parameters.Any(p => p.RefKind == RefKind.Ref || p.RefKind == RefKind.Out);

            // Determine default expression
            var defaultExpr = group.IsVoid ? "" : GetDefaultExpressionForReturn(group.ReturnType, group.IsNullable);
            var throwsOnDefault = !group.IsVoid && !group.IsNullable && IsUninstantiableType(group.ReturnType);

            signatures.Add(new MethodSignatureInfo(
                Parameters: parameters,
                TrackableParameters: trackableParams,
                ParameterDeclarations: UnifiedInterceptorBuilder.BuildParameterDeclarations(parameters),
                ReturnType: group.ReturnType,
                IsVoid: group.IsVoid,
                HasRefOrOutParams: hasRefOrOut,
                DefaultExpression: defaultExpr,
                ThrowsOnDefault: throwsOnDefault));
        }

        // If no non-generic overloads, create empty model
        if (signatures.Count == 0)
        {
            return UnifiedInterceptorBuilder.BuildMethodInterceptor(
                interceptorClassName: interceptClassName,
                methodName: group.Name,
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
            ownerClassName: stubClassRef,
            ownerTypeParameters: "",
            overloads: signatures);
    }

    private static string GetDefaultExpressionForReturn(string returnType, bool isNullable)
    {
        if (isNullable)
            return "default!";

        // Task types return completed task
        if (returnType == "global::System.Threading.Tasks.Task")
            return "global::System.Threading.Tasks.Task.CompletedTask";
        if (returnType == "global::System.Threading.Tasks.ValueTask")
            return "default";

        return "default!";
    }

    private static bool IsUninstantiableType(string returnType)
    {
        // Interface or abstract types that can't have a default instance
        // For now, be conservative and return false - we'll use default!
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

        // Get the first generic overload's type parameters
        var genericOverload = group.Overloads.First(o => o.IsGenericMethod);
        var typeParams = genericOverload.TypeParameters.GetArray()!;
        var typeParamNames = string.Join(", ", typeParams.Select(tp => tp.Name));
        var typeParamCount = typeParams.Length;

        // Build constraint clauses for method type parameters
        var methodConstraintClauses = GetConstraintClauses(typeParams);

        // Get non-generic parameters
        var typeParamSet = new HashSet<string>(typeParams.Select(tp => tp.Name));
        var nonGenericParams = GetInputCombinedParameters(group.CombinedParameters)
            .Where(p => !IsGenericParameterType(p.Type, typeParamSet))
            .ToArray();

        // Build the dictionary key type
        var keyType = typeParamCount == 1
            ? "global::System.Type"
            : $"({string.Join(", ", typeParams.Select(_ => "global::System.Type"))})";

        var keyConstruction = typeParamCount == 1
            ? $"typeof({typeParams[0].Name})"
            : $"({string.Join(", ", typeParams.Select(tp => $"typeof({tp.Name})"))})";

        // Build delegate signature
        var delegateReturnType = group.IsVoid ? "void" : group.ReturnType;
        var allParams = genericOverload.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
        var delegateParams = new List<string> { $"{stubClassRef} ko" };
        foreach (var p in allParams)
        {
            delegateParams.Add($"{p.Type} {p.Name}");
        }
        var delegateParamList = string.Join(", ", delegateParams);
        var delegateSignature = group.IsVoid
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
            NullableType: p.NullableType,
            RefKind: p.RefKind,
            RefPrefix: GetRefKindPrefix(p.RefKind))).ToEquatableArray();

        return new InlineGenericMethodHandlerModel(
            InterceptorClassName: interceptClassName,
            MethodName: group.Name,
            ReturnType: group.ReturnType,
            IsVoid: group.IsVoid,
            TypeParameterNames: typeParamNames,
            KeyType: keyType,
            KeyConstruction: keyConstruction,
            MethodConstraintClauses: methodConstraintClauses,
            TypedHandlerClassName: $"{group.Name}TypedHandler",
            DelegateSignature: delegateSignature,
            NonGenericParameters: nonGenericParamModels,
            LastCallArgType: lastCallArgType,
            LastCallArgsType: lastCallArgsType,
            StubClassName: stubClassRef,
            InterfaceTypeParameterList: ifaceTypeParamList,
            InterfaceConstraintClauses: ifaceConstraintClause);
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

        return new InlineEventModel(
            InterceptorClassName: interceptClassName,
            EventName: evt.Name,
            DelegateType: delegateType,
            TypeParameterList: typeParamList,
            ConstraintClauses: constraintClause);
    }

    private static EquatableArray<InlineInterceptorPropertyModel> BuildInterceptorProperties(
        List<InterfaceMemberInfo> propertyMembers,
        Dictionary<string, MethodGroupInfo> methodGroups,
        List<EventMemberInfo> events,
        string stubClassName,
        int indexerCount,
        string typeParamList)
    {
        var properties = new List<InlineInterceptorPropertyModel>();

        // Property/indexer interceptors
        foreach (var member in propertyMembers)
        {
            var memberName = member.IsIndexer
                ? SymbolHelpers.GetIndexerName(indexerCount, member.IndexerTypeSuffix)
                : member.Name;
            var interceptorType = $"{stubClassName}_{memberName}Interceptor{typeParamList}";

            properties.Add(new InlineInterceptorPropertyModel(
                PropertyName: memberName,
                InterceptorTypeName: interceptorType,
                NeedsNewKeyword: NeedsNewKeyword(memberName),
                Description: $"Interceptor for {memberName}."));
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

        // Event interceptors (with Interceptor suffix)
        foreach (var evt in events)
        {
            var interceptorType = $"{stubClassName}_{evt.Name}Interceptor{typeParamList}";
            properties.Add(new InlineInterceptorPropertyModel(
                PropertyName: $"{evt.Name}Interceptor",
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
        int indexerCount,
        string baseType,
        string typeParamList)
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
                implementations.Add(BuildIndexerImplementation(member, memberIfaceName, simpleIfaceName, indexerCount, iface));
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
            OnCallArgs: "this",
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
            OutParameterInitializations: EquatableArray<string>.Empty);
    }

    private static InlineInterfaceImplementation BuildIndexerImplementation(
        InterfaceMemberInfo member,
        string interfaceFullName,
        string simpleIfaceName,
        int indexerCount,
        InterfaceInfo iface)
    {
        var indexerName = SymbolHelpers.GetIndexerName(indexerCount, member.IndexerTypeSuffix);
        var paramList = string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"));
        var argList = string.Join(", ", member.IndexerParameters.Select(p => p.Name));
        var keyArg = member.IndexerParameters.Count == 1
            ? member.IndexerParameters.GetArray()![0].Name
            : $"({argList})";

        var defaultExpr = member.IsNullable
            ? "default"
            : GetDefaultForType(member.ReturnType, member.DefaultStrategy, member.ConcreteTypeForNew);

        return new InlineInterfaceImplementation(
            Kind: InlineMemberKind.Indexer,
            InterfaceFullName: interfaceFullName,
            SimpleInterfaceName: simpleIfaceName,
            MemberName: "this",
            ReturnType: member.ReturnType,
            IsVoid: false,
            IsInitOnly: false,
            HasGetter: member.HasGetter,
            HasSetter: member.HasSetter,
            InterceptorName: indexerName,
            ParameterDeclarations: paramList,
            ArgumentList: argList,
            InvokeSuffix: "",  // Indexers don't use invoke suffix
            RecordCallArgs: argList,
            OnCallArgs: $"this, {argList}",
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
            OutParameterInitializations: EquatableArray<string>.Empty);
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
        var isMultiOverload = GetUniqueSignatureCount(nonGenericOverloads, group.ReturnType) > 1;

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
                    RefPrefix: GetRefKindPrefix(p.RefKind)))
                .ToEquatableArray();
            invokeSuffix = "_" + UnifiedInterceptorBuilder.GetSignatureSuffix(paramModels, group.ReturnType);
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
            OnCallArgs: "",      // No longer used with new Invoke pattern
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
            OutParameterInitializations: outParamInits);
    }

    private static int GetUniqueSignatureCount(MethodOverloadInfo[] overloads, string returnType)
    {
        var seen = new HashSet<string>();
        foreach (var overload in overloads)
        {
            var paramModels = (overload.Parameters.GetArray() ?? Array.Empty<ParameterInfo>())
                .Select(p => new ParameterModel(
                    Name: p.Name,
                    EscapedName: EscapeIdentifier(p.Name),
                    Type: p.Type,
                    NullableType: MakeNullable(p.Type),
                    RefKind: p.RefKind,
                    RefPrefix: GetRefKindPrefix(p.RefKind)))
                .ToEquatableArray();
            var suffix = UnifiedInterceptorBuilder.GetSignatureSuffix(paramModels, returnType);
            seen.Add(suffix);
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

        var paramList = string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
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
            ReturnType: member.ReturnType,
            IsVoid: member.ReturnType == "void",
            IsInitOnly: false,
            HasGetter: false,
            HasSetter: false,
            InterceptorName: group.Name,
            ParameterDeclarations: paramList,
            ArgumentList: argList,
            InvokeSuffix: "",  // Generic methods use Of<T>() pattern, not invoke suffix
            RecordCallArgs: nonGenericArgList,
            OnCallArgs: member.Parameters.Count > 0 ? $"this, {argList}" : "this",
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
            OutParameterInitializations: outParamInits);
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
            OnCallArgs: "",
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
            OutParameterInitializations: EquatableArray<string>.Empty);
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
            InterceptorName: $"{evt.Name}Interceptor",
            ParameterDeclarations: "",
            ArgumentList: "",
            InvokeSuffix: "",  // Events don't use invoke suffix
            RecordCallArgs: "",
            OnCallArgs: "",
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

        // OnCall type - include type params for open generics so T is in scope
        var stubClassRef = del.IsOpenGeneric && typeParamList.Length > 0
            ? $"Stubs.{stubClassName}{typeParamList}"
            : $"Stubs.{stubClassName}";
        string onCallType;
        if (del.IsVoid)
        {
            onCallType = del.Parameters.Count == 0
                ? $"global::System.Action<{stubClassRef}>"
                : $"global::System.Action<{stubClassRef}, {string.Join(", ", del.Parameters.Select(p => p.Type))}>";
        }
        else
        {
            onCallType = del.Parameters.Count == 0
                ? $"global::System.Func<{stubClassRef}, {del.ReturnType}>"
                : $"global::System.Func<{stubClassRef}, {string.Join(", ", del.Parameters.Select(p => p.Type))}, {del.ReturnType}>";
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
            RefPrefix: GetRefKindPrefix(p.RefKind))).ToEquatableArray();

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
            OnCallType: onCallType,
            LastCallArgType: lastCallArgType,
            LastCallArgsType: lastCallArgsType,
            DefaultExpression: defaultExpr);
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
    /// Determines if two methods with the same name can share an interceptor.
    /// They cannot if:
    /// 1. Different return types AND different parameters (both need separate interceptors)
    /// 2. Same parameter name has different types across overloads (type mismatch in combined params)
    /// </summary>
    private static bool AreMethodsCompatibleForSharedInterceptor(InterfaceMemberInfo m1, InterfaceMemberInfo m2)
    {
        // Check if any shared parameter names have different types - this is always incompatible
        var m1Params = m1.Parameters.ToDictionary(p => p.Name, p => p.Type);
        foreach (var p2 in m2.Parameters)
        {
            if (m1Params.TryGetValue(p2.Name, out var m1Type) && m1Type != p2.Type)
                return false;
        }

        // If parameter names/types are compatible, check return types
        // Methods with same parameters but different return types (like BCL IEnumerable)
        // typically use delegation, so they're compatible for sharing an interceptor
        // Only split when return types differ AND there are different parameters that
        // would cause confusion about which callback to use
        if (m1.ReturnType != m2.ReturnType)
        {
            // Different return types with different parameter sets need separate interceptors
            var m1ParamNames = new HashSet<string>(m1.Parameters.Select(p => p.Name));
            var m2ParamNames = new HashSet<string>(m2.Parameters.Select(p => p.Name));

            // If parameters are completely different or partially different,
            // we need separate interceptors because users would want different callbacks
            if (!m1ParamNames.SetEquals(m2ParamNames))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Groups methods by name, splitting into separate numbered groups when overloads are incompatible.
    /// Returns both the groups dictionary (keyed by group name) and a member-to-group-name mapping.
    /// </summary>
    private static (Dictionary<string, MethodGroupInfo> Groups, Dictionary<string, string> MemberKeyToGroupName) GroupMethodsByName(IEnumerable<InterfaceMemberInfo> methods)
    {
        // First, group all methods by name
        var methodsByName = new Dictionary<string, List<InterfaceMemberInfo>>();

        foreach (var method in methods.Where(m => !m.IsProperty))
        {
            if (!methodsByName.TryGetValue(method.Name, out var list))
            {
                list = new List<InterfaceMemberInfo>();
                methodsByName[method.Name] = list;
            }
            list.Add(method);
        }

        var groups = new Dictionary<string, MethodGroupInfo>();
        var memberKeyToGroupName = new Dictionary<string, string>();

        foreach (var kvp in methodsByName)
        {
            var methodName = kvp.Key;
            var overloads = kvp.Value;

            // Check if all overloads are compatible (can share one interceptor)
            if (overloads.Count == 1 || AreAllOverloadsCompatible(overloads))
            {
                // All compatible - create single group with combined parameters
                var group = BuildMethodGroup(methodName, overloads);
                groups[methodName] = group;

                // Map all members to this group
                foreach (var overload in overloads)
                {
                    memberKeyToGroupName[GetMemberKey(overload)] = methodName;
                }
            }
            else
            {
                // Incompatible overloads - create separate numbered groups
                for (int i = 0; i < overloads.Count; i++)
                {
                    var overload = overloads[i];
                    var numberedName = $"{methodName}{i + 1}";
                    var group = BuildMethodGroup(numberedName, new List<InterfaceMemberInfo> { overload });
                    groups[numberedName] = group;
                    memberKeyToGroupName[GetMemberKey(overload)] = numberedName;
                }
            }
        }

        return (groups, memberKeyToGroupName);
    }

    /// <summary>
    /// Checks if all overloads in a list are compatible with each other for sharing an interceptor.
    /// </summary>
    private static bool AreAllOverloadsCompatible(List<InterfaceMemberInfo> overloads)
    {
        for (int i = 0; i < overloads.Count; i++)
        {
            for (int j = i + 1; j < overloads.Count; j++)
            {
                if (!AreMethodsCompatibleForSharedInterceptor(overloads[i], overloads[j]))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Builds a MethodGroupInfo from a list of compatible overloads.
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
            .Select(o => new MethodOverloadInfo(o.Parameters, o.IsGenericMethod, o.TypeParameters))
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
        $"{GetRefKindPrefix(p.RefKind)}{p.Type} {p.Name}";

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

            if (returnType.Contains($"{tp.Name}?") || returnType.EndsWith($"{tp.Name}?"))
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
}
