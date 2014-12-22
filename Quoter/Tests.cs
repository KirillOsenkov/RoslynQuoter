﻿using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
    [TestMethod]
    public void TestSimpleClass ( )
    {
        Test( "class C { }", @"SyntaxFactory.CompilationUnit()
.WithMembers(
    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
        SyntaxFactory.ClassDeclaration(
            @""C"")
        .WithKeyword(
            SyntaxFactory.Token(
                SyntaxKind.ClassKeyword))
        .WithOpenBraceToken(
            SyntaxFactory.Token(
                SyntaxKind.OpenBraceToken))
        .WithCloseBraceToken(
            SyntaxFactory.Token(
                SyntaxKind.CloseBraceToken))))
.WithEndOfFileToken(
    SyntaxFactory.Token(
        SyntaxKind.EndOfFileToken))
.NormalizeWhitespace()" );
    }

    [TestMethod]
    public void TestMissingToken ( )
    {
        Test( "class", @"SyntaxFactory.CompilationUnit()
.WithMembers(
    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
        SyntaxFactory.ClassDeclaration(
            SyntaxFactory.MissingToken(
                SyntaxKind.IdentifierToken))
        .WithKeyword(
            SyntaxFactory.Token(
                SyntaxKind.ClassKeyword))
        .WithOpenBraceToken(
            SyntaxFactory.MissingToken(
                SyntaxKind.OpenBraceToken))
        .WithCloseBraceToken(
            SyntaxFactory.MissingToken(
                SyntaxKind.CloseBraceToken))))
.WithEndOfFileToken(
    SyntaxFactory.Token(
        SyntaxKind.EndOfFileToken))
.NormalizeWhitespace()" );
    }

    [TestMethod]
    public void TestGlobal ( )
    {
        Test( @"class C { void M() { global::System.String s; } }" );
    }

    [TestMethod]
    public void TestHelloWorld ( )
    {
        Test( @"using System;

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
            SyntaxFactory.IdentifierName(
                @""System""))
        .WithUsingKeyword(
            SyntaxFactory.Token(
                SyntaxKind.UsingKeyword))
        .WithSemicolonToken(
            SyntaxFactory.Token(
                SyntaxKind.SemicolonToken))))
.WithMembers(
    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
        SyntaxFactory.NamespaceDeclaration(
            SyntaxFactory.IdentifierName(
                @""N""))
        .WithNamespaceKeyword(
            SyntaxFactory.Token(
                SyntaxKind.NamespaceKeyword))
        .WithOpenBraceToken(
            SyntaxFactory.Token(
                SyntaxKind.OpenBraceToken))
        .WithMembers(
            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                SyntaxFactory.ClassDeclaration(
                    @""Program"")
                .WithKeyword(
                    SyntaxFactory.Token(
                        SyntaxKind.ClassKeyword))
                .WithOpenBraceToken(
                    SyntaxFactory.Token(
                        SyntaxKind.OpenBraceToken))
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.MethodDeclaration(
                            SyntaxFactory.PredefinedType(
                                SyntaxFactory.Token(
                                    SyntaxKind.VoidKeyword)),
                            SyntaxFactory.Identifier(
                                @""Main""))
                        .WithModifiers(
                            SyntaxFactory.TokenList(
                                SyntaxFactory.Token(
                                    SyntaxKind.StaticKeyword)))
                        .WithParameterList(
                            SyntaxFactory.ParameterList(
                                SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier(
                                            @""args""))
                                    .WithType(
                                        SyntaxFactory.ArrayType(
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(
                                                    SyntaxKind.StringKeyword)))
                                        .WithRankSpecifiers(
                                            SyntaxFactory.SingletonList<ArrayRankSpecifierSyntax>(
                                                SyntaxFactory.ArrayRankSpecifier(
                                                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                        SyntaxFactory.OmittedArraySizeExpression()
                                                        .WithOmittedArraySizeExpressionToken(
                                                            SyntaxFactory.Token(
                                                                SyntaxKind.OmittedArraySizeExpressionToken))))
                                                .WithOpenBracketToken(
                                                    SyntaxFactory.Token(
                                                        SyntaxKind.OpenBracketToken))
                                                .WithCloseBracketToken(
                                                    SyntaxFactory.Token(
                                                        SyntaxKind.CloseBracketToken)))))))
                            .WithOpenParenToken(
                                SyntaxFactory.Token(
                                    SyntaxKind.OpenParenToken))
                            .WithCloseParenToken(
                                SyntaxFactory.Token(
                                    SyntaxKind.CloseParenToken)))
                        .WithBody(
                            SyntaxFactory.Block(
                                SyntaxFactory.SingletonList<StatementSyntax>(
                                    SyntaxFactory.ExpressionStatement(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName(
                                                    @""Console""),
                                                SyntaxFactory.IdentifierName(
                                                    @""WriteLine""))
                                            .WithOperatorToken(
                                                SyntaxFactory.Token(
                                                    SyntaxKind.DotToken)))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.StringLiteralExpression,
                                                            SyntaxFactory.Literal(
                                                                SyntaxFactory.TriviaList(),
                                                                @""""""Hello World"""""",
                                                                @""""""Hello World"""""",
                                                                SyntaxFactory.TriviaList())))))
                                            .WithOpenParenToken(
                                                SyntaxFactory.Token(
                                                    SyntaxKind.OpenParenToken))
                                            .WithCloseParenToken(
                                                SyntaxFactory.Token(
                                                    SyntaxKind.CloseParenToken))))
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(
                                            SyntaxFactory.TriviaList(),
                                            SyntaxKind.SemicolonToken,
                                            SyntaxFactory.TriviaList(
                                                SyntaxFactory.Comment(
                                                    @""// comment""))))))
                            .WithOpenBraceToken(
                                SyntaxFactory.Token(
                                    SyntaxKind.OpenBraceToken))
                            .WithCloseBraceToken(
                                SyntaxFactory.Token(
                                    SyntaxKind.CloseBraceToken)))))
                .WithCloseBraceToken(
                    SyntaxFactory.Token(
                        SyntaxKind.CloseBraceToken))))
        .WithCloseBraceToken(
            SyntaxFactory.Token(
                SyntaxKind.CloseBraceToken))))
.WithEndOfFileToken(
    SyntaxFactory.Token(
        SyntaxKind.EndOfFileToken))
.NormalizeWhitespace()" );
    }

    [TestMethod]
    public void Roundtrip1 ( )
    {
        Test( "class C { string s = \"\\\"\"; }" );
    }

    [TestMethod]
    public void Roundtrip2 ( )
    {
        Test( @"using System;

class Program
{
    static void Main(string[] args)
    {
        
    }
}" );
    }

    [TestMethod]
    public void Roundtrip3 ( )
    {
        Test( "class C { string s = \"\\\"\"; }" );
    }

    [TestMethod]
    public void Roundtrip4 ( )
    {
        Test( "class C { string s = @\"\"\"\"; }" );
    }

    [TestMethod]
    public void Roundtrip5 ( )
    {
        Test( @"class C { void M() { M(1, 2); } }" );
    }

    [TestMethod]
    public void Roundtrip6 ( )
    {
        Test( @"class C { bool b = true; }" );
    }

    [TestMethod]
    public void Roundtrip7 ( )
    {
        Test( @"#error Foo" );
    }

    [TestMethod]
    public void Roundtrip8 ( )
    {
        Test( @"#if false
int i
#endif" );
    }

    [TestMethod]
    public void Roundtrip9 ( )
    {
        Test( @"\\\" );
    }

    [TestMethod]
    public void Roundtrip10 ( )
    {
        Test( @"/// baz <summary>foo</summary> bar" );
    }

    [TestMethod]
    public void Roundtrip11 ( )
    {
        Test( @"class /*///*/C" );
    }

    [TestMethod]
    public void Roundtrip12 ( )
    {
        Test( "#pragma checksum \"file.txt\" \"{00000000-0000-0000-0000-000000000000}\" \"2453\"" );
    }

    [TestMethod]
    public void Roundtrip13 ( )
    {
        Test( @"class \\u0066 { }" );
    }

    [TestMethod]
    public void Roundtrip14 ( )
    {
        Test( @"class C { }" );
    }

    [TestMethod]
    public void Roundtrip15 ( )
    {
        Test( @"class C { void M() { ((Action)(async () =>
                {
                }))(); } }" );
    }

    [TestMethod]
    public void Roundtrip16 ( )
    {
        Test( @"class C { void M() { a ? b : c; } }" );
    }

    private static string GetPath ( string relativePath )
    {
        if ( Path.IsPathRooted( relativePath ) )
        {
            return Path.GetFullPath( relativePath );
        }

        return Path.GetFullPath(
            Path.Combine(
                Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly( ).Location ),
                relativePath ) );
    }

    private void RoundtripFile ( string filePath )
    {
        Test(
            File.ReadAllText( GetPath( filePath ) ),
            useDefaultFormatting: false,
            removeRedundantCalls: false );
    }

    [TestMethod]
    public void Roundtrip20 ( )
    {
        Test( "#line 1 \"a\\b\"" );
    }

    [TestMethod]
    public void Roundtrip21 ( )
    {
        Test( "#line 1 \"a\\\b\"" );
    }

    [TestMethod]
    public void Roundtrip22 ( )
    {
        Test( "#pragma checksum \"..\\..\"" );
    }

    // [TestMethod]
    [WorkItem(15194)]
    public void Roundtrip23 ( )
    {
        Test( "class C { void P { a } }" );
    }

    [TestMethod]
    public void Roundtrip24 ( )
    {
        Test( @"///
class C { }" );
    }

    [TestMethod]
    public void Roundtrip25 ( )
    {
        Test( "class C { void M(__arglist) { M(__arglist); } }" );
    }

    [TestMethod]
    public void Roundtrip26 ( )
    {
        Test( @"
namespace @N
{
   public class @A
   {
       public @string @P { get; set; }
   }
}
" );
    }

    [TestMethod]
    public void RoundtripMissingToken ( )
    {
        Test( "class" );
    }

    private void Test ( string sourceText, string expected, bool useDefaultFormatting = true )
    {
        var quoter = new Quoter( useDefaultFormatting: useDefaultFormatting);
        var actual = quoter.Quote(sourceText);
        Assert.AreEqual( expected, actual );

        Test( sourceText );
    }

    private void Test ( string sourceText )
    {
        Test( sourceText, useDefaultFormatting: true, removeRedundantCalls: true );
        Test( sourceText, useDefaultFormatting: false, removeRedundantCalls: true );
    }

    private static void Test ( string sourceText, bool useDefaultFormatting, bool removeRedundantCalls )
    {
        if ( useDefaultFormatting )
        {
            sourceText = CSharpSyntaxTree
                .ParseText( sourceText )
                .GetRoot( )
                .NormalizeWhitespace( )
                .ToFullString( );
        }

        var quoter = new Quoter( useDefaultFormatting: useDefaultFormatting, removeRedundantModifyingCalls: removeRedundantCalls);
        var generatedCode = quoter.Quote(sourceText);

        ////var evaluator = new Evaluator();
        ////var generatedNode = evaluator.Evaluate(generatedCode) as CompilationUnitSyntax;

        ////var resultText = generatedNode.ToFullString();

        ////if (sourceText != resultText)
        ////{
        ////    File.WriteAllText(@"E:\1.txt", sourceText);
        ////    File.WriteAllText(@"E:\2.txt", resultText);
        ////    File.WriteAllText(@"E:\3.txt", generatedCode);
        ////}

        ////Assert.AreEqual(sourceText, resultText);
    }

    public void CheckSourceFiles ( )
    {
        var rootFolder = @"E:\Roslyn\Main";
        var files = Directory.GetFiles(rootFolder, "*.cs", SearchOption.AllDirectories);
        for ( int i = 0; i < files.Length; i++ )
        {
            VerifyRoundtrip( files [ i ] );
        }
    }

    public void VerifyRoundtrip ( string file )
    {
        try
        {
            var sourceText = File.ReadAllText(file);
            if ( sourceText.Length > 50000 )
            {
                //Log("Skipped large file: " + file);
                return;
            }

            Test( sourceText );
        }
        catch (Exception)
        {
            Log( "Failed: " + file );
        }
    }

    private static void Log ( string text )
    {
        File.AppendAllText( @"E:\Failed.txt", text + Environment.NewLine );
    }
}