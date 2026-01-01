using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KnockOff;

[Generator(LanguageNames.CSharp)]
public class KnockOffGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var classesToGenerate = context.SyntaxProvider.ForAttributeWithMetadataName(
			"KnockOff.KnockOffAttribute",
			predicate: static (node, _) => IsCandidateClass(node),
			transform: static (ctx, _) => TransformClass(ctx));

		context.RegisterSourceOutput(classesToGenerate, static (spc, typeInfo) =>
		{
			if (typeInfo is not null)
			{
				GenerateKnockOff(spc, typeInfo);
			}
		});
	}

	/// <summary>
	/// Predicate: partial class, not abstract, not generic, implements at least one interface
	/// </summary>
	private static bool IsCandidateClass(SyntaxNode node)
	{
		if (node is not ClassDeclarationSyntax classDecl)
			return false;

		// Must be partial
		if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
			return false;

		// Must not be abstract
		if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)))
			return false;

		// Must not be generic (Phase 1 limitation)
		if (classDecl.TypeParameterList?.Parameters.Count > 0)
			return false;

		// Must have base list (potential interfaces)
		if (classDecl.BaseList is null || classDecl.BaseList.Types.Count == 0)
			return false;

		return true;
	}

	/// <summary>
	/// Transform: extract all interface members and user-defined methods
	/// </summary>
	private static KnockOffTypeInfo? TransformClass(GeneratorAttributeSyntaxContext context)
	{
		var classDeclaration = (ClassDeclarationSyntax)context.TargetNode;
		var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

		if (classSymbol is null)
			return null;

		// Get namespace
		var ns = classSymbol.ContainingNamespace;
		var namespaceName = ns.IsGlobalNamespace ? "" : ns.ToDisplayString();

		// Get all implemented interfaces
		var interfaces = classSymbol.AllInterfaces;
		if (interfaces.Length == 0)
			return null;

		var interfaceInfos = new List<InterfaceInfo>();

		foreach (var iface in interfaces)
		{
			var members = new List<InterfaceMemberInfo>();

			foreach (var member in iface.GetMembers())
			{
				if (member is IPropertySymbol property)
				{
					members.Add(CreatePropertyInfo(property));
				}
				else if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
				{
					members.Add(CreateMethodInfo(method));
				}
			}

			if (members.Count > 0)
			{
				// Extract simple interface name for AsXYZ() generation
				var simpleName = GetSimpleInterfaceName(iface.Name);

				interfaceInfos.Add(new InterfaceInfo(
					iface.ToDisplayString(),
					simpleName,
					new EquatableArray<InterfaceMemberInfo>(members.ToArray())));
			}
		}

		// Get user-defined methods that could override interface methods
		var userMethods = GetUserDefinedMethods(classSymbol, interfaceInfos);

		return new KnockOffTypeInfo(
			Namespace: namespaceName,
			ClassName: classSymbol.Name,
			Interfaces: new EquatableArray<InterfaceInfo>(interfaceInfos.ToArray()),
			UserMethods: userMethods);
	}

	/// <summary>
	/// Gets simple name for AsXYZ() method (strips leading 'I' if followed by uppercase)
	/// </summary>
	private static string GetSimpleInterfaceName(string interfaceName)
	{
		// If starts with 'I' followed by uppercase letter, strip the 'I'
		if (interfaceName.Length > 1 &&
			interfaceName[0] == 'I' &&
			char.IsUpper(interfaceName[1]))
		{
			return interfaceName.Substring(1);
		}
		return interfaceName;
	}

	private static InterfaceMemberInfo CreatePropertyInfo(IPropertySymbol property)
	{
		var returnType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var isNullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated
			|| property.Type.IsReferenceType && property.Type.NullableAnnotation != NullableAnnotation.NotAnnotated;

		return new InterfaceMemberInfo(
			Name: property.Name,
			ReturnType: returnType,
			IsProperty: true,
			HasGetter: property.GetMethod is not null,
			HasSetter: property.SetMethod is not null,
			IsNullable: isNullable,
			Parameters: EquatableArray<ParameterInfo>.Empty);
	}

	private static InterfaceMemberInfo CreateMethodInfo(IMethodSymbol method)
	{
		var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var isNullable = method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated
			|| (method.ReturnType.IsReferenceType && method.ReturnType.NullableAnnotation != NullableAnnotation.NotAnnotated);

		// For void methods, they're not "nullable" in the sense that matters
		if (method.ReturnsVoid)
			isNullable = true; // void can't throw for missing return

		var parameters = method.Parameters
			.Select(p => new ParameterInfo(p.Name, p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
			.ToArray();

		return new InterfaceMemberInfo(
			Name: method.Name,
			ReturnType: returnType,
			IsProperty: false,
			HasGetter: false,
			HasSetter: false,
			IsNullable: isNullable,
			Parameters: new EquatableArray<ParameterInfo>(parameters));
	}

	/// <summary>
	/// Find user-defined protected methods that match interface method signatures
	/// </summary>
	private static EquatableArray<UserMethodInfo> GetUserDefinedMethods(
		INamedTypeSymbol classSymbol,
		List<InterfaceInfo> interfaces)
	{
		var userMethods = new List<UserMethodInfo>();

		// Get all methods declared directly on this class (not inherited)
		var classMethods = classSymbol.GetMembers()
			.OfType<IMethodSymbol>()
			.Where(m => m.MethodKind == MethodKind.Ordinary
				&& m.DeclaredAccessibility == Accessibility.Protected
				&& !m.IsStatic)
			.ToList();

		// Build a set of interface method signatures to match against
		var interfaceMethodSignatures = new HashSet<string>();
		foreach (var iface in interfaces)
		{
			foreach (var member in iface.Members)
			{
				if (!member.IsProperty)
				{
					var sig = GetMethodSignature(member.Name, member.ReturnType, member.Parameters);
					interfaceMethodSignatures.Add(sig);
				}
			}
		}

		// Find matching user methods
		foreach (var method in classMethods)
		{
			var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			var parameters = method.Parameters
				.Select(p => new ParameterInfo(p.Name, p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
				.ToArray();

			var sig = GetMethodSignature(method.Name, returnType, new EquatableArray<ParameterInfo>(parameters));

			if (interfaceMethodSignatures.Contains(sig))
			{
				userMethods.Add(new UserMethodInfo(
					Name: method.Name,
					ReturnType: returnType,
					Parameters: new EquatableArray<ParameterInfo>(parameters)));
			}
		}

		return new EquatableArray<UserMethodInfo>(userMethods.ToArray());
	}

	private static string GetMethodSignature(string name, string returnType, EquatableArray<ParameterInfo> parameters)
	{
		var paramTypes = string.Join(",", parameters.Select(p => p.Type));
		return $"{returnType} {name}({paramTypes})";
	}

	/// <summary>
	/// Generate the partial class with explicit interface implementations
	/// </summary>
	private static void GenerateKnockOff(SourceProductionContext context, KnockOffTypeInfo typeInfo)
	{
		var sb = new System.Text.StringBuilder();

		sb.AppendLine("// <auto-generated/>");
		sb.AppendLine("#nullable enable");
		sb.AppendLine();

		if (!string.IsNullOrEmpty(typeInfo.Namespace))
		{
			sb.AppendLine($"namespace {typeInfo.Namespace};");
			sb.AppendLine();
		}

		sb.AppendLine($"partial class {typeInfo.ClassName}");
		sb.AppendLine("{");

		// Collect unique members across all interfaces
		var processedMembers = new Dictionary<string, InterfaceMemberInfo>();
		foreach (var iface in typeInfo.Interfaces)
		{
			foreach (var member in iface.Members)
			{
				var memberKey = member.IsProperty
					? $"prop:{member.Name}"
					: GetMethodSignature(member.Name, member.ReturnType, member.Parameters);

				if (!processedMembers.ContainsKey(memberKey))
				{
					processedMembers[memberKey] = member;
				}
			}
		}

		// 1. Generate per-member ExecutionDetails classes
		foreach (var kvp in processedMembers)
		{
			GenerateMemberExecutionDetailsClass(sb, kvp.Value);
		}

		// 2. Generate ExecutionInfo class
		GenerateExecutionInfoClass(sb, typeInfo.ClassName, processedMembers.Values);

		// 3. Generate ExecutionInfo property
		sb.AppendLine("\t/// <summary>Tracks all interface member invocations for test verification.</summary>");
		sb.AppendLine($"\tpublic {typeInfo.ClassName}ExecutionInfo ExecutionInfo {{ get; }} = new();");
		sb.AppendLine();

		// 4. Generate AsXYZ() methods for each interface
		var generatedAsMethodNames = new HashSet<string>();
		foreach (var iface in typeInfo.Interfaces)
		{
			var methodName = $"As{iface.SimpleName}";
			if (!generatedAsMethodNames.Contains(methodName))
			{
				generatedAsMethodNames.Add(methodName);
				sb.AppendLine($"\t/// <summary>Returns this instance as {iface.FullName}.</summary>");
				sb.AppendLine($"\tpublic {iface.FullName} {methodName}() => this;");
				sb.AppendLine();
			}
		}

		// 5. Generate backing members and explicit implementations for each interface
		var processedMemberKeys = new HashSet<string>();
		foreach (var iface in typeInfo.Interfaces)
		{
			foreach (var member in iface.Members)
			{
				var memberKey = member.IsProperty
					? $"prop:{member.Name}"
					: GetMethodSignature(member.Name, member.ReturnType, member.Parameters);

				if (processedMemberKeys.Contains(memberKey))
					continue;
				processedMemberKeys.Add(memberKey);

				if (member.IsProperty)
				{
					GenerateProperty(sb, iface.FullName, member, typeInfo);
				}
				else
				{
					GenerateMethod(sb, iface.FullName, member, typeInfo);
				}
			}
		}

		sb.AppendLine("}");

		context.AddSource($"{typeInfo.ClassName}.g.cs", sb.ToString());
	}

	/// <summary>
	/// Generate a per-member ExecutionDetails class with strongly-typed tracking
	/// </summary>
	private static void GenerateMemberExecutionDetailsClass(System.Text.StringBuilder sb, InterfaceMemberInfo member)
	{
		sb.AppendLine($"\t/// <summary>Execution tracking for {member.Name}.</summary>");
		sb.AppendLine($"\tpublic sealed class {member.Name}ExecutionDetails");
		sb.AppendLine("\t{");

		if (member.IsProperty)
		{
			// Property tracking: GetCount, SetCount, LastSetValue (typed)
			if (member.HasGetter)
			{
				sb.AppendLine("\t\t/// <summary>Number of times the getter was accessed.</summary>");
				sb.AppendLine("\t\tpublic int GetCount { get; private set; }");
				sb.AppendLine();
			}

			if (member.HasSetter)
			{
				sb.AppendLine("\t\t/// <summary>Number of times the setter was accessed.</summary>");
				sb.AppendLine("\t\tpublic int SetCount { get; private set; }");
				sb.AppendLine();

				// Determine nullable type for LastSetValue
				var nullableType = MakeNullable(member.ReturnType);
				sb.AppendLine("\t\t/// <summary>The value from the most recent setter call.</summary>");
				sb.AppendLine($"\t\tpublic {nullableType} LastSetValue {{ get; private set; }}");
				sb.AppendLine();
			}

			// RecordGet method
			if (member.HasGetter)
			{
				sb.AppendLine("\t\t/// <summary>Records a getter access.</summary>");
				sb.AppendLine("\t\tpublic void RecordGet() => GetCount++;");
				sb.AppendLine();
			}

			// RecordSet method (typed)
			if (member.HasSetter)
			{
				var nullableType = MakeNullable(member.ReturnType);
				sb.AppendLine("\t\t/// <summary>Records a setter access.</summary>");
				sb.AppendLine($"\t\tpublic void RecordSet({nullableType} value) {{ SetCount++; LastSetValue = value; }}");
				sb.AppendLine();
			}

			// Reset method
			sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
			sb.Append("\t\tpublic void Reset() { ");
			if (member.HasGetter) sb.Append("GetCount = 0; ");
			if (member.HasSetter) sb.Append("SetCount = 0; LastSetValue = default; ");
			sb.AppendLine("}");
		}
		else
		{
			// Method tracking: CallCount, WasCalled, LastCallArgs, AllCalls
			var paramCount = member.Parameters.Count;

			if (paramCount == 1)
			{
				// Single parameter - store directly (tuples require 2+ elements)
				var param = member.Parameters.GetArray()![0];
				var nullableType = MakeNullable(param.Type);

				sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.List<{param.Type}> _calls = new();");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
				sb.AppendLine("\t\tpublic int CallCount => _calls.Count;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
				sb.AppendLine("\t\tpublic bool WasCalled => _calls.Count > 0;");
				sb.AppendLine();
				sb.AppendLine($"\t\t/// <summary>The '{param.Name}' argument from the most recent call.</summary>");
				sb.AppendLine($"\t\tpublic {nullableType} LastCallArg => _calls.Count > 0 ? _calls[_calls.Count - 1] : default;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>All recorded calls with their arguments.</summary>");
				sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{param.Type}> AllCalls => _calls;");
				sb.AppendLine();

				sb.AppendLine("\t\t/// <summary>Records a method call.</summary>");
				sb.AppendLine($"\t\tpublic void RecordCall({param.Type} {param.Name}) => _calls.Add({param.Name});");
				sb.AppendLine();

				sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
				sb.AppendLine("\t\tpublic void Reset() => _calls.Clear();");
			}
			else if (paramCount > 1)
			{
				// Multiple parameters - use tuple
				var tupleType = GetTupleType(member.Parameters);

				sb.AppendLine($"\t\tprivate readonly global::System.Collections.Generic.List<{tupleType}> _calls = new();");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
				sb.AppendLine("\t\tpublic int CallCount => _calls.Count;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
				sb.AppendLine("\t\tpublic bool WasCalled => _calls.Count > 0;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>The arguments from the most recent call.</summary>");
				sb.AppendLine($"\t\tpublic {tupleType}? LastCallArgs => _calls.Count > 0 ? _calls[_calls.Count - 1] : null;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>All recorded calls with their arguments.</summary>");
				sb.AppendLine($"\t\tpublic global::System.Collections.Generic.IReadOnlyList<{tupleType}> AllCalls => _calls;");
				sb.AppendLine();

				// RecordCall with typed parameters
				var paramList = string.Join(", ", member.Parameters.Select(p => $"{p.Type} {p.Name}"));
				var tupleConstruction = GetTupleConstruction(member.Parameters);
				sb.AppendLine("\t\t/// <summary>Records a method call.</summary>");
				sb.AppendLine($"\t\tpublic void RecordCall({paramList}) => _calls.Add({tupleConstruction});");
				sb.AppendLine();

				sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
				sb.AppendLine("\t\tpublic void Reset() => _calls.Clear();");
			}
			else
			{
				// No parameters - just track call count
				sb.AppendLine("\t\t/// <summary>Number of times this method was called.</summary>");
				sb.AppendLine("\t\tpublic int CallCount { get; private set; }");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>True if this method was called at least once.</summary>");
				sb.AppendLine("\t\tpublic bool WasCalled => CallCount > 0;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>Records a method call.</summary>");
				sb.AppendLine("\t\tpublic void RecordCall() => CallCount++;");
				sb.AppendLine();
				sb.AppendLine("\t\t/// <summary>Resets all tracking state.</summary>");
				sb.AppendLine("\t\tpublic void Reset() => CallCount = 0;");
			}
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	/// <summary>
	/// Makes a type nullable if it isn't already
	/// </summary>
	private static string MakeNullable(string type)
	{
		if (type.EndsWith("?"))
			return type;

		// Value types need ? suffix, reference types are already nullable with #nullable enable
		// But for clarity and to handle both, we add ? if not present
		return type + "?";
	}

	/// <summary>
	/// Gets the tuple type string for method parameters: (T1 name1, T2 name2, ...)
	/// </summary>
	private static string GetTupleType(EquatableArray<ParameterInfo> parameters)
	{
		var elements = parameters.Select(p => $"{p.Type} {p.Name}");
		return $"({string.Join(", ", elements)})";
	}

	/// <summary>
	/// Gets the tuple construction expression: (name1, name2, ...)
	/// </summary>
	private static string GetTupleConstruction(EquatableArray<ParameterInfo> parameters)
	{
		var elements = parameters.Select(p => p.Name);
		return $"({string.Join(", ", elements)})";
	}

	private static void GenerateExecutionInfoClass(
		System.Text.StringBuilder sb,
		string className,
		IEnumerable<InterfaceMemberInfo> members)
	{
		sb.AppendLine($"\t/// <summary>Execution tracking for {className}.</summary>");
		sb.AppendLine($"\tpublic sealed class {className}ExecutionInfo");
		sb.AppendLine("\t{");

		foreach (var member in members)
		{
			sb.AppendLine($"\t\t/// <summary>Tracks invocations of {member.Name}.</summary>");
			sb.AppendLine($"\t\tpublic {member.Name}ExecutionDetails {member.Name} {{ get; }} = new();");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	private static void GenerateProperty(
		System.Text.StringBuilder sb,
		string interfaceName,
		InterfaceMemberInfo prop,
		KnockOffTypeInfo typeInfo)
	{
		// Generate backing property
		var accessors = "";
		if (prop.HasGetter && prop.HasSetter)
			accessors = "get; set;";
		else if (prop.HasGetter)
			accessors = "get;";
		else if (prop.HasSetter)
			accessors = "set;";

		var defaultValue = prop.IsNullable ? "" : GetDefaultValue(prop.ReturnType);
		var initializer = !string.IsNullOrEmpty(defaultValue) ? $" = {defaultValue};" : "";

		sb.AppendLine($"\t/// <summary>Backing field for {prop.Name}.</summary>");
		sb.AppendLine($"\tprotected {prop.ReturnType} {prop.Name}Backing {{ {accessors} }}{initializer}");
		sb.AppendLine();

		// Generate explicit interface implementation
		sb.AppendLine($"\t{prop.ReturnType} {interfaceName}.{prop.Name}");
		sb.AppendLine("\t{");

		if (prop.HasGetter)
		{
			sb.AppendLine("\t\tget");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tExecutionInfo.{prop.Name}.RecordGet();");
			sb.AppendLine($"\t\t\treturn {prop.Name}Backing;");
			sb.AppendLine("\t\t}");
		}

		if (prop.HasSetter)
		{
			sb.AppendLine("\t\tset");
			sb.AppendLine("\t\t{");
			sb.AppendLine($"\t\t\tExecutionInfo.{prop.Name}.RecordSet(value);");
			sb.AppendLine($"\t\t\t{prop.Name}Backing = value;");
			sb.AppendLine("\t\t}");
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	private static void GenerateMethod(
		System.Text.StringBuilder sb,
		string interfaceName,
		InterfaceMemberInfo method,
		KnockOffTypeInfo typeInfo)
	{
		var paramList = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
		var argList = string.Join(", ", method.Parameters.Select(p => p.Name));

		// Check if user defined this method
		var hasUserMethod = typeInfo.UserMethods.Any(um =>
			um.Name == method.Name &&
			um.ReturnType == method.ReturnType &&
			um.Parameters.Count == method.Parameters.Count &&
			um.Parameters.Zip(method.Parameters, (a, b) => a.Type == b.Type).All(x => x));

		var isVoid = method.ReturnType == "void";

		sb.AppendLine($"\t{method.ReturnType} {interfaceName}.{method.Name}({paramList})");
		sb.AppendLine("\t{");

		// Record the call (now strongly typed)
		if (method.Parameters.Count > 0)
		{
			sb.AppendLine($"\t\tExecutionInfo.{method.Name}.RecordCall({argList});");
		}
		else
		{
			sb.AppendLine($"\t\tExecutionInfo.{method.Name}.RecordCall();");
		}

		if (hasUserMethod)
		{
			// Call user-defined method
			if (isVoid)
			{
				sb.AppendLine($"\t\t{method.Name}({argList});");
			}
			else
			{
				sb.AppendLine($"\t\treturn {method.Name}({argList});");
			}
		}
		else if (!isVoid)
		{
			// No user method - return default or throw
			if (method.IsNullable)
			{
				sb.AppendLine($"\t\treturn default!;");
			}
			else
			{
				sb.AppendLine($"\t\tthrow new global::System.InvalidOperationException(\"No implementation provided for non-nullable return type. Define a protected method '{method.Name}' in your partial class.\");");
			}
		}

		sb.AppendLine("\t}");
		sb.AppendLine();
	}

	private static string GetDefaultValue(string typeName)
	{
		// Handle common value types that need explicit defaults
		if (typeName == "global::System.String" || typeName == "string")
			return "\"\"";
		return "";
	}
}

#region Transform Model Types (Equatable for Incremental Generation)

internal sealed record KnockOffTypeInfo(
	string Namespace,
	string ClassName,
	EquatableArray<InterfaceInfo> Interfaces,
	EquatableArray<UserMethodInfo> UserMethods) : IEquatable<KnockOffTypeInfo>;

internal sealed record InterfaceInfo(
	string FullName,
	string SimpleName,
	EquatableArray<InterfaceMemberInfo> Members) : IEquatable<InterfaceInfo>;

internal sealed record InterfaceMemberInfo(
	string Name,
	string ReturnType,
	bool IsProperty,
	bool HasGetter,
	bool HasSetter,
	bool IsNullable,
	EquatableArray<ParameterInfo> Parameters) : IEquatable<InterfaceMemberInfo>;

internal sealed record ParameterInfo(
	string Name,
	string Type) : IEquatable<ParameterInfo>;

internal sealed record UserMethodInfo(
	string Name,
	string ReturnType,
	EquatableArray<ParameterInfo> Parameters) : IEquatable<UserMethodInfo>;

#endregion
