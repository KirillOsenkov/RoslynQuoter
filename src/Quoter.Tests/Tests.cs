using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynQuoter;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

public class Tests
{
    [Fact]
    public void TestInterpolatedStringWithNewLine()
    {
        var expected = $@"InterpolatedStringExpression(
    Token(SyntaxKind.InterpolatedStringStartToken))
.WithContents(
    SingletonList<InterpolatedStringContentSyntax>(
        InterpolatedStringText()
        .WithTextToken(
            Token(
                TriviaList(),
                SyntaxKind.InterpolatedStringTextToken,
                ""Foo \\n!"",
                @""Foo {"\n"}!"",
                TriviaList()))))
.NormalizeWhitespace()";

        Test("$\"Foo \\n!\"", expected, shortenCodeWithUsingStatic: true, nodeKind: NodeKind.Expression);
    }

    [Fact]
    public void TestUsingSystemWithRedundantCalls()
    {
        Test(@"using System;
", @"SyntaxFactory.CompilationUnit()
.WithUsings(
    SyntaxFactory.SingletonList<UsingDirectiveSyntax>(
        SyntaxFactory.UsingDirective(
            SyntaxFactory.IdentifierName(""System""))
        .WithUsingKeyword(
            SyntaxFactory.Token(SyntaxKind.UsingKeyword))
        .WithSemicolonToken(
            SyntaxFactory.Token(SyntaxKind.SemicolonToken))))
.WithEndOfFileToken(
    SyntaxFactory.Token(SyntaxKind.EndOfFileToken))
.NormalizeWhitespace()", removeRedundantModifyingCalls: false);
    }

    [Fact]
    public void TestUsingSystemWithUsingStatic()
    {
        Test(@"using System;
", @"CompilationUnit()
.WithUsings(
    SingletonList<UsingDirectiveSyntax>(
        UsingDirective(
            IdentifierName(""System""))))
.NormalizeWhitespace()", shortenCodeWithUsingStatic: true);
    }

    [Fact]
    public void TestUsingSystem()
    {
        Test(@"using System;
", @"SyntaxFactory.CompilationUnit()
.WithUsings(
    SyntaxFactory.SingletonList<UsingDirectiveSyntax>(
        SyntaxFactory.UsingDirective(
            SyntaxFactory.IdentifierName(""System""))))
.NormalizeWhitespace()");
    }

    [Fact]
    public void TestSimpleClass()
    {
        Test("class C { }", @"SyntaxFactory.CompilationUnit()
.WithMembers(
    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
        SyntaxFactory.ClassDeclaration(""C"")
        .WithKeyword(
            SyntaxFactory.Token(SyntaxKind.ClassKeyword))
        .WithOpenBraceToken(
            SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
        .WithCloseBraceToken(
            SyntaxFactory.Token(SyntaxKind.CloseBraceToken))))
.WithEndOfFileToken(
    SyntaxFactory.Token(SyntaxKind.EndOfFileToken))
.NormalizeWhitespace()", removeRedundantModifyingCalls: false);
    }

    [Fact]
    public void TestMissingToken()
    {
        Test("class", @"SyntaxFactory.CompilationUnit()
.WithMembers(
    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
        SyntaxFactory.ClassDeclaration(
            SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken))
        .WithKeyword(
            SyntaxFactory.Token(SyntaxKind.ClassKeyword))
        .WithOpenBraceToken(
            SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken))
        .WithCloseBraceToken(
            SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken))))
.WithEndOfFileToken(
    SyntaxFactory.Token(SyntaxKind.EndOfFileToken))
.NormalizeWhitespace()", removeRedundantModifyingCalls: false);
    }

    [Fact]
    public void TestMissingTokenWithUsingStatic()
    {
        Test("class", @"CompilationUnit()
.WithMembers(
    SingletonList<MemberDeclarationSyntax>(
        ClassDeclaration(
            MissingToken(SyntaxKind.IdentifierToken))
        .WithKeyword(
            Token(SyntaxKind.ClassKeyword))
        .WithOpenBraceToken(
            MissingToken(SyntaxKind.OpenBraceToken))
        .WithCloseBraceToken(
            MissingToken(SyntaxKind.CloseBraceToken))))
.WithEndOfFileToken(
    Token(SyntaxKind.EndOfFileToken))
.NormalizeWhitespace()", removeRedundantModifyingCalls: false, shortenCodeWithUsingStatic: true);
    }


    [Fact]
    public void TestGlobal()
    {
        Test(@"class C { void M() { global::System.String s; } }");
    }

    [Fact]
    public void TestEmptyBlock()
    {
        Test(@"class C { void M() { } }");
    }

    [Fact]
    public void TestInterpolatedString()
    {
        Test(@"class C { string s = $""a""; }");
    }

    [Fact]
    public void TestAttribute()
    {
        Test(@"[Foo]class C { }");
    }

    [Fact]
    public void TestHelloWorld()
    {
        Test(@"using System;

namespace N
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello World""); // comment
        }
    }
}", @"SyntaxFactory.CompilationUnit()
.WithUsings(
    SyntaxFactory.SingletonList<UsingDirectiveSyntax>(
        SyntaxFactory.UsingDirective(
            SyntaxFactory.IdentifierName(""System""))
        .WithUsingKeyword(
            SyntaxFactory.Token(SyntaxKind.UsingKeyword))
        .WithSemicolonToken(
            SyntaxFactory.Token(SyntaxKind.SemicolonToken))))
.WithMembers(
    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
        SyntaxFactory.NamespaceDeclaration(
            SyntaxFactory.IdentifierName(""N""))
        .WithNamespaceKeyword(
            SyntaxFactory.Token(SyntaxKind.NamespaceKeyword))
        .WithOpenBraceToken(
            SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
        .WithMembers(
            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                SyntaxFactory.ClassDeclaration(""Program"")
                .WithKeyword(
                    SyntaxFactory.Token(SyntaxKind.ClassKeyword))
                .WithOpenBraceToken(
                    SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.MethodDeclaration(
                            SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                            SyntaxFactory.Identifier(""Main""))
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                        .WithParameterList(
                            SyntaxFactory.ParameterList(
                                SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier(""args""))
                                    .WithType(
                                        SyntaxFactory.ArrayType(
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                                        .WithRankSpecifiers(
                                            SyntaxFactory.SingletonList<ArrayRankSpecifierSyntax>(
                                                SyntaxFactory.ArrayRankSpecifier(
                                                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                        SyntaxFactory.OmittedArraySizeExpression()
                                                        .WithOmittedArraySizeExpressionToken(
                                                            SyntaxFactory.Token(SyntaxKind.OmittedArraySizeExpressionToken))))
                                                .WithOpenBracketToken(
                                                    SyntaxFactory.Token(SyntaxKind.OpenBracketToken))
                                                .WithCloseBracketToken(
                                                    SyntaxFactory.Token(SyntaxKind.CloseBracketToken)))))))
                            .WithOpenParenToken(
                                SyntaxFactory.Token(SyntaxKind.OpenParenToken))
                            .WithCloseParenToken(
                                SyntaxFactory.Token(SyntaxKind.CloseParenToken)))
                        .WithBody(
                            SyntaxFactory.Block(
                                SyntaxFactory.SingletonList<StatementSyntax>(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName(""Console""),
                                                SyntaxFactory.IdentifierName(""WriteLine""))
                                            .WithOperatorToken(
                                                SyntaxFactory.Token(SyntaxKind.DotToken)))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.StringLiteralExpression,
                                                            SyntaxFactory.Literal(""Hello World"")))))
                                            .WithOpenParenToken(
                                                SyntaxFactory.Token(SyntaxKind.OpenParenToken))
                                            .WithCloseParenToken(
                                                SyntaxFactory.Token(SyntaxKind.CloseParenToken))))
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(
                                            SyntaxFactory.TriviaList(),
                                            SyntaxKind.SemicolonToken,
                                            SyntaxFactory.TriviaList(
                                                SyntaxFactory.Comment(""// comment""))))))
                            .WithOpenBraceToken(
                                SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                            .WithCloseBraceToken(
                                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)))))
                .WithCloseBraceToken(
                    SyntaxFactory.Token(SyntaxKind.CloseBraceToken))))
        .WithCloseBraceToken(
            SyntaxFactory.Token(SyntaxKind.CloseBraceToken))))
.WithEndOfFileToken(
    SyntaxFactory.Token(SyntaxKind.EndOfFileToken))
.NormalizeWhitespace()", removeRedundantModifyingCalls: false);
    }

    [Fact]
    public void TestComment()
    {
        Test(@"class C
{
  void M()
  {
    A(""M""); // comment
  }
}");
    }

    [Fact]
    public void TestSimpleStringLiteral()
    {
        Test("class C { string s = \"z\"; }"); // "z"
    }

    [Fact]
    public void TestStringLiteralWithBackslash()
    {
        Test("class C { string s = \"a\\b\"");
    }

    [Fact]
    public void TestSimpleIntLiteral()
    {
        Test("class C { int i = 42; }");
    }

    [Fact]
    public void TestSimpleCharLiteral()
    {
        Test("class C { char c = 'z'; }");
    }

    [Theory]
    [InlineData("'")]
    [InlineData("0")]
    [InlineData("a")]
    [InlineData("b")]
    [InlineData("f")]
    [InlineData("n")]
    [InlineData("r")]
    [InlineData("t")]
    [InlineData("v")]
    public void TestEscapedCharLiterals(string ch)
    {
        Test($"'\\{ch}'", $@"SyntaxFactory.LiteralExpression(
    SyntaxKind.CharacterLiteralExpression,
    SyntaxFactory.Literal('\{ch}'))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression);
    }

    [Theory]
    [InlineData("u1234", "ሴ")]
    [InlineData("x123a", "ሺ")]
    [InlineData("U00001234", "ሴ")]
    public void TestEscapedCharLiterals2(string ch, string charValue)
    {
        Test($"'\\{ch}'", $@"SyntaxFactory.LiteralExpression(
    SyntaxKind.CharacterLiteralExpression,
    SyntaxFactory.Literal(
        ""'\\{ch}'"",
        '{charValue}'))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression);
    }

    [Fact]
    public void TestEscapedCharLiterals3()
    {
        Test("'\"'", $@"SyntaxFactory.LiteralExpression(
    SyntaxKind.CharacterLiteralExpression,
    SyntaxFactory.Literal('""'))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression);
    }

    [Fact]
    public void TestEscapedCharLiterals4()
    {
        Test("'\\\\'", $@"SyntaxFactory.LiteralExpression(
    SyntaxKind.CharacterLiteralExpression,
    SyntaxFactory.Literal('\\'))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression);
    }

    [Fact]
    public void TestEscapedChar5()
    {
        Test("\"@abc\\rdef\"", nodeKind: NodeKind.Expression);
    }

    [Fact]
    public void TestEscapedChar6()
    {
        Test("@\"\\n\"", nodeKind: NodeKind.Expression);
    }

    [Fact]
    public void TestTrueFalseAndNull()
    {
        Test("class C { var x = true ? false : null; }");
    }

    [Fact]
    public void Roundtrip1()
    {
        Test("class C { string s = \"\\\"\"; }"); // "\""
    }

    [Fact]
    public void Roundtrip2()
    {
        Test(@"using System;

class Program
{
    static void Main(string[] args)
    {
        
    }
}");
    }

    [Fact]
    public void Roundtrip3()
    {
        Test("class C { string s = \"\\\"\"; }");
    }

    [Fact]
    public void Roundtrip4()
    {
        Test("class C { string s = @\" zzz \"\" zzz \"; }");
    }

    [Fact]
    public void Roundtrip5()
    {
        Test(@"class C { void M() { M(1, 2); } }");
    }

    [Fact]
    public void RoundtripBoolLiteral()
    {
        Test(@"class C { bool b = true; }");
    }

    [Fact]
    public void Roundtrip7()
    {
        Test(@"#error Foo");
    }

    [Fact]
    public void Roundtrip8()
    {
        Test(@"#if false
int i
#endif");
    }

    [Fact]
    public void Roundtrip9()
    {
        Test(@"\\\");
    }

    [Fact]
    public void Roundtrip10()
    {
        Test(@"/// baz <summary>foo</summary> bar");
    }

    [Fact]
    public void Roundtrip11()
    {
        Test(@"class /*///*/C");
    }

    [Fact]
    public void Roundtrip12()
    {
        Test("#pragma checksum \"file.txt\" \"{00000000-0000-0000-0000-000000000000}\" \"2453\"");
    }

    [Fact]
    public void Roundtrip13()
    {
        Test(@"class \\u0066 { }");
    }

    [Fact]
    public void Roundtrip14()
    {
        Test(@"class C { }");
    }

    [Fact]
    public void Roundtrip15()
    {
        Test(@"class C { void M() { ((Action)(async () =>
                {
                }))(); } }");
    }

    [Fact]
    public void Roundtrip16()
    {
        Test(@"class C { void M() { a ? b : c; } }");
    }

    [Fact]
    public void NotPattern()
    {
        Test("x is not null", nodeKind: NodeKind.Expression);
    }

    private static string GetPath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return Path.GetFullPath(relativePath);
        }

        return Path.GetFullPath(
            Path.Combine(
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location),
                relativePath));
    }

    private void RoundtripFile(string filePath)
    {
        Test(
            File.ReadAllText(GetPath(filePath)),
            useDefaultFormatting: false,
            removeRedundantCalls: false,
            shortenCodeWithUsingStatic: true);
    }

    [Fact]
    public void Roundtrip20()
    {
        Test("#line 1 \"a\\b\"");
    }

    [Fact]
    public void Roundtrip21()
    {
        Test("#line 1 \"a\\\b\"");
    }

    [Fact]
    public void Roundtrip21_1()
    {
        Test("\"\b\"", nodeKind: NodeKind.Expression);
        Test(" \"\b\"", nodeKind: NodeKind.Expression);
        Test("\"\b\" ", nodeKind: NodeKind.Expression);
        Test(" \"\b\" ", nodeKind: NodeKind.Expression);
        Test("\"a\\\b\"", nodeKind: NodeKind.Expression);
    }

    [Fact]
    public void Roundtrip22()
    {
        Test("#pragma checksum \"..\\..\"");
    }

    [Fact]
    public void Roundtrip23()
    {
        Test("class C { void P { a } }");
    }

    [Fact]
    public void Roundtrip24()
    {
        Test(@"///
class C { }");
    }

    [Fact]
    public void Roundtrip25()
    {
        Test("class C { void M(__arglist) { M(__arglist); } }");
    }

    [Fact]
    public void Roundtrip26()
    {
        Test(@"
namespace @N
{
   public class @A
   {
       public @string @P { get; set; }
   }
}
");
    }

    [Fact]
    public void Roundtrip27()
    {
        Test("class C { void M() { int x; x = 42; } }");
    }

    [Fact]
    public void Roundtrip28()
    {
        Test(@"[module: System.Copyright(""\n\t\u0123(C) \""2009"" + ""\u0123"")]");
    }

    [Fact]
    public void SwitchCase()
    {
        Test(@"class C { public C() { switch(0) { case 1: break; default: break;} } } ");
    }

    [Fact]
    public void TestObsoleteAttribute()
    {
        Test("class C { int i => 0; }");
    }

    [Fact]
    public void TestNewlineInConstant()
    {
        Test(@"[module: System.Copyright(""\n"")]");
    }

    [Fact]
    public void TestQuoteInLiteral()
    {
        Test(@"[module: A(""\"""")]");
    }

    [Fact]
    public void TestQuoteInVerbatimLiteral()
    {
        Test(@"[module: A(@"""""""")]");
    }

    [Fact]
    public void TestBackslashInLiteral()
    {
        Test(@"[module: A(""\\"")]");
    }

    [Fact]
    public void RoundtripMissingToken()
    {
        Test("class");
    }

    [Fact]
    public void TestXmlDocComment()
    {
        Test(@"    /// <summary>
    /// test
    /// </summary>
class C { }");
    }

    [Fact]
    public void TestXmlDocSummaryWithNamespace()
    {
        Test(@"    /// <summary xml:lang=""ru"">
    /// test
    /// </summary>
class C { }");
    }

    [Fact]
    public void TestXmlDocAll()
    {
        Test(@"/// <!--a-->
/// <![CDATA[c]]>
/// <completionlist cref=""a""/>
/// <exception cref=""a"" />
/// <include file='a' path='[@name=""b""]'/>
/// <permission cref=""a"" />
/// <remarks></remarks>
/// <see cref=""a""/>
/// <seealso cref=""a""/>
/// <summary>a</summary>
/// <example>a</example>
/// <param name=""a"">a</param>
class C { }");
    }

    [Fact]
    public void TestDefaultLiteral()
    {
        Test(@"class C
{
    void A(int x = default)
    {
    }
}");
    }

    [Fact]
    public void TestIssue49()
    {
        Test(
          @"if () {}",
          "Parse error. Have you selected the right Parse As context?",
          nodeKind: NodeKind.MemberDeclaration,
          testRoundtrip: false);
    }

    [Fact]
    public void TestIssue61()
    {
        Test(@"void M() { var a = M() is char and > 'H' }");
    }

    [Fact]
    public void TestIdentifiers()
    {
        Test(@"using System;
var @class = 12;
Console.WriteLine(nameof(@class));");
    }

    [Fact]
    public void TestFloatLiteral()
    {
        Test("1F", @"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(1F))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression);
    }

    [Fact]
    public void TestDoubleLiteral()
    {
        Test("1D", @"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(
        ""1D"",
        1D))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );
    }

    [Fact]
    public void TestDoubleLiteralSmall()
    {
        Test("1d", @"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(
        ""1d"",
        1d))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );
    }

    [Fact]
    public void TestDecimalLiteral()
    {
        Test("1M", @"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(1M))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );
    }

    [Fact]
    public void TestDecimalLiteralSmall()
    {
        Test("1m", @"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(
        ""1m"",
        1m))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );
    }

    [Fact]
    public void TestUnsignedLiteral()
    {
        Test("1u", @"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(
        ""1u"",
        1u))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );

        Test("0x1u", @"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(
        ""0x1u"",
        0x1u))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );
    }

    [Fact]
    public void TestLongLiteral()
    {
        Test("1l", @"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(
        ""1l"",
        1l))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );

        Test("0x1L", @"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(
        ""0x1L"",
        0x1L))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );
    }

    [Theory]
    [InlineData("ul")]
    [InlineData("uL")]
    [InlineData("Ul")]
    [InlineData("lu")]
    [InlineData("lU")]
    [InlineData("Lu")]
    [InlineData("LU")]
    public void TestUnsignedLongLiteral(string suffix)
    {
        Test("1" + suffix, $@"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(
        ""1{suffix}"",
        1{suffix}))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );

        Test("0x2" + suffix, $@"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(
        ""0x2{suffix}"",
        0x2{suffix}))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );
    }

    [Fact]
    public void TestUL()
    {
        const string suffix = "UL";
        Test("1" + suffix, $@"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(1{suffix}))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );

        Test("0x2" + suffix, $@"SyntaxFactory.LiteralExpression(
    SyntaxKind.NumericLiteralExpression,
    SyntaxFactory.Literal(
        ""0x2{suffix}"",
        0x2{suffix}))
.NormalizeWhitespace()", nodeKind: NodeKind.Expression
        );
    }

    [Fact]
    public void TestIssue77()
    {
        Test("Foo(0x0000800000000000)", NodeKind.Expression);
    }

    [Fact]
    public void TestBinaryLiteral()
    {
        Test("0b_0010_1010", NodeKind.Expression);
    }

    private void Test(
        string sourceText,
        string expected,
        bool useDefaultFormatting = true,
        bool removeRedundantModifyingCalls = true,
        bool shortenCodeWithUsingStatic = false,
        NodeKind nodeKind = NodeKind.CompilationUnit,
        bool testRoundtrip = true)
    {
        var quoter = new Quoter
        {
            UseDefaultFormatting = useDefaultFormatting,
            RemoveRedundantModifyingCalls = removeRedundantModifyingCalls,
            ShortenCodeWithUsingStatic = shortenCodeWithUsingStatic
        };
        var actual = quoter.QuoteText(sourceText, nodeKind);
        Assert.Equal(expected, actual);

        if (testRoundtrip)
        {
            Test(sourceText, nodeKind);
        }
    }

    private void Test(string sourceText, NodeKind nodeKind = NodeKind.CompilationUnit)
    {
        Test(sourceText, useDefaultFormatting: true, removeRedundantCalls: true, shortenCodeWithUsingStatic: false, nodeKind);
        Test(sourceText, useDefaultFormatting: false, removeRedundantCalls: true, shortenCodeWithUsingStatic: true, nodeKind);
    }

    private static void Test(
      string sourceText,
      bool useDefaultFormatting,
      bool removeRedundantCalls,
      bool shortenCodeWithUsingStatic,
      NodeKind nodeKind = NodeKind.CompilationUnit)
    {
        if (useDefaultFormatting)
        {
            sourceText = CSharpSyntaxTree
                .ParseText(sourceText)
                .GetRoot()
                .NormalizeWhitespace()
                .ToFullString();
        }

        var quoter = new Quoter
        {
            UseDefaultFormatting = useDefaultFormatting,
            RemoveRedundantModifyingCalls = removeRedundantCalls,
            ShortenCodeWithUsingStatic = shortenCodeWithUsingStatic
        };
        var generatedCode = quoter.Quote(sourceText, nodeKind);

        var resultText = quoter.Evaluate(generatedCode);

        if (sourceText != resultText)
        {
            //File.WriteAllText(@"D:\1.txt", sourceText);
            //File.WriteAllText(@"D:\2.txt", resultText);
            //File.WriteAllText(@"D:\3.txt", generatedCode);
        }

        Assert.Equal(sourceText, resultText);
    }

    public void CheckSourceFiles()
    {
        var rootFolder = @"C:\roslyn-internal\Closed\Test\Files\";
        var files = Directory.GetFiles(rootFolder, "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            VerifyRoundtrip(files[i]);
        }
    }

    public void VerifyRoundtrip(string file)
    {
        try
        {
            var sourceText = File.ReadAllText(file);
            if (sourceText.Length > 50000)
            {
                //Log("Skipped large file: " + file);
                return;
            }

            Test(sourceText);
        }
        catch (Exception)
        {
            Log("Failed: " + file);
        }
    }

    private static void Log(string text)
    {
        File.AppendAllText(@"Failed.txt", text + Environment.NewLine);
    }
}