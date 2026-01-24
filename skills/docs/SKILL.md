---
name: C# Framework Documentation
description: This skill should be used when the user asks to "create documentation", "write docs", "update documentation", "add code samples", "sync snippets", "create README", "document this framework", or mentions MarkdownSnippets, documentation samples, or keeping docs in sync with code. Provides comprehensive guidance for documenting C# open source frameworks.
version: 0.1.0
---

# C# Framework Documentation

Comprehensive documentation system for C# open source frameworks using MarkdownSnippets for code synchronization.

## Core Workflow

Documentation follows a two-agent pipeline:

1. **docs-architect** - Creates documentation structure with snippet placeholders (does NOT write code)
2. **docs-code-samples** - Creates compilable sample projects that fill the placeholders

Always run docs-architect first, then docs-code-samples.

## Documentation Structure

Standard structure for C# frameworks:

```
README.md                    # Project overview, quick start, evaluation guide
docs/
├── getting-started.md       # Installation, first usage
├── guides/                  # Feature-specific guides
│   ├── feature-a.md
│   └── feature-b.md
├── reference/               # API reference
│   └── api.md
└── migration/               # Migration guides
    └── from-other-lib.md
```

**Excluded from documentation workflow:** `docs/todos/`, `docs/plans/`, `docs/release-notes/`

## MarkdownSnippets Integration

### Snippet Placeholder Syntax

In markdown documentation, use this syntax for code placeholders:

```markdown
<!-- snippet: snippet-name -->
<!-- endSnippet -->
```

The docs-architect creates descriptive placeholder names. The docs-code-samples agent creates the actual code.

### Sample Code Location

Sample code lives in `src/docs/samples/` with this structure:

```
src/docs/samples/
├── Samples.csproj           # Main xUnit test project
├── GettingStartedSamples.cs # Samples for getting-started.md
├── FeatureASamples.cs       # Samples for guides/feature-a.md
└── Platforms/               # Platform-specific projects when needed
    ├── Blazor/
    └── AspNetCore/
```

### Sample Code Format

Each sample is a region-wrapped test method:

```csharp
#region snippet-name
[Fact]
public void SnippetName()
{
    // Actual compilable code
}
#endregion
```

MarkdownSnippets extracts the code between `#region` and `#endregion` markers.

## Using the Agents

### docs-architect Agent

Invoke when creating or restructuring documentation:

- Creates documentation files with proper structure
- Writes content targeted at expert .NET developers
- Creates descriptive snippet placeholders
- Does NOT write actual C# code samples

### docs-code-samples Agent

Invoke after docs-architect completes:

- Reads documentation to find snippet placeholders
- Creates sample projects in `src/docs/samples/`
- Writes compilable, tested C# code
- Ensures code matches placeholder descriptions

## MarkdownSnippets Setup

For projects without MarkdownSnippets configured, see `references/markdownsnippets-setup.md` for installation and configuration guidance.

## Writing Guidelines

### Target Audience

Documentation targets expert .NET and C# developers:

- No explanations of basic C# concepts
- No hand-holding or verbose tutorials
- Direct, technical language
- Focus on API usage and patterns

### Content Progression

Documentation progresses from introductory to detailed:

1. **README** - Quick evaluation: What does it do? Why use it?
2. **Getting Started** - Minimal setup to first working code
3. **Guides** - Feature deep-dives with examples
4. **Reference** - Complete API documentation

### Snippet Naming

Use descriptive, hierarchical names:

- `getting-started-install` - Installation snippet
- `getting-started-basic-usage` - First usage example
- `feature-a-setup` - Feature A setup code
- `feature-a-advanced` - Advanced Feature A usage

## Additional Resources

### Reference Files

For detailed guidance, consult:

- **`references/markdownsnippets-setup.md`** - MarkdownSnippets installation and configuration
- **`references/documentation-patterns.md`** - Common documentation patterns and templates
