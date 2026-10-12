// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Scans lexical boundaries once, keeping opaque text intact and blanking only real comments.
/// </summary>
/// <param name="text">The original source.</param>
/// <param name="jsx">Whether angle brackets can introduce JSX.</param>
/// <remarks>
/// Recursive code, template, tag and children modes form the mode stack. This is a boundary reader, not a
/// TypeScript parser: ambiguous expression syntax uses the previous-token heuristic, and incomplete boundaries
/// are reported rather than used to infer screen structure.
/// </remarks>
internal sealed class TsxSource(string text, bool jsx)
{
    static readonly HashSet<string> _expressionKeywords = new(StringComparer.Ordinal)
    {
        "return", "typeof", "instanceof", "in", "of", "new", "delete", "void", "throw", "case", "do", "else", "yield", "await"
    };

    readonly char[] _code = text.ToCharArray();
    readonly bool[] _isCode = new bool[text.Length];
    readonly Dictionary<int, int> _closes = [];
    readonly List<JsxTag> _tags = [];
    int _position;
    string? _scannedCode;

    /// <summary>
    /// Gets the comment-blanked source, with original offsets and line endings preserved.
    /// </summary>
    internal string Code => _scannedCode ??= new(_code);

    /// <summary>
    /// Gets opening tags in source order.
    /// </summary>
    internal IReadOnlyList<JsxTag> Tags => _tags;

    /// <summary>
    /// Gets the first boundary that could not be scanned.
    /// </summary>
    internal (int Line, string Construct)? Problem { get; private set; }

    bool HasInput => _position < text.Length && Problem is null;

    char Current => text[_position];

    /// <summary>
    /// Scans a TypeScript module.
    /// </summary>
    /// <param name="text">The original source.</param>
    /// <param name="jsx">Whether this is TSX rather than plain TypeScript.</param>
    /// <returns>The scanned boundaries.</returns>
    internal static TsxSource Scan(string text, bool jsx = true)
    {
        var source = new TsxSource(text, jsx);
        source.ReadCode(-1, -1);

        return source;
    }

    /// <summary>
    /// Scans JSX children, allowing sibling elements without a module wrapper.
    /// </summary>
    /// <param name="text">The markup.</param>
    /// <returns>The scanned boundaries.</returns>
    internal static TsxSource ScanMarkup(string text)
    {
        var source = new TsxSource(text, true);
        source.ReadChildren(-1);

        return source;
    }

    /// <summary>
    /// Determines whether a position is code rather than opaque text or a comment.
    /// </summary>
    /// <param name="position">The original offset.</param>
    /// <returns>Whether it is code.</returns>
    internal bool IsCode(int position) => _isCode[position];

    /// <summary>
    /// Finds the end of a matched brace, string or type argument list.
    /// </summary>
    /// <param name="open">The opening offset.</param>
    /// <returns>The exclusive end, or -1 for an unmatched boundary.</returns>
    internal int CloseOf(int open) => _closes.GetValueOrDefault(open, -1);

    static bool IsIdentifierStart(char character) => char.IsLetter(character) || character is '_' or '$';

    static bool IsIdentifierPart(char character) => char.IsLetterOrDigit(character) || character is '_' or '$';

    static bool IsTagPart(char character) => IsIdentifierPart(character) || character is '.' or '-' or ':';

    static bool IsLineBreak(char character) => character is '\r' or '\n';

    bool Starts(string value) => text.AsSpan(_position).StartsWith(value.AsSpan(), StringComparison.Ordinal);

    void Fail(int start, string construct) => Problem ??= (1 + text.AsSpan(0, start).Count('\n'), construct);

    void ReadCode(int open, int parent)
    {
        var expression = true;
        while (HasInput)
        {
            if (Current == '}' && open >= 0)
            {
                _isCode[_position] = true;
                _closes[open] = ++_position;
                return;
            }

            if (ReadTrivia())
            {
                continue;
            }

            expression = ReadToken(expression, parent);
        }

        if (open >= 0)
        {
            Fail(open, "brace expression");
        }
    }

    bool ReadTrivia()
    {
        if (char.IsWhiteSpace(Current))
        {
            _isCode[_position++] = true;
            return true;
        }

        return ReadComment();
    }

    bool ReadComment()
    {
        if (Starts("//"))
        {
            BlankLineComment();
            return true;
        }

        if (Starts("/*"))
        {
            BlankBlockComment();
            return true;
        }

        return false;
    }

    void Blank()
    {
        if (!IsLineBreak(Current))
        {
            _code[_position] = ' ';
        }

        _position++;
    }

    void BlankLineComment()
    {
        while (_position < text.Length && !IsLineBreak(Current))
        {
            Blank();
        }
    }

    void BlankBlockComment()
    {
        var start = _position;
        Blank();
        Blank();
        while (_position < text.Length)
        {
            if (Starts("*/"))
            {
                Blank();
                Blank();
                return;
            }

            Blank();
        }

        Fail(start, "block comment");
    }

    bool ReadToken(bool expression, int parent)
    {
        if (ReadOpaque(expression, parent))
        {
            return false;
        }

        _isCode[_position] = true;
        if (Current == '{')
        {
            var open = _position++;
            ReadCode(open, parent);
            return false;
        }

        if (IsIdentifierStart(Current))
        {
            return _expressionKeywords.Contains(ReadIdentifier());
        }

        var token = Current;
        _position++;

        return AllowsExpression(token);
    }

    bool ReadOpaque(bool expression, int parent)
    {
        if (Current is '\'' or '"')
        {
            ReadString(false);
            return true;
        }

        if (Current == '`')
        {
            ReadTemplate(parent);
            return true;
        }

        return ReadExpressionLiteral(expression, parent);
    }

    bool ReadExpressionLiteral(bool expression, int parent)
    {
        if (!expression)
        {
            return false;
        }

        if (Current == '/')
        {
            ReadRegex();
            return true;
        }

        if (StartsJsx())
        {
            ReadTag(parent);
            return true;
        }

        return false;
    }

    bool AllowsExpression(char token) => token is not (')' or ']' or '}') && !char.IsLetterOrDigit(token);

    string ReadIdentifier()
    {
        var start = _position;
        while (_position < text.Length && IsIdentifierPart(Current))
        {
            _isCode[_position++] = true;
        }

        return text[start.._position];
    }

    bool StartsJsx()
    {
        if (!jsx || !Starts("<"))
        {
            return false;
        }

        var next = _position + 1;
        if (next >= text.Length)
        {
            return false;
        }

        return IsJsxNameStart(text[next]) && !IsGenericArrow(next);
    }

    bool IsJsxNameStart(char character) => character == '>' || IsIdentifierStart(character);

    bool IsGenericArrow(int start)
    {
        var end = start;
        while (end < text.Length && IsIdentifierPart(text[end]))
        {
            end++;
        }

        while (end < text.Length && char.IsWhiteSpace(text[end]))
        {
            end++;
        }

        if (end >= text.Length)
        {
            return false;
        }

        return text[end] == ',' || text.AsSpan(end).StartsWith("extends ".AsSpan(), StringComparison.Ordinal);
    }

    void ReadString(bool attribute)
    {
        var start = _position;
        var quote = Current;
        _position++;
        while (_position < text.Length)
        {
            if (Current == quote)
            {
                _closes[start] = ++_position;
                return;
            }

            if (!attribute && ReadStringEscape(start))
            {
                continue;
            }

            _position++;
        }

        Fail(start, attribute ? "JSX attribute string" : "string literal");
    }

    bool ReadStringEscape(int start)
    {
        if (IsLineBreak(Current))
        {
            Fail(start, "string literal");
            _position = text.Length;
            return true;
        }

        if (Current != '\\')
        {
            return false;
        }

        _position = Math.Min(text.Length, _position + 2);

        return true;
    }

    void ReadTemplate(int parent)
    {
        var start = _position++;
        while (HasInput)
        {
            if (Current == '`')
            {
                _closes[start] = ++_position;
                return;
            }

            ReadTemplatePart(parent);
        }

        Fail(start, "template literal");
    }

    void ReadTemplatePart(int parent)
    {
        if (Current == '\\')
        {
            _position = Math.Min(text.Length, _position + 2);
            return;
        }

        if (Starts("${"))
        {
            _position++;
            ReadCode(_position++, parent);
            return;
        }

        _position++;
    }

    void ReadRegex()
    {
        var start = _position++;
        var inClass = false;
        while (_position < text.Length)
        {
            if (IsLineBreak(Current))
            {
                break;
            }

            if (Current == '/' && !inClass)
            {
                _position++;
                ReadRegexFlags();
                return;
            }

            inClass = ReadRegexPart(inClass);
        }

        Fail(start, "regular expression literal");
    }

    bool ReadRegexPart(bool inClass)
    {
        var character = Current;
        _position++;
        if (character == '\\')
        {
            _position = Math.Min(text.Length, _position + 1);
            return inClass;
        }

        return character switch
        {
            '[' => true,
            ']' => false,
            _ => inClass
        };
    }

    void ReadRegexFlags()
    {
        while (_position < text.Length && char.IsLetter(Current))
        {
            _position++;
        }
    }

    void ReadTag(int parent)
    {
        var start = _position++;
        var nameStart = _position;
        while (_position < text.Length && IsTagPart(Current))
        {
            _position++;
        }

        var tag = new JsxTag(text[nameStart.._position], start, parent);
        var index = _tags.Count;
        _tags.Add(tag);
        tag.AttributesStart = _position;
        ReadTypeArguments();
        ReadTagOpening(tag, index);
        if (tag.OpeningEnd == 0)
        {
            Fail(start, "JSX opening tag");
            return;
        }

        if (!tag.SelfClosing)
        {
            ReadChildren(index);
        }
    }

    void ReadTypeArguments()
    {
        if (_position >= text.Length || Current != '<')
        {
            return;
        }

        var start = _position++;
        var depth = 1;
        while (HasInput)
        {
            depth = ReadTypePart(depth);
            if (depth == 0)
            {
                _closes[start] = _position;
                return;
            }
        }

        Fail(start, "JSX type arguments");
    }

    int ReadTypePart(int depth)
    {
        if (ReadComment())
        {
            return depth;
        }

        if (ReadOpaque(false, -1))
        {
            return depth;
        }

        var character = Current;
        var arrow = Starts("=>");
        _position += arrow ? 2 : 1;

        return arrow ? depth : TypeDepth(character, depth);
    }

    int TypeDepth(char character, int depth) => character switch
    {
        '<' => depth + 1,
        '>' => depth - 1,
        _ => depth
    };

    void ReadTagOpening(JsxTag tag, int parent)
    {
        while (HasInput)
        {
            if (Starts("/>") || Current == '>')
            {
                tag.SelfClosing = Starts("/>");
                _position += tag.SelfClosing ? 2 : 1;
                tag.OpeningEnd = _position;
                if (tag.SelfClosing)
                {
                    FinishTag(tag, _position);
                }

                return;
            }

            ReadTagPart(parent);
        }
    }

    void ReadTagPart(int parent)
    {
        if (ReadComment())
        {
            return;
        }

        if (Current is '"' or '\'')
        {
            ReadString(true);
            return;
        }

        if (Current == '{')
        {
            ReadCode(_position++, parent);
            return;
        }

        _position++;
    }

    void FinishTag(JsxTag tag, int childrenEnd)
    {
        tag.ChildrenEnd = childrenEnd;
        tag.End = _position;
        tag.IsComplete = true;
    }

    void ReadChildren(int parent)
    {
        while (HasInput)
        {
            if (Starts("</"))
            {
                ReadClosing(parent);
                return;
            }

            ReadChild(parent);
        }

        if (parent >= 0)
        {
            Fail(_tags[parent].Start, "JSX children");
        }
    }

    void ReadChild(int parent)
    {
        if (Current == '<')
        {
            ReadTag(parent);
            return;
        }

        if (Current == '{')
        {
            ReadChildExpression(parent);
            return;
        }

        if (!char.IsWhiteSpace(Current))
        {
            MarkContent(parent);
        }

        _position++;
    }

    void ReadChildExpression(int parent)
    {
        var open = _position++;
        ReadCode(open, parent);
        if (CloseOf(open) is > 0 and var end && HasExpressionContent(open, end))
        {
            MarkContent(parent);
        }
    }

    bool HasExpressionContent(int open, int end) => !string.IsNullOrWhiteSpace(new string(_code, open + 1, end - open - 2));

    void MarkContent(int parent)
    {
        if (parent >= 0)
        {
            _tags[parent].HasContent = true;
        }
    }

    void ReadClosing(int parent)
    {
        var start = _position;
        _position += 2;
        var nameStart = _position;
        while (_position < text.Length && IsTagPart(Current))
        {
            _position++;
        }

        var name = text[nameStart.._position];
        SkipWhitespace();
        if (!MatchesClosing(parent, name))
        {
            Fail(start, "JSX closing tag");
            return;
        }

        _position++;
        FinishTag(_tags[parent], start);
    }

    bool MatchesClosing(int parent, string name)
    {
        if (parent < 0 || _position >= text.Length)
        {
            return false;
        }

        return Current == '>' && _tags[parent].Name == name;
    }

    void SkipWhitespace()
    {
        while (_position < text.Length && char.IsWhiteSpace(Current))
        {
            _position++;
        }
    }
}
