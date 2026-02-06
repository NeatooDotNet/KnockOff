// src/Generator/Model/StandaloneClass/BaseClassMethodModel.cs
#nullable enable
using KnockOff;

namespace KnockOff.Model.StandaloneClass;

/// <summary>
/// Model for a virtual protected method in the base class.
/// Users can override these methods (suffixed with '_') to provide default behavior.
/// </summary>
internal sealed record BaseClassMethodModel(
    /// <summary>The method name (without '_' suffix).</summary>
    string MethodName,
    /// <summary>The method return type (e.g., "void", "string", "T?").</summary>
    string ReturnType,
    /// <summary>Parameter declarations (e.g., "string command, int count").</summary>
    string ParameterDeclarations,
    /// <summary>Whether the method returns void.</summary>
    bool IsVoid,
    /// <summary>Whether the method is abstract (vs virtual).</summary>
    bool IsAbstract,
    /// <summary>The target class member description for XML doc.</summary>
    string TargetMemberDescription);
