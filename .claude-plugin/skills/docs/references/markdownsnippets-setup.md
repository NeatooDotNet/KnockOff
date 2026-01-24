# MarkdownSnippets Setup

MarkdownSnippets is a tool by Simon Cropp that keeps code samples in documentation synchronized with actual compilable code.

## How It Works

1. Code samples live in C# files with `#region snippet-name` markers
2. Markdown files contain `<!-- snippet: snippet-name -->` placeholders
3. MarkdownSnippets extracts code from regions and injects into markdown
4. Running the tool keeps documentation in sync with code changes

## Installation Options

### Option 1: .NET Global Tool (Recommended)

```bash
dotnet tool install -g MarkdownSnippets.Tool
```

Run manually:
```bash
mdsnippets
```

### Option 2: NuGet Package for Build Integration

Add to your test/samples project:

```xml
<ItemGroup>
  <PackageReference Include="MarkdownSnippets.MsBuild" Version="*" PrivateAssets="all" />
</ItemGroup>
```

This runs automatically during build.

### Option 3: GitHub Action

```yaml
- name: Run MarkdownSnippets
  uses: SimonCropp/MarkdownSnippets@master
```

## Configuration

Create `mdsnippets.json` in repository root:

```json
{
  "Convention": "InPlaceOverwrite",
  "TocLevel": 2,
  "MaxWidth": 120,
  "LinkFormat": "GitHub",
  "DocumentExtensions": [
    "md"
  ],
  "UrlsAsSnippets": [],
  "ExcludeDirectories": [
    "docs/todos",
    "docs/plans",
    "docs/release-notes"
  ],
  "ExcludeMarkdownDirectories": [
    "docs/todos",
    "docs/plans",
    "docs/release-notes"
  ]
}
```

### Configuration Options

| Option | Description | Recommended |
|--------|-------------|-------------|
| `Convention` | How to handle snippets | `InPlaceOverwrite` |
| `TocLevel` | Table of contents depth | `2` |
| `MaxWidth` | Max line width | `120` |
| `ExcludeDirectories` | Directories to skip for snippets | todos, plans, release-notes |
| `ExcludeMarkdownDirectories` | Markdown dirs to skip | Same as above |

## Sample Project Structure

Create `src/docs/samples/Samples.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
  </ItemGroup>

  <!-- Reference your framework project -->
  <ItemGroup>
    <ProjectReference Include="..\..\YourFramework\YourFramework.csproj" />
  </ItemGroup>

</Project>
```

## Writing Snippets

### In C# Sample Files

```csharp
public class GettingStartedSamples
{
    #region getting-started-basic
    [Fact]
    public void BasicUsage()
    {
        var service = new MyService();
        var result = service.DoSomething();
        Assert.NotNull(result);
    }
    #endregion

    #region getting-started-advanced
    [Fact]
    public void AdvancedUsage()
    {
        var service = new MyService(new Options
        {
            EnableFeature = true
        });
        var result = service.DoSomethingAdvanced();
        Assert.Equal("expected", result);
    }
    #endregion
}
```

### In Markdown Documentation

```markdown
## Basic Usage

<!-- snippet: getting-started-basic -->
<!-- endSnippet -->

## Advanced Usage

<!-- snippet: getting-started-advanced -->
<!-- endSnippet -->
```

### After Running MarkdownSnippets

The markdown file becomes:

```markdown
## Basic Usage

<!-- snippet: getting-started-basic -->
```cs
[Fact]
public void BasicUsage()
{
    var service = new MyService();
    var result = service.DoSomething();
    Assert.NotNull(result);
}
```
<!-- endSnippet -->

## Advanced Usage

<!-- snippet: getting-started-advanced -->
```cs
[Fact]
public void AdvancedUsage()
{
    var service = new MyService(new Options
    {
        EnableFeature = true
    });
    var result = service.DoSomethingAdvanced();
    Assert.Equal("expected", result);
}
```
<!-- endSnippet -->
```

## Workflow

1. **docs-architect** creates documentation with empty snippet placeholders
2. **docs-code-samples** creates sample code with `#region` markers
3. Run `mdsnippets` to sync code into documentation
4. Commit both documentation and sample code changes

## Troubleshooting

### Snippet Not Found

Error: `Snippet 'name' not found`

- Verify `#region name` exists in a C# file
- Check spelling matches exactly (case-sensitive)
- Ensure sample file is in a scanned directory

### Snippet Not Updated

- Run `mdsnippets` manually to force update
- Check `ExcludeDirectories` doesn't exclude your samples
- Verify the snippet markers are formatted correctly

### Build Integration Not Running

- Ensure `MarkdownSnippets.MsBuild` package is installed
- Check it has `PrivateAssets="all"` attribute
- Verify build output for MarkdownSnippets messages
