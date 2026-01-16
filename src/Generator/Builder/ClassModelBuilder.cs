// src/Generator/Builder/ClassModelBuilder.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using KnockOff.Model.Inline;
using KnockOff.Model.Shared;
using Microsoft.CodeAnalysis;

namespace KnockOff.Builder;

/// <summary>
/// Transforms ClassStubInfo into InlineClassStubModel.
/// All decision logic for "what to generate" for class stubs lives here.
/// </summary>
internal static class ClassModelBuilder
{
    public static InlineClassStubModel Build(ClassStubInfo cls)
    {
        var stubClassName = cls.Name;

        // Type parameters
        var typeParamList = SymbolHelpers.FormatTypeParameterList(cls.TypeParameters);
        var constraints = SymbolHelpers.FormatTypeConstraints(cls.TypeParameters);
        var constraintClause = string.IsNullOrEmpty(constraints) ? "" : $" {constraints}";

        // For open generic, replace the empty <> in FullName with actual type params
        var baseType = cls.IsOpenGeneric && typeParamList.Length > 0
            ? SymbolHelpers.ReplaceUnboundGeneric(cls.FullName, typeParamList)
            : cls.FullName;

        // Group methods by name for overload handling
        var methodGroups = GroupMethodsByName(cls.Members.Where(m => !m.IsProperty && !m.IsIndexer));

        // Count indexers to determine naming strategy
        var indexerCount = SymbolHelpers.CountClassIndexers(cls.Members);

        // Check for required members
        var requiredMembers = cls.Members.Where(m => m.IsProperty && m.IsRequired).ToList();
        var hasRequiredMembers = requiredMembers.Count > 0;
        var requiredMemberNames = requiredMembers.Select(m => m.Name).ToEquatableArray();

        // Build type parameters model
        var typeParameters = cls.TypeParameters.Select(tp => new TypeParameterModel(
            Name: tp.Name,
            Constraints: string.Join(", ", tp.Constraints))).ToEquatableArray();

        // Build models for interceptor classes
        var properties = new List<InlineClassPropertyModel>();
        var indexers = new List<InlineClassIndexerModel>();
        var methods = new List<InlineClassMethodModel>();
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
                    NeedsNewKeyword: false,
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
                    NeedsNewKeyword: false,
                    Description: $"Interceptor for {indexerName}."));
                resetStatements.Add($"{indexerName}.Reset();");
            }
        }

        // Build method interceptors
        foreach (var group in methodGroups.Values)
        {
            var hasOverloads = group.Members.Count > 1;
            for (int i = 0; i < group.Members.Count; i++)
            {
                var member = group.Members.GetArray()![i];
                var handlerName = hasOverloads ? $"{group.Name}{i + 1}" : group.Name;
                var methodModel = BuildMethodModel(member, stubClassName, handlerName, typeParamList, constraintClause);
                methods.Add(methodModel);
                interceptorProperties.Add(new InlineInterceptorPropertyModel(
                    PropertyName: handlerName,
                    InterceptorTypeName: $"{methodModel.InterceptorClassName}{typeParamList}",
                    NeedsNewKeyword: false,
                    Description: $"Interceptor for {group.Name}."));
                resetStatements.Add($"{handlerName}.Reset();");
            }
        }

        // Build event interceptors
        foreach (var evt in cls.Events)
        {
            var eventModel = BuildEventModel(evt, stubClassName, typeParamList, constraintClause);
            events.Add(eventModel);
            interceptorProperties.Add(new InlineInterceptorPropertyModel(
                PropertyName: evt.Name,
                InterceptorTypeName: $"{eventModel.InterceptorClassName}{typeParamList}",
                NeedsNewKeyword: false,
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
                implProperties.Add(BuildImplPropertyModel(member));
            }
            else if (member.IsIndexer)
            {
                implIndexers.Add(BuildImplIndexerModel(member, indexerCount));
            }
        }

        // Impl methods
        foreach (var group in methodGroups.Values)
        {
            var hasOverloads = group.Members.Count > 1;
            for (int i = 0; i < group.Members.Count; i++)
            {
                var member = group.Members.GetArray()![i];
                var handlerName = hasOverloads ? $"{group.Name}{i + 1}" : group.Name;
                implMethods.Add(BuildImplMethodModel(member, handlerName));
            }
        }

        // Impl events
        foreach (var evt in cls.Events)
        {
            implEvents.Add(new InlineClassImplEventModel(
                EventName: evt.Name,
                DelegateType: evt.FullDelegateTypeName.TrimEnd('?')));
        }

        return new InlineClassStubModel(
            StubClassName: stubClassName,
            ClassType: cls.FullName,
            BaseType: baseType,
            IsOpenGeneric: cls.IsOpenGeneric,
            TypeParameters: typeParameters,
            TypeParameterList: typeParamList,
            ConstraintClauses: constraintClause,
            Constructors: constructors,
            Properties: properties.ToEquatableArray(),
            Indexers: indexers.ToEquatableArray(),
            Methods: methods.ToEquatableArray(),
            Events: events.ToEquatableArray(),
            InterceptorProperties: interceptorProperties.ToEquatableArray(),
            ResetStatements: resetStatements.ToEquatableArray(),
            ImplProperties: implProperties.ToEquatableArray(),
            ImplIndexers: implIndexers.ToEquatableArray(),
            ImplMethods: implMethods.ToEquatableArray(),
            ImplEvents: implEvents.ToEquatableArray(),
            HasRequiredMembers: hasRequiredMembers,
            RequiredMemberNames: requiredMemberNames);
    }

    #region Model Building

    private static InlineClassPropertyModel BuildPropertyModel(
        ClassMemberInfo member,
        string stubClassName,
        string typeParamList,
        string constraintClause)
    {
        var interceptClassName = $"{stubClassName}_{member.Name}Interceptor";
        var stubClassRef = $"Stubs.{stubClassName}{typeParamList}";

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
        var stubClassRef = $"Stubs.{stubClassName}{typeParamList}";

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

    private static InlineClassMethodModel BuildMethodModel(
        ClassMemberInfo member,
        string stubClassName,
        string handlerName,
        string typeParamList,
        string constraintClause)
    {
        var interceptClassName = $"{stubClassName}_{handlerName}Interceptor";
        var stubClassRef = $"Stubs.{stubClassName}{typeParamList}";

        var inputParams = GetInputParameters(member.Parameters).ToArray();

        // Build delegate type
        var delegateParamTypes = string.Join(", ", inputParams.Select(p => p.Type));
        var delegateParams = string.IsNullOrEmpty(delegateParamTypes)
            ? stubClassRef
            : $"{stubClassRef}, {delegateParamTypes}";
        var isVoid = member.ReturnType == "void";
        var delegateType = isVoid
            ? $"global::System.Action<{delegateParams}>"
            : $"global::System.Func<{delegateParams}, {member.ReturnType}>";

        // Build input parameters model
        var inputParamModels = inputParams.Select(p => new ParameterModel(
            Name: p.Name,
            EscapedName: EscapeIdentifier(p.Name),
            Type: p.Type,
            NullableType: MakeNullable(p.Type),
            RefKind: p.RefKind,
            RefPrefix: GetRefKindPrefix(p.RefKind))).ToEquatableArray();

        // LastCallArg/Args types
        string? lastCallArgType = null;
        string? lastCallArgsType = null;
        if (inputParams.Length == 1)
        {
            lastCallArgType = MakeNullable(inputParams[0].Type);
        }
        else if (inputParams.Length > 1)
        {
            lastCallArgsType = $"({string.Join(", ", inputParams.Select(p => $"{MakeNullable(p.Type)} {p.Name}"))})?";
        }

        var paramDecl = string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
        var argList = string.Join(", ", member.Parameters.Select(p => FormatArgument(p)));

        return new InlineClassMethodModel(
            InterceptorClassName: interceptClassName,
            HandlerName: handlerName,
            MethodName: member.Name,
            ReturnType: member.ReturnType,
            IsVoid: isVoid,
            ParameterDeclarations: paramDecl,
            ArgumentList: argList,
            InputParameters: inputParamModels,
            DelegateType: delegateType,
            LastCallArgType: lastCallArgType,
            LastCallArgsType: lastCallArgsType,
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
        return new InlineClassEventModel(
            InterceptorClassName: interceptClassName,
            EventName: evt.Name,
            DelegateType: evt.FullDelegateTypeName.TrimEnd('?'),
            TypeParameterList: typeParamList,
            ConstraintClauses: constraintClause);
    }

    private static InlineConstructorModel BuildConstructorModel(ClassConstructorInfo ctor, string typeParamList)
    {
        var paramList = string.Join(", ", ctor.Parameters.Select(p => $"{p.Type} {p.Name}"));
        var argList = string.Join(", ", ctor.Parameters.Select(p => p.Name));
        return new InlineConstructorModel(
            ParameterDeclarations: paramList,
            BaseCallArguments: argList);
    }

    private static InlineClassImplPropertyModel BuildImplPropertyModel(ClassMemberInfo member)
    {
        return new InlineClassImplPropertyModel(
            PropertyName: member.Name,
            ReturnType: member.ReturnType,
            AccessModifier: member.AccessModifier,
            IsRequired: member.IsRequired,
            HasGetter: member.HasGetter,
            HasSetter: member.HasSetter,
            IsInitOnly: member.IsInitOnly,
            IsAbstract: member.IsAbstract);
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

    private static InlineClassImplMethodModel BuildImplMethodModel(ClassMemberInfo member, string handlerName)
    {
        var paramList = string.Join(", ", member.Parameters.Select(p => FormatParameter(p)));
        var argList = string.Join(", ", member.Parameters.Select(p => FormatArgument(p)));
        var inputParams = GetInputParameters(member.Parameters).ToArray();
        var inputArgList = string.Join(", ", inputParams.Select(p => p.Name));

        var isVoid = member.ReturnType == "void";
        var isTask = member.ReturnType == "global::System.Threading.Tasks.Task";
        var isValueTask = member.ReturnType == "global::System.Threading.Tasks.ValueTask";

        var onCallArgs = inputParams.Length > 0 ? $"_stub, {inputArgList}" : "_stub";

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
            OnCallArgumentList: onCallArgs);
    }

    #endregion

    #region Helper Methods

    private static Dictionary<string, ClassMethodGroupInfo> GroupMethodsByName(IEnumerable<ClassMemberInfo> methods)
    {
        var tempGroups = new Dictionary<string, List<ClassMemberInfo>>();

        foreach (var method in methods)
        {
            if (!tempGroups.TryGetValue(method.Name, out var list))
            {
                list = new List<ClassMemberInfo>();
                tempGroups[method.Name] = list;
            }
            list.Add(method);
        }

        return tempGroups.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var first = kvp.Value[0];
                return new ClassMethodGroupInfo(
                    kvp.Key,
                    first.ReturnType,
                    first.ReturnType == "void",
                    first.IsNullable,
                    kvp.Value);
            });
    }

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

    #endregion
}
