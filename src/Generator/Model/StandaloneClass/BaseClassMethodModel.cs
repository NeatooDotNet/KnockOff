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
    string TargetMemberDescription,
    // Ref return support
    /// <summary>True if the method returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if the method returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false)
{
    /// <summary>True if the method returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}
