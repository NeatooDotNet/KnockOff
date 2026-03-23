// src/Generator/Model/StandaloneClass/BaseClassMethodModel.cs
#nullable enable
using KnockOff;
using KnockOff.Model.Shared;

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
    /// <summary>Parameters for XML doc generation (shortened types).</summary>
    EquatableArray<ParameterModel> Parameters,
    // Ref return support
    /// <summary>True if the method returns by ref (ref T).</summary>
    bool ReturnsByRef = false,
    /// <summary>True if the method returns by ref readonly (ref readonly T).</summary>
    bool ReturnsByRefReadonly = false,
    /// <summary>Default assignments for out parameters (e.g., "a = default!; b = default!;").
    /// Used in virtual base methods where out params must be assigned before returning.
    /// Empty when the method has no out parameters.</summary>
    string OutParameterDefaults = "")
{
    /// <summary>True if the method returns by ref or ref readonly.</summary>
    public bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
    /// <summary>The ref/ref readonly prefix for the return type in signatures.</summary>
    public string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
}
