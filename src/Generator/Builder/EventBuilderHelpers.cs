// src/Generator/Builder/EventBuilderHelpers.cs
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace KnockOff.Builder;

/// <summary>
/// Shared helper for computing event Raise() method info from EventMemberInfo.
/// Used by all four builders (FlatModelBuilder, InlineModelBuilder, ClassModelBuilder, StandaloneClassModelBuilder).
/// </summary>
internal static class EventBuilderHelpers
{
	/// <summary>
	/// Computes the Raise method signature components from an EventMemberInfo.
	/// Returns parameter declarations, argument names, return type, and delegate classification.
	/// </summary>
	internal static (string RaiseParams, string RaiseArgs, string RaiseReturnType, bool RaiseReturnsValue, bool UsesDynamicInvoke) GetRaiseMethodInfo(EventMemberInfo evt)
	{
		var paramArray = evt.DelegateParameters.GetArray() ?? Array.Empty<ParameterInfo>();

		switch (evt.DelegateKind)
		{
			case EventDelegateKind.EventHandler:
				return ("object? sender, global::System.EventArgs e", "sender, e", "void", false, false);

			case EventDelegateKind.EventHandlerOfT:
				var eventArgsType = paramArray.Length > 1 ? paramArray[1].Type : "global::System.EventArgs";
				return ($"object? sender, {eventArgsType} e", "sender, e", "void", false, false);

			case EventDelegateKind.Action:
				if (paramArray.Length == 0)
				{
					return ("", "", "void", false, false);
				}
				else
				{
					var paramDecls = string.Join(", ", paramArray.Select(p => $"{p.Type} {EscapeIdentifier(p.Name)}"));
					var paramNames = string.Join(", ", paramArray.Select(p => EscapeIdentifier(p.Name)));
					return (paramDecls, paramNames, "void", false, false);
				}

			case EventDelegateKind.Func:
				var funcParamDecls = string.Join(", ", paramArray.Select(p => $"{p.Type} {EscapeIdentifier(p.Name)}"));
				var funcParamNames = string.Join(", ", paramArray.Select(p => EscapeIdentifier(p.Name)));
				var returnType = evt.ReturnTypeName ?? "object";
				return (funcParamDecls, funcParamNames, returnType, true, false);

			case EventDelegateKind.Custom:
			default:
				// For custom delegates, generate a generic Raise
				if (paramArray.Length == 0)
				{
					return ("", "", "void", false, true);
				}
				else
				{
					var customParamDecls = string.Join(", ", paramArray.Select(p => $"{p.Type} {EscapeIdentifier(p.Name)}"));
					var customParamNames = string.Join(", ", paramArray.Select(p => EscapeIdentifier(p.Name)));
					return (customParamDecls, customParamNames, "void", false, true);
				}
		}
	}

	/// <summary>
	/// Escapes C# reserved keywords by prefixing with @.
	/// Private copy for event Raise parameter generation.
	/// </summary>
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
}
