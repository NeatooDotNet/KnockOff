// src/Generator/Builder/StandaloneClassModelBuilder.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using KnockOff.Model.Inline;
using KnockOff.Model.Shared;
using KnockOff.Model.StandaloneClass;
using Microsoft.CodeAnalysis;

namespace KnockOff.Builder;

/// <summary>
/// Transforms StandaloneClassStubInfo into StandaloneClassGenerationUnit.
/// Reuses model types and patterns from ClassModelBuilder for inline class stubs.
/// </summary>
internal static class StandaloneClassModelBuilder
{
    /// <summary>
    /// Suffix added to generic interceptor property names when a method group has both generic and non-generic overloads.
    /// </summary>
    private const string GenericSuffix = "Generic";

    public static StandaloneClassGenerationUnit Build(StandaloneClassStubInfo info)
    {
        var cls = info.TargetClassInfo!;
        var stubClassName = info.ClassName;

        // Type parameters from user's stub class
        var typeParamList = SymbolHelpers.FormatTypeParameterList(info.TypeParameters);
        var constraints = SymbolHelpers.FormatTypeConstraints(info.TypeParameters);
        var constraintClause = string.IsNullOrEmpty(constraints) ? "" : $" {constraints}";

        // For open generic, replace the empty <> in FullName with actual type params
        var baseType = cls.IsOpenGeneric && typeParamList.Length > 0
            ? SymbolHelpers.ReplaceUnboundGeneric(cls.FullName, typeParamList)
            : cls.FullName;

        // Split methods into non-generic and generic groups
        var allMethods = cls.Members.Where(m => !m.IsProperty && !m.IsIndexer).ToList();
        var methodsByName = allMethods.GroupBy(m => m.Name).ToList();

        // Count indexers to determine naming strategy
        var indexerCount = SymbolHelpers.CountClassIndexers(cls.Members);

        // Check for required members
        var requiredMembers = cls.Members.Where(m => m.IsProperty && m.IsRequired).ToList();
        var hasRequiredMembers = requiredMembers.Count > 0;
        var requiredMemberNames = requiredMembers.Select(m => m.Name).ToEquatableArray();

        // Build type parameters model
        var typeParameters = info.TypeParameters.Select(tp => new TypeParameterModel(
            Name: tp.Name,
            Constraints: string.Join(", ", tp.Constraints))).ToEquatableArray();

        // Build user override lookups for base class pattern
        var userOverrideProperties = new HashSet<string>(info.UserOverrideProperties.GetArray() ?? Array.Empty<string>());
        var userOverrideMethods = new HashSet<string>(info.UserOverrideMethods.GetArray() ?? Array.Empty<string>());

        // Build models for interceptor classes
        var properties = new List<InlineClassPropertyModel>();
        var indexers = new List<InlineClassIndexerModel>();
        var methods = new List<UnifiedMethodInterceptorModel>();
        var genericHandlers = new List<InlineGenericMethodHandlerModel>();
        var events = new List<InlineClassEventModel>();
        var interceptorProperties = new List<InlineInterceptorPropertyModel>();
        var resetStatements = new List<string>();

        // Build property and indexer interceptors
        foreach (var member in cls.Members)
        {
            if (member.IsProperty && !member.IsIndexer)
            {
                var propModel = BuildPropertyModel(member, stubClassName, typeParamList, constraintClause);
                properties.Add(propModel);
                interceptorProperties.Add(new InlineInterceptorPropertyModel(
                    PropertyName: member.Name,
                    InterceptorTypeName: $"{propModel.InterceptorClassName}{typeParamList}",
                    NeedsNewKeyword: NeedsNewKeyword(member.Name),
                    Description: $"Interceptor for {member.Name}."));
                resetStatements.Add($"{member.Name}.Reset();");
            }
            else if (member.IsIndexer)
            {
                var indexerName = SymbolHelpers.GetIndexerName(indexerCount, member.IndexerTypeSuffix);
                var indexerModel = BuildIndexerModel(member, stubClassName, indexerCount, typeParamList, constraintClause);
                indexers.Add(indexerModel);
                interceptorProperties.Add(new InlineInterceptorPropertyModel(
                    PropertyName: indexerName,
                    InterceptorTypeName: $"{indexerModel.InterceptorClassName}{typeParamList}",
                    NeedsNewKeyword: NeedsNewKeyword(indexerName),
                    Description: $"Interceptor for {indexerName}."));
                resetStatements.Add($"{indexerName}.Reset();");
            }
        }

        // Build method interceptors: handle mixed groups (generic + non-generic with same name)
        var nonGenericMethodGroups = new List<MethodGroup>();

        foreach (var nameGroup in methodsByName)
        {
            var name = nameGroup.Key;
            var members = nameGroup.ToList();
            var hasGeneric = members.Any(m => m.IsGenericMethod);
            var hasNonGeneric = members.Any(m => !m.IsGenericMethod);

            if (hasGeneric && hasNonGeneric)
            {
                // Mixed group: split into separate interceptors
                var nonGenericMembers = members.Where(m => !m.IsGenericMethod).ToList();
                var genericMembers = members.Where(m => m.IsGenericMethod).ToList();

                // Non-generic overloads get a UnifiedMethodInterceptorModel
                nonGenericMethodGroups.Add(new MethodGroup(name, name, nonGenericMembers));

                // Generic overloads get an InlineGenericMethodHandlerModel with "Generic" suffix
                var genericName = name + GenericSuffix;
                var handler = BuildGenericMethodHandlerModel(genericMembers, genericName, name, stubClassName, typeParamList, constraintClause, info.TypeParameters);
                genericHandlers.Add(handler);
                interceptorProperties.Add(new InlineInterceptorPropertyModel(
                    PropertyName: genericName,
                    InterceptorTypeName: $"{handler.InterceptorClassName}{typeParamList}",
                    NeedsNewKeyword: NeedsNewKeyword(genericName),
                    Description: $"Interceptor for {name} (generic overloads, use .Of<T>())."));
                resetStatements.Add($"{genericName}.Reset();");
            }
            else if (hasGeneric)
            {
                // Pure generic group
                var handler = BuildGenericMethodHandlerModel(members, name, name, stubClassName, typeParamList, constraintClause, info.TypeParameters);
                genericHandlers.Add(handler);
                interceptorProperties.Add(new InlineInterceptorPropertyModel(
                    PropertyName: name,
                    InterceptorTypeName: $"{handler.InterceptorClassName}{typeParamList}",
                    NeedsNewKeyword: NeedsNewKeyword(name),
                    Description: $"Interceptor for {name} (generic)."));
                resetStatements.Add($"{name}.Reset();");
            }
            else
            {
                // Pure non-generic group
                nonGenericMethodGroups.Add(new MethodGroup(name, name, members));
            }
        }

        // Build non-generic method interceptors using UnifiedInterceptorBuilder
        foreach (var group in nonGenericMethodGroups)
        {
            var interceptorClassName = $"{stubClassName}_{group.GroupName}Interceptor";
            var ownerClassName = $"{stubClassName}{typeParamList}";

            // Convert ClassMemberInfo to MethodSignatureInfo for UnifiedInterceptorBuilder
            // Set per-signature UserMethodName for overloads with user overrides
            var signatures = group.Members
                .Select(m =>
                {
                    var sig = ToMethodSignatureInfo(m);
                    var hasUserOverride = userOverrideMethods.Contains(
                        SymbolHelpers.BuildOverrideSignatureKey(m.Name, m.Parameters));
                    return hasUserOverride ? sig with { UserMethodName = $"__UserMethod_{m.Name}" } : sig;
                })
                .ToList();

            // Pass userMethodName to the interceptor builder if ANY overload has a user override
            var anyHasUserOverride = signatures.Any(s => s.UserMethodName != null);
            var userMethodName = anyHasUserOverride ? $"__UserMethod_{group.MethodName}" : null;

            var methodModel = UnifiedInterceptorBuilder.BuildMethodInterceptor(
                interceptorClassName: interceptorClassName,
                methodName: group.MethodName,
                declaringInterface: "",  // Class stubs don't have a declaring interface
                ownerClassName: ownerClassName,
                ownerTypeParameters: "",
                overloads: signatures,
                userMethodName: userMethodName);

            methods.Add(methodModel);

            // One interceptor property per group
            interceptorProperties.Add(new InlineInterceptorPropertyModel(
                PropertyName: group.GroupName,
                InterceptorTypeName: $"{interceptorClassName}{typeParamList}",
                NeedsNewKeyword: NeedsNewKeyword(group.GroupName),
                Description: $"Interceptor for {group.MethodName}."));
            resetStatements.Add($"{group.GroupName}.Reset();");
        }

        // Build event interceptors
        foreach (var evt in cls.Events)
        {
            var eventModel = BuildEventModel(evt, stubClassName, typeParamList, constraintClause);
            events.Add(eventModel);
            interceptorProperties.Add(new InlineInterceptorPropertyModel(
                PropertyName: evt.Name,
                InterceptorTypeName: $"{eventModel.InterceptorClassName}{typeParamList}",
                NeedsNewKeyword: NeedsNewKeyword(evt.Name),
                Description: $"Interceptor for {evt.Name}."));
            resetStatements.Add($"{evt.Name}.Reset();");
        }

        // Build constructor models
        var constructors = cls.Constructors.Select(ctor => BuildConstructorModel(ctor, typeParamList)).ToEquatableArray();

        // Build Impl class member models
        var implProperties = new List<InlineClassImplPropertyModel>();
        var implIndexers = new List<InlineClassImplIndexerModel>();
        var implMethods = new List<InlineClassImplMethodModel>();
        var implEvents = new List<InlineClassImplEventModel>();

        // Impl properties
        foreach (var member in cls.Members)
        {
            if (member.IsProperty && !member.IsIndexer)
            {
                // Check if this property has a user-defined override (PropertyName_ suffix)
                var hasUserOverride = userOverrideProperties.Contains(member.Name + "_");
                implProperties.Add(BuildImplPropertyModel(member, hasUserOverride));
            }
            else if (member.IsIndexer)
            {
                implIndexers.Add(BuildImplIndexerModel(member, indexerCount));
            }
        }

        // Impl methods: non-generic methods use existing Invoke pattern
        foreach (var group in nonGenericMethodGroups)
        {
            // Find the corresponding method model to check if it has overloads
            var methodModel = methods.First(m => m.MethodName == group.MethodName &&
                m.InterceptorClassName == $"{stubClassName}_{group.GroupName}Interceptor");
            var hasOverloads = methodModel.Overloads.Count > 0;

            foreach (var member in group.Members)
            {
                // For multi-overload interceptors, compute the invoke suffix
                var invokeSuffix = "";
                if (hasOverloads)
                {
                    var sig = ToMethodSignatureInfo(member);
                    invokeSuffix = "_" + UnifiedInterceptorBuilder.GetSignatureSuffix(sig.Parameters, sig.ReturnType);
                }

                // Check if this specific method overload has a user override
                var hasUserOverride = userOverrideMethods.Contains(
                    SymbolHelpers.BuildOverrideSignatureKey(member.Name, member.Parameters));

                implMethods.Add(BuildImplMethodModel(member, group.GroupName, invokeSuffix, hasOverloads, hasUserOverride));
            }
        }

        // Impl methods: generic methods use Of<T>() pattern
        foreach (var nameGroup in methodsByName)
        {
            var name = nameGroup.Key;
            var members = nameGroup.ToList();
            var genericMembers = members.Where(m => m.IsGenericMethod).ToList();
            if (genericMembers.Count == 0) continue;

            var hasNonGeneric = members.Any(m => !m.IsGenericMethod);
            var handlerName = hasNonGeneric ? name + GenericSuffix : name;

            foreach (var member in genericMembers)
            {
                implMethods.Add(BuildImplGenericMethodModel(member, handlerName));
            }
        }

        // Impl events
        foreach (var evt in cls.Events)
        {
            implEvents.Add(new InlineClassImplEventModel(
                EventName: evt.Name,
                DelegateType: evt.FullDelegateTypeName.TrimEnd('?'),
                AccessModifier: evt.AccessModifier));
        }

        // Build containing types models
        var containingTypes = info.ContainingTypes.Select(ct => new ContainingTypeModel(
            Keyword: ct.Keyword,
            Name: ct.Name,
            AccessModifier: ct.AccessibilityModifier)).ToEquatableArray();

        // Build base class properties (virtual protected properties for user override pattern)
        var baseClassProperties = cls.Members
            .Where(m => m.IsProperty && !m.IsIndexer)
            .Select(m => new BaseClassPropertyModel(
                PropertyName: m.Name,
                ReturnType: m.ReturnType,
                HasGetter: m.HasGetter,
                HasSetter: m.HasSetter,
                TargetMemberDescription: $"{cls.FullName}.{m.Name}"))
            .ToEquatableArray();

        // Build base class methods (virtual protected methods for user override pattern)
        // Skip generic methods -- user method overrides for generic methods are not supported
        var baseClassMethods = cls.Members
            .Where(m => !m.IsProperty && !m.IsIndexer && !m.IsGenericMethod)
            .Select(m => new BaseClassMethodModel(
                MethodName: m.Name,
                ReturnType: m.ReturnType,
                ParameterDeclarations: string.Join(", ", m.Parameters.Select(p => FormatParameter(p))),
                IsVoid: m.ReturnType == "void",
                IsAbstract: m.IsAbstract,
                TargetMemberDescription: $"{cls.FullName}.{m.Name}"))
            .ToEquatableArray();

        return new StandaloneClassGenerationUnit(
            ClassName: stubClassName,
            TargetClassName: cls.FullName,
            TargetClassForInheritance: baseType,
            Namespace: info.Namespace,
            TypeParameters: typeParameters,
            ContainingTypes: containingTypes,
            Properties: properties.ToEquatableArray(),
            Indexers: indexers.ToEquatableArray(),
            Methods: methods.ToEquatableArray(),
            Events: events.ToEquatableArray(),
            GenericMethodHandlers: genericHandlers.ToEquatableArray(),
            InterceptorProperties: interceptorProperties.ToEquatableArray(),
            ResetStatements: resetStatements.ToEquatableArray(),
            Constructors: constructors,
            ImplProperties: implProperties.ToEquatableArray(),
            ImplIndexers: implIndexers.ToEquatableArray(),
            ImplMethods: implMethods.ToEquatableArray(),
            ImplEvents: implEvents.ToEquatableArray(),
            HasRequiredMembers: hasRequiredMembers,
            RequiredMemberNames: requiredMemberNames,
            Strict: info.Strict,
            BaseClassProperties: baseClassProperties,
            BaseClassMethods: baseClassMethods);
    }

    #region Method Grouping

    /// <summary>
    /// Represents a group of method overloads that share a single interceptor.
    /// </summary>
    private sealed class MethodGroup
    {
        public string MethodName { get; }
        public string GroupName { get; }
        public List<ClassMemberInfo> Members { get; }

        public MethodGroup(string methodName, string groupName, List<ClassMemberInfo> members)
        {
            MethodName = methodName;
            GroupName = groupName;
            Members = members;
        }
    }

    /// <summary>
    /// Groups methods by name. All overloads share a single interceptor with multiple Call signatures.
    /// </summary>
    private static List<MethodGroup> GroupMethodsByName(IEnumerable<ClassMemberInfo> methods)
    {
        return methods
            .GroupBy(m => m.Name)
            .Select(g => new MethodGroup(g.Key, g.Key, g.ToList()))
            .ToList();
    }

    #endregion

    #region MethodSignatureInfo Conversion

    /// <summary>
    /// Converts a ClassMemberInfo to MethodSignatureInfo for use with UnifiedInterceptorBuilder.
    /// </summary>
    private static MethodSignatureInfo ToMethodSignatureInfo(ClassMemberInfo member)
    {
        var parameters = member.Parameters
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
        var isVoid = member.ReturnType == "void";
        var defaultExpr = isVoid ? "" : "default!";

        return new MethodSignatureInfo(
            Parameters: parameters,
            TrackableParameters: trackableParams,
            ParameterDeclarations: UnifiedInterceptorBuilder.BuildParameterDeclarations(parameters),
            ReturnType: member.ReturnType,
            IsVoid: isVoid,
            HasRefOrOutParams: hasRefOrOut,
            DefaultExpression: defaultExpr,
            ThrowsOnDefault: false);
    }

    #endregion

    #region Model Building

    private static InlineClassPropertyModel BuildPropertyModel(
        ClassMemberInfo member,
        string stubClassName,
        string typeParamList,
        string constraintClause)
    {
        var interceptClassName = $"{stubClassName}_{member.Name}Interceptor";
        var stubClassRef = $"{stubClassName}{typeParamList}";

        return new InlineClassPropertyModel(
            InterceptorClassName: interceptClassName,
            PropertyName: member.Name,
            ReturnType: member.ReturnType,
            NullableReturnType: MakeNullable(member.ReturnType),
            HasGetter: member.HasGetter,
            HasSetter: member.HasSetter,
            IsRequired: member.IsRequired,
            StubClassName: stubClassRef,
            TypeParameterList: typeParamList,
            ConstraintClauses: constraintClause);
    }

    private static InlineClassIndexerModel BuildIndexerModel(
        ClassMemberInfo member,
        string stubClassName,
        int indexerCount,
        string typeParamList,
        string constraintClause)
    {
        var indexerName = SymbolHelpers.GetIndexerName(indexerCount, member.IndexerTypeSuffix);
        var interceptClassName = $"{stubClassName}_{indexerName}Interceptor";
        var stubClassRef = $"{stubClassName}{typeParamList}";

        var keyType = member.IndexerParameters.Count == 1
            ? member.IndexerParameters.GetArray()![0].Type
            : $"({string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"))})";

        var paramSig = string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"));
        var argList = string.Join(", ", member.IndexerParameters.Select(p => p.Name));
        var keyExpr = member.IndexerParameters.Count == 1
            ? member.IndexerParameters.GetArray()![0].Name
            : $"({argList})";

        return new InlineClassIndexerModel(
            InterceptorClassName: interceptClassName,
            IndexerName: indexerName,
            ReturnType: member.ReturnType,
            KeyType: keyType,
            HasGetter: member.HasGetter,
            HasSetter: member.HasSetter,
            ParameterDeclarations: paramSig,
            ArgumentList: argList,
            KeyExpression: keyExpr,
            StubClassName: stubClassRef,
            TypeParameterList: typeParamList,
            ConstraintClauses: constraintClause);
    }

    private static InlineClassEventModel BuildEventModel(
        EventMemberInfo evt,
        string stubClassName,
        string typeParamList,
        string constraintClause)
    {
        var interceptClassName = $"{stubClassName}_{evt.Name}Interceptor";
        var (raiseParams, raiseArgs, raiseReturnType, raiseReturnsValue, usesDynamicInvoke) =
            EventBuilderHelpers.GetRaiseMethodInfo(evt);
        return new InlineClassEventModel(
            InterceptorClassName: interceptClassName,
            EventName: evt.Name,
            DelegateType: evt.FullDelegateTypeName.TrimEnd('?'),
            TypeParameterList: typeParamList,
            ConstraintClauses: constraintClause,
            RaiseParameters: raiseParams,
            RaiseArguments: raiseArgs,
            RaiseReturnType: raiseReturnType,
            RaiseReturnsValue: raiseReturnsValue,
            UsesDynamicInvoke: usesDynamicInvoke);
    }

    private static InlineConstructorModel BuildConstructorModel(ClassConstructorInfo ctor, string typeParamList)
    {
        var paramList = string.Join(", ", ctor.Parameters.Select(p => $"{p.Type} {p.Name}"));
        var argList = string.Join(", ", ctor.Parameters.Select(p => p.Name));
        return new InlineConstructorModel(
            ParameterDeclarations: paramList,
            BaseCallArguments: argList);
    }

    private static InlineClassImplPropertyModel BuildImplPropertyModel(ClassMemberInfo member, bool hasUserOverride)
    {
        return new InlineClassImplPropertyModel(
            PropertyName: member.Name,
            ReturnType: member.ReturnType,
            AccessModifier: member.AccessModifier,
            IsRequired: member.IsRequired,
            HasGetter: member.HasGetter,
            HasSetter: member.HasSetter,
            IsInitOnly: member.IsInitOnly,
            IsAbstract: member.IsAbstract,
            HasUserOverride: hasUserOverride);
    }

    private static InlineClassImplIndexerModel BuildImplIndexerModel(ClassMemberInfo member, int indexerCount)
    {
        var indexerName = SymbolHelpers.GetIndexerName(indexerCount, member.IndexerTypeSuffix);
        var paramList = string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"));
        var argList = string.Join(", ", member.IndexerParameters.Select(p => p.Name));
        var keyArg = member.IndexerParameters.Count == 1
            ? member.IndexerParameters.GetArray()![0].Name
            : $"({argList})";

        return new InlineClassImplIndexerModel(
            IndexerName: indexerName,
            ReturnType: member.ReturnType,
            AccessModifier: member.AccessModifier,
            ParameterDeclarations: paramList,
            ArgumentList: argList,
            KeyExpression: keyArg,
            HasGetter: member.HasGetter,
            HasSetter: member.HasSetter,
            IsAbstract: member.IsAbstract,
            IsNullable: member.IsNullable,
            DefaultStrategy: member.DefaultStrategy,
            ConcreteTypeForNew: member.ConcreteTypeForNew);
    }

    private static InlineClassImplMethodModel BuildImplMethodModel(
        ClassMemberInfo member,
        string handlerName,
        string invokeSuffix,
        bool hasOverloads,
        bool hasUserOverride = false)
    {
        var paramList = string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
        var argList = string.Join(", ", member.Parameters.Select(p => FormatArgument(p)));
        var inputParams = GetInputParameters(member.Parameters).ToArray();
        var inputArgList = string.Join(", ", inputParams.Select(p => p.Name));

        var isVoid = member.ReturnType == "void";
        var isTask = member.ReturnType == "global::System.Threading.Tasks.Task";
        var isValueTask = member.ReturnType == "global::System.Threading.Tasks.ValueTask";

        var callArgs = inputArgList;

        return new InlineClassImplMethodModel(
            HandlerName: handlerName,
            MethodName: member.Name,
            ReturnType: member.ReturnType,
            AccessModifier: member.AccessModifier,
            IsVoid: isVoid,
            IsTask: isTask,
            IsValueTask: isValueTask,
            IsAbstract: member.IsAbstract,
            ParameterDeclarations: paramList,
            ArgumentList: argList,
            InputArgumentList: inputArgList,
            CallArgumentList: callArgs,
            InvokeSuffix: invokeSuffix,
            HasUserOverride: hasUserOverride);
    }

    /// <summary>
    /// Builds an InlineClassImplMethodModel for a generic method override.
    /// Generic methods use the Of&lt;T&gt;() handler pattern instead of Invoke.
    /// </summary>
    private static InlineClassImplMethodModel BuildImplGenericMethodModel(
        ClassMemberInfo member,
        string handlerName)
    {
        var typeParams = member.TypeParameters.GetArray()!;
        var typeParamNames = string.Join(", ", typeParams.Select(tp => tp.Name));
        var typeParamDecl = $"<{typeParamNames}>";

        // Build set of method-level type parameter names for filtering
        var typeParamSet = new HashSet<string>(typeParams.Select(tp => tp.Name));

        var paramList = string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
        var argList = string.Join(", ", member.Parameters.Select(p => FormatArgument(p)));

        // Non-generic parameters for RecordCall (exclude params typed with method-level type params)
        var nonGenericParams = member.Parameters
            .Where(p => !IsGenericParameterType(p.Type, typeParamSet))
            .ToArray();
        var nonGenericArgList = string.Join(", ", nonGenericParams.Select(p => p.Name));

        var isVoid = member.ReturnType == "void";

        // Detect Task<T>/ValueTask<T> return types for async handling
        var isTask = false;
        var isValueTask = false;
        var taskTypeArg = "";

        if (member.ReturnType.StartsWith("global::System.Threading.Tasks.Task<"))
        {
            isTask = true;
            taskTypeArg = member.ReturnType.Substring(
                "global::System.Threading.Tasks.Task<".Length,
                member.ReturnType.Length - "global::System.Threading.Tasks.Task<".Length - 1);
        }
        else if (member.ReturnType.StartsWith("global::System.Threading.Tasks.ValueTask<"))
        {
            isValueTask = true;
            taskTypeArg = member.ReturnType.Substring(
                "global::System.Threading.Tasks.ValueTask<".Length,
                member.ReturnType.Length - "global::System.Threading.Tasks.ValueTask<".Length - 1);
        }
        else if (member.ReturnType == "global::System.Threading.Tasks.Task")
        {
            isTask = true;
        }
        else if (member.ReturnType == "global::System.Threading.Tasks.ValueTask")
        {
            isValueTask = true;
        }

        return new InlineClassImplMethodModel(
            HandlerName: handlerName,
            MethodName: member.Name,
            ReturnType: member.ReturnType,
            AccessModifier: member.AccessModifier,
            IsVoid: isVoid,
            IsTask: isTask,
            IsValueTask: isValueTask,
            IsAbstract: member.IsAbstract,
            ParameterDeclarations: paramList,
            ArgumentList: argList,
            InputArgumentList: "", // Not used for generic methods
            CallArgumentList: "", // Not used for generic methods
            InvokeSuffix: "", // Not used for generic methods
            IsGenericMethod: true,
            TypeParameterDecl: typeParamDecl,
            ConstraintClauses: "", // Must be empty for overrides -- C# inherits constraints from base
            OfTypeAccess: $".Of<{typeParamNames}>()",
            NonGenericArgList: nonGenericArgList,
            TaskTypeArg: taskTypeArg);
    }

    /// <summary>
    /// Builds an InlineGenericMethodHandlerModel for a group of generic methods.
    /// Adapted from ClassModelBuilder.BuildGenericMethodHandlerModel.
    /// </summary>
    private static InlineGenericMethodHandlerModel BuildGenericMethodHandlerModel(
        List<ClassMemberInfo> genericMembers,
        string groupName,
        string methodName,
        string stubClassName,
        string classTypeParamList,
        string classConstraintClause,
        EquatableArray<TypeParameterInfo> classTypeParams)
    {
        var interceptClassName = $"{stubClassName}_{groupName}Interceptor";
        var stubClassRef = $"{stubClassName}{classTypeParamList}";

        // Get the first generic member's type parameters
        var firstMember = genericMembers[0];
        var typeParams = firstMember.TypeParameters.GetArray()!;
        var typeParamNames = string.Join(", ", typeParams.Select(tp => tp.Name));
        var typeParamCount = typeParams.Length;

        // Build constraint clauses for method type parameters
        var methodConstraintClauses = GetConstraintClauses(typeParams);

        // Get non-generic parameters (exclude params typed with method-level type params)
        var typeParamSet = new HashSet<string>(typeParams.Select(tp => tp.Name));
        var inputParams = firstMember.Parameters
            .Where(p => p.RefKind != RefKind.Out)
            .ToArray();
        var nonGenericParams = inputParams
            .Where(p => !IsGenericParameterType(p.Type, typeParamSet))
            .ToArray();

        // Build the dictionary key type
        var keyType = typeParamCount == 1
            ? "global::System.Type"
            : $"({string.Join(", ", typeParams.Select(_ => "global::System.Type"))})";

        var keyConstruction = typeParamCount == 1
            ? $"typeof({typeParams[0].Name})"
            : $"({string.Join(", ", typeParams.Select(tp => $"typeof({tp.Name})"))})";

        // Build delegate signature (all params, not just non-generic)
        var delegateReturnType = firstMember.ReturnType == "void" ? "void" : firstMember.ReturnType;
        var allParams = firstMember.Parameters.GetArray() ?? Array.Empty<ParameterInfo>();
        var delegateParams = new List<string>();
        foreach (var p in allParams)
        {
            delegateParams.Add($"{p.Type} {p.Name}");
        }
        var delegateParamList = string.Join(", ", delegateParams);
        var isVoid = firstMember.ReturnType == "void";
        var delegateSignature = isVoid
            ? $"public delegate void {methodName}Delegate({delegateParamList});"
            : $"public delegate {delegateReturnType} {methodName}Delegate({delegateParamList});";

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
            RefPrefix: GetRefKindPrefix(p.RefKind))).ToEquatableArray();

        return new InlineGenericMethodHandlerModel(
            InterceptorClassName: interceptClassName,
            MethodName: methodName,
            ReturnType: firstMember.ReturnType,
            IsVoid: isVoid,
            TypeParameterNames: typeParamNames,
            KeyType: keyType,
            KeyConstruction: keyConstruction,
            MethodConstraintClauses: methodConstraintClauses,
            TypedHandlerClassName: $"{methodName}TypedHandler",
            DelegateSignature: delegateSignature,
            NonGenericParameters: nonGenericParamModels,
            LastCallArgType: lastCallArgType,
            LastCallArgsType: lastCallArgsType,
            StubClassName: stubClassRef,
            InterfaceTypeParameterList: classTypeParamList,
            InterfaceConstraintClauses: classConstraintClause);
    }

    #endregion

    #region NeedsNewKeyword

    private static readonly HashSet<string> ObjectMemberNames = new(StringComparer.Ordinal)
    {
        "Equals", "GetHashCode", "ToString", "GetType"
    };

    private static bool NeedsNewKeyword(string interceptorName) =>
        ObjectMemberNames.Contains(interceptorName);

    #endregion

    #region Helper Methods

    private static IEnumerable<ParameterInfo> GetInputParameters(EquatableArray<ParameterInfo> parameters) =>
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

    #endregion
}
