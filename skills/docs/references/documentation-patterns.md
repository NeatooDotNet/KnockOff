# Documentation Patterns

Common patterns and templates for C# framework documentation.

## README Structure

A compelling README for framework evaluation:

```markdown
# Framework Name

Brief tagline describing what the framework does.

## Why Framework Name?

2-3 sentences on the core value proposition. What problem does it solve better than alternatives?

## Quick Example

<!-- snippet: readme-teaser -->
<!-- endSnippet -->

## Installation

<!-- snippet: readme-install -->
<!-- endSnippet -->

## Documentation

- [Getting Started](docs/getting-started.md)
- [Guides](docs/guides/)
- [API Reference](docs/reference/)
- [Migration from X](docs/migration/from-x.md)

## License

[License type]
```

## Getting Started Template

```markdown
# Getting Started

## Installation

<!-- snippet: getting-started-install -->
<!-- endSnippet -->

## Your First [Thing]

Create a simple example:

<!-- snippet: getting-started-first -->
<!-- endSnippet -->

## Configuration

Optional configuration options:

<!-- snippet: getting-started-config -->
<!-- endSnippet -->

## Next Steps

- [Feature A Guide](guides/feature-a.md) - Deep dive into Feature A
- [Feature B Guide](guides/feature-b.md) - Learn about Feature B
- [API Reference](reference/api.md) - Complete API documentation
```

## Feature Guide Template

```markdown
# Feature Name

Brief description of what this feature does.

## Basic Usage

<!-- snippet: feature-basic -->
<!-- endSnippet -->

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `OptionA` | `string` | `null` | Description |
| `OptionB` | `bool` | `false` | Description |

## Common Patterns

### Pattern 1: [Name]

<!-- snippet: feature-pattern-1 -->
<!-- endSnippet -->

### Pattern 2: [Name]

<!-- snippet: feature-pattern-2 -->
<!-- endSnippet -->

## Advanced Usage

### [Advanced Topic]

<!-- snippet: feature-advanced -->
<!-- endSnippet -->

## Troubleshooting

### Issue: [Common Problem]

**Symptom:** Description of what goes wrong

**Solution:** How to fix it

<!-- snippet: feature-troubleshoot -->
<!-- endSnippet -->
```

## API Reference Template

```markdown
# API Reference

## Core Types

### `ClassName`

Description of the class purpose.

#### Constructor

<!-- snippet: api-classname-ctor -->
<!-- endSnippet -->

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `PropA` | `string` | Description |
| `PropB` | `int` | Description |

#### Methods

##### `MethodName(params)`

Description of what the method does.

**Parameters:**
- `param1` (`Type`) - Description
- `param2` (`Type`) - Description

**Returns:** `ReturnType` - Description

<!-- snippet: api-classname-methodname -->
<!-- endSnippet -->
```

## Migration Guide Template

```markdown
# Migrating from [Other Library]

## Overview

Key differences between [Other Library] and [This Framework]:

| Aspect | [Other Library] | [This Framework] |
|--------|-----------------|------------------|
| Approach | Description | Description |
| Performance | Description | Description |
| API Style | Description | Description |

## Step-by-Step Migration

### Step 1: Installation

Remove [Other Library]:
```bash
dotnet remove package OtherLibrary
```

Add [This Framework]:
<!-- snippet: migration-install -->
<!-- endSnippet -->

### Step 2: [Specific Change]

**Before ([Other Library]):**
<!-- snippet: migration-before-1 -->
<!-- endSnippet -->

**After ([This Framework]):**
<!-- snippet: migration-after-1 -->
<!-- endSnippet -->

### Step 3: [Another Change]

**Before:**
<!-- snippet: migration-before-2 -->
<!-- endSnippet -->

**After:**
<!-- snippet: migration-after-2 -->
<!-- endSnippet -->

## Common Migration Issues

### Issue: [Problem]

**Solution:** Description and example
```

## Snippet Naming Conventions

### Hierarchical Names

Use consistent, descriptive naming:

| Document | Snippet Pattern | Example |
|----------|-----------------|---------|
| README | `readme-{section}` | `readme-teaser`, `readme-install` |
| Getting Started | `getting-started-{topic}` | `getting-started-first`, `getting-started-config` |
| Feature Guides | `{feature}-{topic}` | `methods-oncall`, `properties-value` |
| API Reference | `api-{class}-{member}` | `api-service-create`, `api-options-configure` |
| Migration | `migration-{before|after}-{n}` | `migration-before-1`, `migration-after-1` |

### Naming Rules

1. Use lowercase with hyphens
2. Be descriptive but concise
3. Group by document, then by section
4. Number sequences when order matters

## Content Guidelines

### For Expert Developers

**Do:**
- Jump straight to code
- Use technical terminology
- Show realistic examples
- Include edge cases

**Don't:**
- Explain basic C# concepts
- Over-explain obvious things
- Use verbose introductions
- Include placeholder comments like "// Your code here"

### Code Sample Quality

Every snippet should:
- Compile without errors
- Run as a passing test
- Demonstrate one concept clearly
- Use realistic (not toy) examples

### Progressive Detail

1. **README**: Just enough to evaluate
2. **Getting Started**: Minimal viable usage
3. **Guides**: Complete feature coverage
4. **Reference**: Every option and method

## Placeholder Descriptions

When docs-architect creates placeholders, include enough context for docs-code-samples:

**Good placeholder context:**
```markdown
Show how to configure method returns with a callback that receives the arguments:

<!-- snippet: methods-oncall-with-args -->
<!-- endSnippet -->
```

**Bad placeholder context:**
```markdown
<!-- snippet: methods-example -->
<!-- endSnippet -->
```

The description above the snippet tells docs-code-samples exactly what to create.
