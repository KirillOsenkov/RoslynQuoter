using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using RoslynQuoter;

namespace QuoterService.Controllers
{
    [Route("api/[controller]")]
    public class QuoterController : Controller
    {
        [HttpGet]
        public IActionResult Get(
            string sourceText,
            NodeKind nodeKind = NodeKind.CompilationUnit,
            bool openCurlyOnNewLine = false,
            bool closeCurlyOnNewLine = false,
            bool preserveOriginalWhitespace = false,
            bool keepRedundantApiCalls = false,
            bool avoidUsingStatic = false,
            bool generateLINQPad = false)
        {
            string prefix = null;

            string responseText = "Quoter is currently down for maintenance. Please check back later.";
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

                    prefix = "Congratulations! You've found a bug in Quoter! Please open an issue at <a href=\"https://github.com/KirillOsenkov/RoslynQuoter/issues/new\" target=\"_blank\">https://github.com/KirillOsenkov/RoslynQuoter/issues/new</a> and paste the code you've typed above and this stack:";
                }
            }

            if (generateLINQPad)
            {
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

            responseText = HttpUtility.HtmlEncode(responseText);

            if (prefix != null)
            {
                responseText = "<div class=\"error\"><p>" + prefix + "</p><p>" + responseText + "</p><p><br/>P.S. Sorry!</p></div>";
            }

            return Ok(responseText);
        }
    }
}