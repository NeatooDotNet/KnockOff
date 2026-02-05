// src/Generator/Model/StandaloneClass/BaseClassPropertyModel.cs
#nullable enable

namespace KnockOff.Model.StandaloneClass;

/// <summary>
/// Model for a virtual protected property in the base class.
/// Users can override these properties (suffixed with '_') to provide default behavior.
/// </summary>
internal sealed record BaseClassPropertyModel(
    /// <summary>The property name (without '_' suffix).</summary>
    string PropertyName,
    /// <summary>The property type (e.g., "string", "int").</summary>
    string ReturnType,
    /// <summary>Whether the property has a getter.</summary>
    bool HasGetter,
    /// <summary>Whether the property has a setter.</summary>
    bool HasSetter,
    /// <summary>The target class member description for XML doc.</summary>
    string TargetMemberDescription);
