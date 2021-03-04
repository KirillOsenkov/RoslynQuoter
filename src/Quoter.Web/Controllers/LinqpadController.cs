using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using RoslynQuoter;

namespace QuoterService.Controllers
{
    [Route("api/[controller]")]
    public class LinqpadController : Controller
    {
        [HttpGet]
        public IActionResult Get(
            string sourceText,
            NodeKind nodeKind = NodeKind.CompilationUnit,
            bool openCurlyOnNewLine = false,
            bool closeCurlyOnNewLine = false,
            bool preserveOriginalWhitespace = false,
            bool keepRedundantApiCalls = false,
            bool avoidUsingStatic = false)
        {
            string responseText;

            if (string.IsNullOrEmpty(sourceText))
            {
                responseText = "Please specify the source text.";
            }
            else if (sourceText.Length > 2000)
            {
                responseText = "Only strings shorter than 2000 characters are supported; your input string is " + sourceText.Length + " characters long.";
            }
            else
            {
                try
                {
                    var quoter = new Quoter
                    {
                        OpenParenthesisOnNewLine = openCurlyOnNewLine,
                        ClosingParenthesisOnNewLine = closeCurlyOnNewLine,
                        UseDefaultFormatting = !preserveOriginalWhitespace,
                        RemoveRedundantModifyingCalls = !keepRedundantApiCalls,
                        ShortenCodeWithUsingStatic = !avoidUsingStatic
                    };

                    responseText = quoter.QuoteText(sourceText, nodeKind);
                }
                catch (Exception ex)
                {
                    responseText = ex.ToString();
                }
            }

            var linqpadFile = $@"<Query Kind=""Expression"">
  <NuGetReference>Microsoft.CodeAnalysis.Compilers</NuGetReference>
  <NuGetReference>Microsoft.CodeAnalysis.CSharp</NuGetReference>
  <Namespace>static Microsoft.CodeAnalysis.CSharp.SyntaxFactory</Namespace>
  <Namespace>Microsoft.CodeAnalysis.CSharp.Syntax</Namespace>
  <Namespace>Microsoft.CodeAnalysis.CSharp</Namespace>
  <Namespace>Microsoft.CodeAnalysis</Namespace>
</Query>

{responseText}
";

            var responseBytes = Encoding.UTF8.GetBytes(linqpadFile);

            return File(responseBytes, "application/octet-stream", "Quoter.linq");
        }
    }
}