#nullable enable
using System;
using System.Text;

namespace KnockOff.Renderer;

/// <summary>
/// Helper for generating formatted C# code with automatic indentation management.
/// </summary>
internal sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent = 0;

    /// <summary>
    /// Sets the current indentation level.
    /// </summary>
    public void SetIndent(int level) => _indent = level;

    /// <summary>
    /// Gets the current indentation level.
    /// </summary>
    public int Indent => _indent;

    public void Line(string text = "")
    {
        if (string.IsNullOrEmpty(text))
        {
            _sb.AppendLine();
        }
        else
        {
            _sb.Append('\t', _indent);
            _sb.AppendLine(text);
        }
    }

    public void Append(string text) => _sb.Append(text);

    public IDisposable Block(string header)
    {
        Line(header);
        Line("{");
        _indent++;
        return new BlockScope(this);
    }

    public IDisposable Braces()
    {
        Line("{");
        _indent++;
        return new BlockScope(this);
    }

    public override string ToString() => _sb.ToString();

    private sealed class BlockScope : IDisposable
    {
        private readonly CodeWriter _writer;
        public BlockScope(CodeWriter writer) => _writer = writer;
        public void Dispose()
        {
            _writer._indent--;
            _writer.Line("}");
        }
    }
}
