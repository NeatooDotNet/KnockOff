// src/Generator/Model/Shared/SourceProviderInfo.cs
#nullable enable

namespace KnockOff.Model.Shared;

/// <summary>
/// Represents a Source(TInterface) method to generate for the stub.
/// Each interface in the inheritance hierarchy gets its own Source overload.
/// </summary>
internal sealed record SourceProviderInfo(
	/// <summary>
	/// The fully qualified interface type for this source method.
	/// E.g., "global::System.Collections.Generic.IList&lt;string&gt;"
	/// </summary>
	string InterfaceType,

	/// <summary>
	/// The method name for this source method.
	/// Usually "Source", but may need suffix for collision avoidance.
	/// </summary>
	string MethodName,

	/// <summary>
	/// Mappings from interceptor names to their source interface types.
	/// Each mapping specifies which interface provides the _source for that interceptor.
	/// </summary>
	EquatableArray<SourceMemberMapping> MemberMappings) : IEquatable<SourceProviderInfo>;

/// <summary>
/// Maps an interceptor to its source interface for a specific Source(T) overload.
/// </summary>
internal sealed record SourceMemberMapping(
	/// <summary>
	/// The interceptor name (e.g., "Count", "Add", "Indexer").
	/// </summary>
	string InterceptorName,

	/// <summary>
	/// The interface type that provides this member's source.
	/// This is used as the _source field type in the interceptor.
	/// E.g., "global::System.Collections.Generic.ICollection&lt;string&gt;" for Count.
	/// </summary>
	string SourceInterfaceType,

	/// <summary>
	/// If true, set _source = source; if false, clear _source = null.
	/// False when the source interface doesn't cover this member.
	/// </summary>
	bool SetSource) : IEquatable<SourceMemberMapping>;
