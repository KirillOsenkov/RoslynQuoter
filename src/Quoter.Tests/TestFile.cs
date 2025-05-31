using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

class TestFile
{
    public static void Test()
    {
        var text = SyntaxFactory.XmlText()
                            .WithTextTokens(
                                SyntaxFactory.TokenList(
                                    SyntaxFactory.XmlTextLiteral(
                                        SyntaxFactory.TriviaList(
                                            SyntaxFactory.DocumentationCommentExterior("///")),
                                        " ",
                                        " ",
                                        SyntaxFactory.TriviaList())));
        var keyword = SyntaxFactory.XmlEmptyElement("inheritdoc");
        //var keyword = SyntaxFactory.XmlNullKeywordElement()
        //                    .WithName(
        //                        SyntaxFactory.XmlName(
        //                            SyntaxFactory.Identifier("inheritdoc")));
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
                    SyntaxKind.SingleLineDocumentationCommentTrivia,
                    SyntaxFactory.List<XmlNodeSyntax>(
                        new XmlNodeSyntax[]{text
                            ,
                            keyword}));

        var cu = SyntaxFactory.CompilationUnit()
.WithEndOfFileToken(
    SyntaxFactory.Token(
        SyntaxFactory.TriviaList(
            SyntaxFactory.Trivia(trivia
                )),
        SyntaxKind.EndOfFileToken,
        SyntaxFactory.TriviaList()));

        var str = cu.ToFullString();
    }
}