using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;

/// <summary>
/// A tool that for a given C# program constructs Roslyn API calls to create a syntax tree that
/// describes this program. As opposed to SyntaxTree.ParseText() that creates the syntax tree object
/// graph in runtime, Quoter returns the C# source code that will construct such syntax tree object
/// graph when compiled and executed.
/// </summary>
/// <example>
/// new Quoter().Quote("class C{}") returns:
/// 
/// CompilationUnit()
/// .WithMembers(
///     List&lt;MemberDeclarationSyntax&gt;
///         ClassDeclaration(
///             Identifier(
///                 "C"))
///         .WithKeyword(
///             Token(
///                 ClassKeyword,
///                 TriviaList(
///                     Space)))
/// .WithEndOfFileToken(
///     Syntax.Token(
///         SyntaxKind.EndOfFileToken))
/// </example>
public class Quoter
{
    public bool OpenParenthesisOnNewLine { get; set; }
    public bool ClosingParenthesisOnNewLine { get; set; }
    public bool UseDefaultFormatting { get; set; }
    public bool RemoveRedundantModifyingCalls { get; set; }
    public bool ShortenCodeWithUsingStatic { get; set; }

    private readonly ScriptOptions options = ScriptOptions.Default
        .AddReferences(typeof(SyntaxNode).Assembly, typeof(CSharpSyntaxNode).Assembly)
        .AddImports(
            "System",
            "Microsoft.CodeAnalysis",
            "Microsoft.CodeAnalysis.CSharp",
            "Microsoft.CodeAnalysis.CSharp.Syntax",
            "Microsoft.CodeAnalysis.CSharp.SyntaxFactory");

    public Quoter()
    {
        UseDefaultFormatting = true;
        RemoveRedundantModifyingCalls = true;
    }

    /// <summary>
    /// Given the input C# program <paramref name="sourceText"/> returns the C# source code of
    /// Roslyn API calls that recreate the syntax tree for the input program.
    /// </summary>
    /// <param name="sourceText">A C# program (one compilation unit)</param>
    /// <returns>A C# expression that describes calls to the Roslyn syntax API necessary to recreate
    /// the syntax tree for the source program.</returns>
    public string Quote(string sourceText)
    {
        return Quote(sourceText, NodeKind.CompilationUnit);
    }

    /// <summary>
    /// Given the input C# code <paramref name="sourceText"/> returns the C# source code of
    /// Roslyn API calls that recreate the syntax tree for the input code.
    /// </summary>
    /// <param name="sourceText">A C# souce text</param>
    /// <param name="nodeKind">What kind of C# syntax node should the input be parsed as</param>
    /// <returns>A C# expression that describes calls to the Roslyn syntax API necessary to recreate
    /// the syntax tree for the source text.</returns>
    public string Quote(string sourceText, NodeKind nodeKind)
    {
        return Quote(Parse(sourceText, nodeKind));
    }

    /// <summary>
    /// Given the input C# code <paramref name="sourceText"/> returns
    /// the syntax tree for the input code.
    /// </summary>
    /// <param name="sourceText">A C# souce text</param>
    /// <param name="nodeKind">What kind of C# syntax node should the input be parsed as</param>
    private static SyntaxNode Parse(string sourceText, NodeKind nodeKind)
    {
        switch (nodeKind)
        {
            case NodeKind.CompilationUnit:
                return SyntaxFactory.ParseCompilationUnit(sourceText);
            case NodeKind.Statement:
                return SyntaxFactory.ParseStatement(sourceText);
            case NodeKind.Expression:
                return SyntaxFactory.ParseExpression(sourceText);
            default:
                throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Given the input C# syntax node <paramref name="node"/> returns the C# source code of
    /// Roslyn API calls that recreate the syntax node.
    /// </summary>
    /// <param name="node">A C# syntax node</param>
    /// <returns>A C# expression that describes calls to the Roslyn syntax API necessary to recreate
    /// the input syntax node.</returns>
    internal string Quote(SyntaxNode node)
    {
        ApiCall rootApiCall = Quote(node, name: null);
        if (UseDefaultFormatting)
        {
            rootApiCall.Add(new MethodCall { Name = ".NormalizeWhitespace" });
        }

        string generatedCode = Print(rootApiCall);
        return generatedCode;
    }

    /// <summary>
    /// Recursive method that "quotes" a SyntaxNode, SyntaxToken, SyntaxTrivia or other objects.
    /// </summary>
    /// <returns>A description of Roslyn API calls necessary to recreate the input object.</returns>
    private ApiCall Quote(object treeElement, string name = null)
    {
        if (treeElement is SyntaxTrivia)
        {
            return QuoteTrivia((SyntaxTrivia)treeElement);
        }

        if (treeElement is SyntaxToken)
        {
            return QuoteToken((SyntaxToken)treeElement, name);
        }

        if (treeElement is SyntaxNodeOrToken)
        {
            SyntaxNodeOrToken syntaxNodeOrToken = (SyntaxNodeOrToken)treeElement;
            if (syntaxNodeOrToken.IsNode)
            {
                return QuoteNode(syntaxNodeOrToken.AsNode(), name);
            }
            else
            {
                return QuoteToken(syntaxNodeOrToken.AsToken(), name);
            }
        }

        return QuoteNode((SyntaxNode)treeElement, name);
    }

    /// <summary>
    /// The main recursive method that given a SyntaxNode recursively quotes the entire subtree.
    /// </summary>
    private ApiCall QuoteNode(SyntaxNode node, string name)
    {
        List<ApiCall> quotedPropertyValues = QuotePropertyValues(node);
        MethodInfo factoryMethod = PickFactoryMethodToCreateNode(node);
        string factoryMethodName = factoryMethod.Name;

        if (!ShortenCodeWithUsingStatic)
        {
            factoryMethodName = factoryMethod.DeclaringType.Name + "." + factoryMethodName;
        }

        var factoryMethodCall = new MethodCall()
        {
            Name = factoryMethodName
        };

        var codeBlock = new ApiCall(name, factoryMethodCall);

        AddFactoryMethodArguments(factoryMethod, factoryMethodCall, quotedPropertyValues);
        AddModifyingCalls(node, codeBlock, quotedPropertyValues);

        return codeBlock;
    }

    /// <summary>
    /// Inspects the property values of the <paramref name="node"/> object using Reflection and
    /// creates API call descriptions for the property values recursively. Properties that are not
    /// essential to the shape of the syntax tree (such as Span) are ignored.
    /// </summary>
    private List<ApiCall> QuotePropertyValues(SyntaxNode node)
    {
        var result = new List<ApiCall>();

        var properties = node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Filter out non-essential properties listed in nonStructuralProperties
        result.AddRange(properties
            .Where(propertyInfo => !nonStructuralProperties.Contains(propertyInfo.Name))
            .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null)
            .Select(propertyInfo => QuotePropertyValue(node, propertyInfo))
            .Where(apiCall => apiCall != null));

        // HACK: factory methods for the following node types accept back the first "kind" parameter
        // that we filter out above. Add an artificial "property value" that can be later used to
        // satisfy the first parameter of type SyntaxKind.
        if (node is AccessorDeclarationSyntax ||
            node is AssignmentExpressionSyntax ||
            node is BinaryExpressionSyntax ||
            node is ClassOrStructConstraintSyntax ||
            node is CheckedExpressionSyntax ||
            node is CheckedStatementSyntax ||
            node is ConstructorInitializerSyntax ||
            node is GotoStatementSyntax ||
            node is InitializerExpressionSyntax ||
            node is LiteralExpressionSyntax ||
            node is MemberAccessExpressionSyntax ||
            node is OrderingSyntax ||
            node is PostfixUnaryExpressionSyntax ||
            node is PrefixUnaryExpressionSyntax ||
            node is DocumentationCommentTriviaSyntax ||
            node is YieldStatementSyntax)
        {
            result.Add(new ApiCall("Kind", "SyntaxKind." + node.Kind().ToString()));
        }

        return result;
    }

    /// <summary>
    /// Quote the value of the property <paramref name="property"/> of object <paramref
    /// name="node"/>
    /// </summary>
    private ApiCall QuotePropertyValue(SyntaxNode node, PropertyInfo property)
    {
        var value = property.GetValue(node, null);
        var propertyType = property.PropertyType;

        if (propertyType == typeof(SyntaxToken))
        {
            return QuoteToken((SyntaxToken)value, property.Name);
        }

        if (propertyType == typeof(SyntaxTokenList))
        {
            return QuoteList((IEnumerable)value, property.Name);
        }

        if (propertyType.IsGenericType &&
            (propertyType.GetGenericTypeDefinition() == typeof(SyntaxList<>) ||
             propertyType.GetGenericTypeDefinition() == typeof(SeparatedSyntaxList<>)))
        {
            return QuoteList((IEnumerable)value, property.Name);
        }

        if (value is SyntaxNode)
        {
            return QuoteNode((SyntaxNode)value, property.Name);
        }

        if (value is string)
        {
            var text = value.ToString();
            var verbatim = text.Contains("\r") || text.Contains("\n");
            return new ApiCall(property.Name, EscapeAndQuote(text, verbatim));
        }

        if (value is bool)
        {
            return new ApiCall(property.Name, value.ToString().ToLowerInvariant());
        }

        return null;
    }

    private string SyntaxFactoryMethod(string text)
    {
        if (!ShortenCodeWithUsingStatic)
        {
            text = "SyntaxFactory." + text;
        }

        return text;
    }

    private ApiCall QuoteList(IEnumerable syntaxList, string name)
    {
        IEnumerable<object> sourceList = syntaxList.Cast<object>();

        string methodName = SyntaxFactoryMethod("List");
        string listType = null;
        var propertyType = syntaxList.GetType();
        if (propertyType.IsGenericType)
        {
            var methodType = propertyType.GetGenericArguments()[0].Name;
            listType = methodType;

            if (propertyType.GetGenericTypeDefinition() == typeof(SeparatedSyntaxList<>))
            {
                listType = "SyntaxNodeOrToken";
                methodName = SyntaxFactoryMethod("SeparatedList");
                sourceList = ((SyntaxNodeOrTokenList)
                    syntaxList.GetType().GetMethod("GetWithSeparators").Invoke(syntaxList, null))
                    .Cast<object>()
                    .ToArray();
            }

            methodName += "<" + methodType + ">";
        }

        if (propertyType.Name == "SyntaxTokenList")
        {
            methodName = SyntaxFactoryMethod("TokenList");
        }

        if (propertyType.Name == "SyntaxTriviaList")
        {
            methodName = SyntaxFactoryMethod("TriviaList");
        }

        var elements = new List<object>(sourceList
            .Select(o => Quote(o))
            .Where(cb => cb != null));
        if (elements.Count == 0)
        {
            return null;
        }
        else if (elements.Count == 1)
        {
            if (methodName.StartsWith("List"))
            {
                methodName = "SingletonList" + methodName.Substring("List".Length);
            }

            if (methodName.StartsWith(SyntaxFactoryMethod("List")))
            {
                methodName = SyntaxFactoryMethod("SingletonList") + methodName.Substring(SyntaxFactoryMethod("List").Length);
            }

            if (methodName.StartsWith("SeparatedList"))
            {
                methodName = "SingletonSeparatedList" + methodName.Substring("SeparatedList".Length);
            }

            if (methodName.StartsWith(SyntaxFactoryMethod("SeparatedList")))
            {
                methodName = SyntaxFactoryMethod("SingletonSeparatedList") + methodName.Substring(SyntaxFactoryMethod("SeparatedList").Length);
            }
        }
        else
        {
            elements = new List<object>
            {
                new ApiCall(
                    "methodName",
                    "new " + listType + "[]",
                    elements,
                    useCurliesInsteadOfParentheses: true)
            };
        }

        var codeBlock = new ApiCall(name, methodName, elements);
        return codeBlock;
    }

    private ApiCall QuoteToken(SyntaxToken value, string name)
    {
        if (value == default(SyntaxToken) || value.Kind() == SyntaxKind.None)
        {
            return null;
        }

        var arguments = new List<object>();
        string methodName = SyntaxFactoryMethod("Token");
        bool verbatim =
            value.Text.StartsWith("@") ||
            value.Text.Contains("\r") ||
            value.Text.Contains("\n");
        string escapedTokenValueText = EscapeAndQuote(value.ToString(), verbatim);
        object leading = GetLeadingTrivia(value);
        object actualValue;
        object trailing = GetTrailingTrivia(value);

        if (leading != null || trailing != null)
        {
            leading = leading ?? GetEmptyTrivia("LeadingTrivia");
            trailing = trailing ?? GetEmptyTrivia("TrailingTrivia");
        }

        if (value.Kind() == SyntaxKind.IdentifierToken && !value.IsMissing)
        {
            methodName = SyntaxFactoryMethod("Identifier");
            if (value.IsMissing)
            {
                methodName = SyntaxFactoryMethod("MissingToken");
            }

            if (value.IsMissing)
            {
                actualValue = value.Kind();
            }
            else
            {
                actualValue = escapedTokenValueText;
            }

            AddIfNotNull(arguments, leading);
            arguments.Add(actualValue);
            AddIfNotNull(arguments, trailing);
        }
        else if (value.Kind() == SyntaxKind.InterpolatedStringTextToken && !value.IsMissing)
        {
            leading = leading ?? GetEmptyTrivia("LeadingTrivia");
            trailing = trailing ?? GetEmptyTrivia("TrailingTrivia");
            AddIfNotNull(arguments, leading);
            arguments.Add(value.Kind());
            arguments.Add(escapedTokenValueText);
            arguments.Add(escapedTokenValueText);
            AddIfNotNull(arguments, trailing);
        }
        else if ((value.Kind() == SyntaxKind.XmlTextLiteralToken ||
            value.Kind() == SyntaxKind.XmlTextLiteralNewLineToken ||
            value.Kind() == SyntaxKind.XmlEntityLiteralToken) && !value.IsMissing)
        {
            methodName = SyntaxFactoryMethod("XmlTextLiteral");
            if (value.Kind() == SyntaxKind.XmlTextLiteralNewLineToken)
            {
                methodName = SyntaxFactoryMethod("XmlTextNewLine");
            }
            else if (value.Kind() == SyntaxKind.XmlEntityLiteralToken)
            {
                methodName = SyntaxFactoryMethod("XmlEntity");
            }

            arguments.Add(leading ?? GetEmptyTrivia("LeadingTrivia"));
            arguments.Add(escapedTokenValueText);
            arguments.Add(escapedTokenValueText);
            arguments.Add(trailing ?? GetEmptyTrivia("TrailingTrivia"));
        }
        else if ((value.Parent is LiteralExpressionSyntax ||
            value.Kind() == SyntaxKind.StringLiteralToken ||
            value.Kind() == SyntaxKind.NumericLiteralToken) &&
            value.Kind() != SyntaxKind.TrueKeyword &&
            value.Kind() != SyntaxKind.FalseKeyword &&
            value.Kind() != SyntaxKind.NullKeyword &&
            value.Kind() != SyntaxKind.ArgListKeyword &&
            !value.IsMissing)
        {
            methodName = SyntaxFactoryMethod("Literal");
            bool shouldAddTrivia = leading != null || trailing != null;
            if (shouldAddTrivia)
            {
                arguments.Add(leading ?? GetEmptyTrivia("LeadingTrivia"));
            }

            string escapedText = EscapeAndQuote(value.Text);
            string escapedValue = EscapeAndQuote(value.ValueText);

            if (value.Kind() == SyntaxKind.CharacterLiteralToken)
            {
                escapedValue = EscapeAndQuote(value.ValueText, "'");
            }
            else if (value.Kind() != SyntaxKind.StringLiteralToken)
            {
                escapedValue = value.ValueText;
            }

            if (shouldAddTrivia ||
                (value.Kind() == SyntaxKind.StringLiteralToken &&
                value.ToString() != Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Literal(value.ValueText).ToString()))
            {
                arguments.Add(escapedText);
            }

            arguments.Add(escapedValue);

            if (shouldAddTrivia)
            {
                arguments.Add(trailing ?? GetEmptyTrivia("TrailingTrivia"));
            }
        }
        else
        {
            if (value.IsMissing)
            {
                methodName = SyntaxFactoryMethod("MissingToken");
            }

            if (value.Kind() == SyntaxKind.BadToken)
            {
                methodName = SyntaxFactoryMethod("BadToken");
                leading = leading ?? GetEmptyTrivia("LeadingTrivia");
                trailing = trailing ?? GetEmptyTrivia("TrailingTrivia");
            }

            object tokenValue = value.Kind();

            if (value.Kind() == SyntaxKind.BadToken)
            {
                tokenValue = escapedTokenValueText;
            }

            AddIfNotNull(arguments, leading);
            arguments.Add(tokenValue);
            AddIfNotNull(arguments, trailing);
        }

        return new ApiCall(name, methodName, arguments);
    }

    private static void AddIfNotNull(List<object> arguments, object value)
    {
        if (value != null)
        {
            arguments.Add(value);
        }
    }

    private object GetLeadingTrivia(SyntaxToken value)
    {
        if (value.HasLeadingTrivia)
        {
            var quotedLeadingTrivia = QuoteList(value.LeadingTrivia, "LeadingTrivia");
            if (quotedLeadingTrivia != null)
            {
                return quotedLeadingTrivia;
            }
        }

        return null;
    }

    private object GetTrailingTrivia(SyntaxToken value)
    {
        if (value.HasTrailingTrivia)
        {
            var quotedTrailingTrivia = QuoteList(value.TrailingTrivia, "TrailingTrivia");
            if (quotedTrailingTrivia != null)
            {
                return quotedTrailingTrivia;
            }
        }

        return null;
    }

    private object GetEmptyTrivia(string parentPropertyName)
    {
        return new ApiCall(parentPropertyName, SyntaxFactoryMethod("TriviaList"), arguments: null);
    }

    private ApiCall QuoteTrivia(SyntaxTrivia syntaxTrivia)
    {
        string factoryMethodName = SyntaxFactoryMethod("Trivia");
        string text = syntaxTrivia.ToString();
        if (syntaxTrivia.FullSpan.Length == 0 ||
            (syntaxTrivia.Kind() == SyntaxKind.WhitespaceTrivia && UseDefaultFormatting))
        {
            return null;
        }

        PropertyInfo triviaFactoryProperty = null;
        if (triviaFactoryProperties.TryGetValue(syntaxTrivia.ToString(), out triviaFactoryProperty) &&
            ((SyntaxTrivia)triviaFactoryProperty.GetValue(null)).Kind() == syntaxTrivia.Kind())
        {
            if (UseDefaultFormatting)
            {
                return null;
            }

            return new ApiCall(null, SyntaxFactoryMethod(triviaFactoryProperty.Name));
        }

        if (!string.IsNullOrEmpty(text) &&
            string.IsNullOrWhiteSpace(text) &&
            syntaxTrivia.Kind() == SyntaxKind.WhitespaceTrivia)
        {
            if (UseDefaultFormatting)
            {
                return null;
            }

            factoryMethodName = SyntaxFactoryMethod("Whitespace");
        }

        if (syntaxTrivia.Kind() == SyntaxKind.SingleLineCommentTrivia ||
            syntaxTrivia.Kind() == SyntaxKind.MultiLineCommentTrivia)
        {
            factoryMethodName = SyntaxFactoryMethod("Comment");
        }

        if (syntaxTrivia.Kind() == SyntaxKind.PreprocessingMessageTrivia)
        {
            factoryMethodName = SyntaxFactoryMethod("PreprocessingMessage");
        }

        if (syntaxTrivia.Kind() == SyntaxKind.DisabledTextTrivia)
        {
            factoryMethodName = SyntaxFactoryMethod("DisabledText");
        }

        if (syntaxTrivia.Kind() == SyntaxKind.DocumentationCommentExteriorTrivia)
        {
            factoryMethodName = SyntaxFactoryMethod("DocumentationCommentExterior");
        }

        var t = syntaxTrivia.ToString();
        var verbatim = t.Contains("\r") || t.Contains("\n");
        object argument = EscapeAndQuote(t, verbatim: verbatim);

        if (syntaxTrivia.HasStructure)
        {
            argument = QuoteNode(syntaxTrivia.GetStructure(), "Structure");
        }

        return new ApiCall(null, factoryMethodName, CreateArgumentList(argument));
    }

    private void AddFactoryMethodArguments(
        MethodInfo factory,
        MethodCall factoryMethodCall,
        List<ApiCall> quotedValues)
    {
        foreach (var factoryMethodParameter in factory.GetParameters())
        {
            var parameterName = factoryMethodParameter.Name;
            var parameterType = factoryMethodParameter.ParameterType;

            ApiCall quotedCodeBlock = FindValue(parameterName, quotedValues);

            // if we have Block(List<StatementSyntax>(new StatementSyntax[] { A, B })), just simplify it to
            // Block(A, B)
            if (quotedCodeBlock != null && factory.GetParameters().Length == 1 && factoryMethodParameter.GetCustomAttribute<ParamArrayAttribute>() != null)
            {
                var methodCall = quotedCodeBlock.FactoryMethodCall as MethodCall;
                if (methodCall != null && methodCall.Name.Contains("List") && methodCall.Arguments.Count == 1)
                {
                    var argument = methodCall.Arguments[0] as ApiCall;
                    var arrayCreation = argument.FactoryMethodCall as MethodCall;
                    if (argument != null && arrayCreation != null && arrayCreation.Name.StartsWith("new ") && arrayCreation.Name.EndsWith("[]"))
                    {
                        foreach (var arrayElement in arrayCreation.Arguments)
                        {
                            factoryMethodCall.AddArgument(arrayElement);
                        }

                        quotedValues.Remove(quotedCodeBlock);
                        return;
                    }
                }
            }

            // special case to prefer SyntaxFactory.IdentifierName("C") to 
            // SyntaxFactory.IdentifierName(Syntax.Identifier("C"))
            if (parameterName == "name" && parameterType == typeof(string))
            {
                quotedCodeBlock = quotedValues.First(a => a.Name == "Identifier");
                var methodCall = quotedCodeBlock.FactoryMethodCall as MethodCall;
                if (methodCall != null && methodCall.Name == SyntaxFactoryMethod("Identifier"))
                {
                    if (methodCall.Arguments.Count == 1)
                    {
                        factoryMethodCall.AddArgument(methodCall.Arguments[0]);
                    }
                    else
                    {
                        factoryMethodCall.AddArgument(quotedCodeBlock);
                    }

                    quotedValues.Remove(quotedCodeBlock);
                    continue;
                }
            }

            // special case to prefer SyntaxFactory.ClassDeclarationSyntax(string) instead of 
            // SyntaxFactory.ClassDeclarationSyntax(SyntaxToken)
            if (parameterName == "identifier" && parameterType == typeof(string))
            {
                var methodCall = quotedCodeBlock.FactoryMethodCall as MethodCall;
                if (methodCall != null &&
                    methodCall.Name == SyntaxFactoryMethod("Identifier") &&
                    methodCall.Arguments.Count == 1)
                {
                    factoryMethodCall.AddArgument(methodCall.Arguments[0]);
                    quotedValues.Remove(quotedCodeBlock);
                    continue;
                }
            }

            if (quotedCodeBlock != null)
            {
                factoryMethodCall.AddArgument(quotedCodeBlock);
                quotedValues.Remove(quotedCodeBlock);
            }
            else if (!factoryMethodParameter.IsOptional)
            {
                if (parameterType.IsArray)
                {
                    // assuming this is a params parameter that accepts an array, so if we have nothing we don't need to pass anything
                    continue;
                }

                // if we don't have a value just try passing null and see if it will work later
                if (!parameterType.IsValueType)
                {
                    factoryMethodCall.AddArgument("null");
                }
                else
                {
                    factoryMethodCall.AddArgument(
                        new MethodCall()
                        {
                            Name = "default",
                            Arguments = new List<object>
                            {
                                GetPrintableTypeName(parameterType)
                            }
                        });
                }
            }
        }
    }

    /// <summary>
    /// Helper to quickly create a list from one or several items
    /// </summary>
    private static List<object> CreateArgumentList(params object[] args)
    {
        return new List<object>(args);
    }

    /// <summary>
    /// Escapes strings to be included within "" using C# escaping rules
    /// </summary>
    public static string Escape(string text, bool escapeVerbatim = false)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            string toAppend = text[i].ToString();
            if (text[i] == '"')
            {
                if (escapeVerbatim)
                {
                    toAppend = "\"\"";
                }
                else
                {
                    toAppend = "\\\"";
                }
            }
            else if (text[i] == '\\' && !escapeVerbatim)
            {
                toAppend = "\\\\";
            }

            sb.Append(toAppend);
        }

        return sb.ToString();
    }

    public static string EscapeAndQuote(string text, string quoteChar = "\"")
    {
        bool verbatim = text.Contains("\n") || text.Contains("\r");
        return EscapeAndQuote(text, verbatim, quoteChar);
    }

    public static string EscapeAndQuote(string text, bool verbatim, string quoteChar = "\"")
    {
        if (text == Environment.NewLine)
        {
            return "Environment.NewLine";
        }

        if (text == "\n")
        {
            return "\"\\n\"";
        }

        text = Escape(text, verbatim);
        text = SurroundWithQuotes(text, quoteChar);
        if (verbatim)
        {
            text = "@" + text;
        }

        return text;
    }

    private static string SurroundWithQuotes(string text, string quoteChar = "\"")
    {
        text = quoteChar + text + quoteChar;
        return text;
    }

    /// <summary>
    /// Finds a value in a list using case-insensitive search
    /// </summary>
    private ApiCall FindValue(string parameterName, IEnumerable<ApiCall> values)
    {
        return values.FirstOrDefault(
            v => parameterName.Equals(v.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly HashSet<string> factoryMethodsToExclude = new HashSet<string>
    {
        "DocumentationComment",
        "XmlNewLine",
    };

    /// <summary>
    /// Static methods on Microsoft.CodeAnalysis.CSharp.SyntaxFactory class that construct SyntaxNodes
    /// </summary>
    /// <example>Syntax.ClassDeclaration()</example>
    private static readonly Dictionary<string, IEnumerable<MethodInfo>> factoryMethods = GetFactoryMethods();

    /// <summary>
    /// Five public properties on Microsoft.CodeAnalysis.CSharp.SyntaxFactory that return trivia: CarriageReturn,
    /// LineFeed, CarriageReturnLineFeed, Space and Tab.
    /// </summary>
    private static readonly Dictionary<string, PropertyInfo> triviaFactoryProperties = GetTriviaFactoryProperties();

    /// <summary>
    /// Gets the five properties on SyntaxFactory that return ready-made trivia: CarriageReturn,
    /// CarriageReturnLineFeed, LineFeed, Space and Tab.
    /// </summary>
    private static Dictionary<string, PropertyInfo> GetTriviaFactoryProperties()
    {
        var result = typeof(SyntaxFactory)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(propertyInfo => propertyInfo.PropertyType == typeof(SyntaxTrivia))
            .Where(propertyInfo => !propertyInfo.Name.Contains("Elastic"))
            .ToDictionary(propertyInfo => ((SyntaxTrivia)propertyInfo.GetValue(null)).ToString());

        return result;
    }

    /// <summary>
    /// Returns static methods on Microsoft.CodeAnalysis.CSharp.SyntaxFactory that return types derived from
    /// SyntaxNode and bucketizes them by overloads.
    /// </summary>
    private static Dictionary<string, IEnumerable<MethodInfo>> GetFactoryMethods()
    {
        var result = new Dictionary<string, IEnumerable<MethodInfo>>();

        var staticMethods = typeof(SyntaxFactory)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<ObsoleteAttribute>() == null);

        foreach (var method in staticMethods.OrderBy(m => m.ToString()))
        {
            var returnTypeName = method.ReturnType.Name;

            if (factoryMethodsToExclude.Contains(method.ToString()) ||
                factoryMethodsToExclude.Contains(method.Name))
            {
                continue;
            }

            IEnumerable<MethodInfo> bucket = null;
            if (!result.TryGetValue(returnTypeName, out bucket))
            {
                bucket = new List<MethodInfo>();
                result.Add(returnTypeName, bucket);
            }

            ((List<MethodInfo>)bucket).Add(method);
        }

        return result;
    }

    /// <summary>
    /// Uses Reflection to inspect static factory methods on the Microsoft.CodeAnalysis.CSharp.SyntaxFactory
    /// class and pick an overload that creates a node of the same type as the input <paramref
    /// name="node"/>
    /// </summary>
    private MethodInfo PickFactoryMethodToCreateNode(SyntaxNode node)
    {
        string name = node.GetType().Name;

        IEnumerable<MethodInfo> candidates = null;
        if (!factoryMethods.TryGetValue(name, out candidates))
        {
            throw new NotSupportedException(name + " is not supported");
        }

        var candidateNames = candidates.Select(m => m.ToString()).ToArray();

        // if there's exactly one method, there's nothing to deliberate
        if (candidates.Count() == 1)
        {
            return candidates.First();
        }

        var usingDirectiveSyntax = node as UsingDirectiveSyntax;
        if (usingDirectiveSyntax != null)
        {
            if (usingDirectiveSyntax.Alias == null)
            {
                candidates = candidates.Where(m => m.ToString() != "Microsoft.CodeAnalysis.CSharp.Syntax.UsingDirectiveSyntax UsingDirective(Microsoft.CodeAnalysis.CSharp.Syntax.NameEqualsSyntax, Microsoft.CodeAnalysis.CSharp.Syntax.NameSyntax)");
            }
        }

        int minParameterCount = candidates.Min(m => m.GetParameters().Length);

        // HACK: for LiteralExpression pick the overload with two parameters - the overload with one
        // parameter only allows true/false/null literals
        if (node is LiteralExpressionSyntax)
        {
            SyntaxKind kind = ((LiteralExpressionSyntax)node).Kind();
            if (kind != SyntaxKind.TrueLiteralExpression &&
                kind != SyntaxKind.FalseLiteralExpression &&
                kind != SyntaxKind.NullLiteralExpression)
            {
                minParameterCount = 2;
            }
        }

        MethodInfo factory = null;

        if ((node is BaseTypeDeclarationSyntax ||
             node is IdentifierNameSyntax))
        {
            Type desiredParameterType = typeof(string);
            factory = candidates.FirstOrDefault(m => m.GetParameters()[0].ParameterType == desiredParameterType);
            if (factory != null)
            {
                return factory;
            }
        }

        var candidatesWithMinParameterCount = candidates.Where(m => m.GetParameters().Length == minParameterCount).ToArray();

        if (minParameterCount == 1 && candidatesWithMinParameterCount.Length > 1)
        {
            // first see if we have a method that accepts params parameter and return that if found
            var paramArray = candidatesWithMinParameterCount.FirstOrDefault(m => m.GetParameters()[0].GetCustomAttribute<ParamArrayAttribute>() != null);
            if (paramArray != null)
            {
                return paramArray;
            }

            // if there are multiple candidates with one parameter, pick the one that is optional
            var firstParameterOptional = candidatesWithMinParameterCount.FirstOrDefault(m => m.GetParameters()[0].IsOptional);
            if (firstParameterOptional != null)
            {
                return firstParameterOptional;
            }
        }

        // otherwise just pick the first one (this is arbitrary)
        factory = candidatesWithMinParameterCount[0];

        return factory;
    }

    /// <summary>
    /// Adds information about subsequent modifying fluent interface style calls on an object (like
    /// foo.With(...).With(...))
    /// </summary>
    private void AddModifyingCalls(object treeElement, ApiCall apiCall, List<ApiCall> values)
    {
        var methods = treeElement.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<ObsoleteAttribute>() == null);

        foreach (var value in values)
        {
            var properCase = ProperCase(value.Name);
            var methodName = "With" + properCase;
            if (methods.Any(m => m.Name == methodName))
            {
                methodName = "." + methodName;
            }
            else
            {
                throw new NotSupportedException("Sorry, this is a bug in Quoter. Please file a bug at https://github.com/KirillOsenkov/RoslynQuoter/issues/new.");
            }

            var methodCall = new MethodCall
            {
                Name = methodName,
                Arguments = CreateArgumentList(value)
            };

            AddModifyingCall(apiCall, methodCall);
        }
    }

    private void AddModifyingCall(ApiCall apiCall, MethodCall methodCall)
    {
        if (RemoveRedundantModifyingCalls)
        {
            var before = Evaluate(apiCall, UseDefaultFormatting);
            apiCall.Add(methodCall);
            var after = Evaluate(apiCall, UseDefaultFormatting);
            if (before == after)
            {
                apiCall.Remove(methodCall);
            }

            return;
        }

        apiCall.Add(methodCall);
    }

    /// <summary>
    /// Calls the Roslyn syntax API to actually create the syntax tree object and return the source
    /// code generated by the syntax tree.
    /// </summary>
    /// <param name="apiCallString">Code that calls Roslyn syntax APIs as a string</param>
    /// <returns>The string that corresponds to the code of the syntax tree.</returns>
    public string Evaluate(string apiCallString, bool normalizeWhitespace = false)
    {
        var generatedNode = CSharpScript.EvaluateAsync<SyntaxNode>(apiCallString, options).Result;
        if (normalizeWhitespace)
        {
            generatedNode = generatedNode.NormalizeWhitespace();
        }

        var resultText = generatedNode.ToFullString();
        return resultText;
    }

    private string Evaluate(ApiCall apiCall, bool normalizeWhitespace = false)
    {
        return Evaluate(Print(apiCall), normalizeWhitespace);
    }

    /// <summary>
    /// Flattens a tree of ApiCalls into a single string.
    /// </summary>
    private string Print(ApiCall root)
    {
        var sb = new StringBuilder();
        Print(root, sb, 0, OpenParenthesisOnNewLine, ClosingParenthesisOnNewLine);
        var generatedCode = sb.ToString();
        return generatedCode;
    }

    private static string PrintWithDefaultFormatting(ApiCall root)
    {
        var sb = new StringBuilder();
        Print(
            root,
            sb,
            0,
            openParenthesisOnNewLine: false,
            closingParenthesisOnNewLine: false);
        var generatedCode = sb.ToString();
        return generatedCode;
    }

    private static void Print(
        ApiCall codeBlock,
        StringBuilder sb,
        int depth = 0,
        bool openParenthesisOnNewLine = false,
        bool closingParenthesisOnNewLine = false)
    {
        Print(
            codeBlock.FactoryMethodCall,
            sb,
            depth,
            useCurliesInsteadOfParentheses: codeBlock.UseCurliesInsteadOfParentheses,
            openParenthesisOnNewLine: openParenthesisOnNewLine,
            closingParenthesisOnNewLine: closingParenthesisOnNewLine);
        if (codeBlock.InstanceMethodCalls != null)
        {
            foreach (var call in codeBlock.InstanceMethodCalls)
            {
                PrintNewLine(sb);
                Print(
                    call,
                    sb,
                    depth,
                    useCurliesInsteadOfParentheses: codeBlock.UseCurliesInsteadOfParentheses,
                    openParenthesisOnNewLine: openParenthesisOnNewLine,
                    closingParenthesisOnNewLine: closingParenthesisOnNewLine);
            }
        }
    }

    private static void Print(
        MemberCall call,
        StringBuilder sb,
        int depth,
        bool openParenthesisOnNewLine = false,
        bool closingParenthesisOnNewLine = false,
        bool useCurliesInsteadOfParentheses = false)
    {
        var openParen = useCurliesInsteadOfParentheses ? "{" : "(";
        var closeParen = useCurliesInsteadOfParentheses ? "}" : ")";
        Print(call.Name, sb, depth);

        MethodCall methodCall = call as MethodCall;
        if (methodCall != null)
        {
            if (methodCall.Arguments == null || !methodCall.Arguments.Any())
            {
                Print(openParen + closeParen, sb, 0);
                return;
            }

            bool needNewLine = true;

            if (methodCall.Arguments.Count == 1 &&
                (methodCall.Arguments[0] is string || methodCall.Arguments[0] is SyntaxKind))
            {
                needNewLine = false;
            }

            if (openParenthesisOnNewLine && needNewLine)
            {
                PrintNewLine(sb);
                Print(openParen, sb, depth);
            }
            else
            {
                Print(openParen, sb, 0);
            }

            if (needNewLine)
            {
                PrintNewLine(sb);
            }

            bool needComma = false;
            foreach (var block in methodCall.Arguments)
            {
                if (needComma)
                {
                    Print(",", sb, 0);
                    PrintNewLine(sb);
                }

                if (block is string)
                {
                    Print(
                        (string)block,
                        sb,
                        needNewLine ? depth + 1 : 0);
                }
                else if (block is SyntaxKind)
                {
                    Print("SyntaxKind." + ((SyntaxKind)block).ToString(), sb, needNewLine ? depth + 1 : 0);
                }
                else if (block is ApiCall)
                {
                    Print(
                        block as ApiCall,
                        sb,
                        depth + 1,
                        openParenthesisOnNewLine: openParenthesisOnNewLine,
                        closingParenthesisOnNewLine: closingParenthesisOnNewLine);
                }
                else if (block is MemberCall)
                {
                    Print(
                        block as MemberCall,
                        sb,
                        depth + 1,
                        openParenthesisOnNewLine: openParenthesisOnNewLine,
                        closingParenthesisOnNewLine: closingParenthesisOnNewLine,
                        useCurliesInsteadOfParentheses: useCurliesInsteadOfParentheses);
                }

                needComma = true;
            }

            if (closingParenthesisOnNewLine && needNewLine)
            {
                PrintNewLine(sb);
                Print(closeParen, sb, depth);
            }
            else
            {
                Print(closeParen, sb, 0);
            }
        }
    }

    private static void PrintNewLine(StringBuilder sb)
    {
        sb.AppendLine();
    }

    private static void Print(string line, StringBuilder sb, int indent)
    {
        PrintIndent(sb, indent);
        sb.Append(line);
    }

    private static void PrintIndent(StringBuilder sb, int indent)
    {
        if (indent > 0)
        {
            sb.Append(new string(' ', indent * 4));
        }
    }

    private static string ProperCase(string str)
    {
        return char.ToUpperInvariant(str[0]) + str.Substring(1);
    }

    private static string GetPrintableTypeName(Type parameterType)
    {
        if (parameterType.GenericTypeArguments.Length == 0)
        {
            return parameterType.Name;
        }

        var sb = new StringBuilder();
        sb.Append(parameterType.Name.Substring(0, parameterType.Name.IndexOf('`')));
        sb.Append("<");
        sb.Append(string.Join(", ", parameterType.GetGenericArguments().Select(a => GetPrintableTypeName(a))));
        sb.Append(">");
        return sb.ToString();
    }

    /// <summary>
    /// Enumerates names of properties on SyntaxNode, SyntaxToken and SyntaxTrivia classes that do
    /// not impact the shape of the syntax tree and are not essential to reconstructing the tree.
    /// </summary>
    private static readonly string[] nonStructuralProperties =
    {
        "AllowsAnyExpression",
        "Arity",
        "ContainsAnnotations",
        "ContainsDiagnostics",
        "ContainsDirectives",
        "ContainsSkippedText",
        "DirectiveNameToken",
        "FullSpan",
        "HasLeadingTrivia",
        "HasTrailingTrivia",
        "HasStructuredTrivia",
        "HasStructure",
        "IsConst",
        "IsDirective",
        "IsElastic",
        "IsFixed",
        "IsMissing",
        "IsStructuredTrivia",
        "IsUnboundGenericName",
        "IsVar",
        "Kind",
        "Language",
        "Parent",
        "ParentTrivia",
        "PlainName",
        "Span",
        "SyntaxTree",
    };

    /// <summary>
    /// "Stringly typed" representation of a C# property or method invocation expression, with a
    /// string for the property or method name and a list of similarly loosely typed argument
    /// expressions. Simply speaking, this is a tree of strings.
    /// </summary>
    /// <example>
    /// Data structure to represent code (API calls) of simple hierarchical shape such as:
    /// A.B(C, D.E(F(G, H), I))
    /// </example>
    private class ApiCall
    {
        public string Name { get; private set; }
        public MemberCall FactoryMethodCall { get; private set; }
        public List<MethodCall> InstanceMethodCalls { get; private set; }
        public bool UseCurliesInsteadOfParentheses { get; private set; }

        public ApiCall()
        {
        }

        public ApiCall(string parentPropertyName, string factoryMethodName)
        {
            Name = parentPropertyName;
            FactoryMethodCall = new MemberCall
            {
                Name = factoryMethodName
            };
        }

        public ApiCall(string parentPropertyName, string factoryMethodName, List<object> arguments, bool useCurliesInsteadOfParentheses = false)
        {
            UseCurliesInsteadOfParentheses = useCurliesInsteadOfParentheses;
            Name = parentPropertyName;
            FactoryMethodCall = new MethodCall
            {
                Name = factoryMethodName,
                Arguments = arguments
            };
        }

        public ApiCall(string name, MethodCall factoryMethodCall)
        {
            Name = name;
            FactoryMethodCall = factoryMethodCall;
        }

        public void Add(MethodCall methodCall)
        {
            if (InstanceMethodCalls == null)
            {
                InstanceMethodCalls = new List<MethodCall>();
            }

            InstanceMethodCalls.Add(methodCall);
        }

        public void Remove(MethodCall methodCall)
        {
            if (InstanceMethodCalls == null)
            {
                return;
            }

            InstanceMethodCalls.Remove(methodCall);
        }

        public override string ToString()
        {
            return Quoter.PrintWithDefaultFormatting(this);
        }
    }

    /// <summary>
    /// Simple data structure to represent a member call, primarily just the string Name.
    /// </summary>
    private class MemberCall
    {
        public string Name { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            Quoter.Print(this, sb, 0);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a method call that has a Name and an arbitrary list of Arguments.
    /// </summary>
    private class MethodCall : MemberCall
    {
        public List<object> Arguments { get; set; }

        public void AddArgument(object value)
        {
            if (Arguments == null)
            {
                Arguments = new List<object>();
            }

            Arguments.Add(value);
        }
    }
}

/// <summary>
/// Represents one of basic C# syntax node kinds.
/// </summary>
public enum NodeKind
{
    CompilationUnit,
    Statement,
    Expression
}