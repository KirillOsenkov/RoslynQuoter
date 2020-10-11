using System;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using QuoterWeb;
using RoslynQuoter;

namespace QuoterService.Controllers
{
    [Route("api/[controller]")]
    public class QuoterController : Controller
    {
        [HttpPost]
        public IActionResult Post([FromBody] QuoterRequestArgument arguments)
        {
            string prefix = null;

            string responseText = "Quoter is currently down for maintenance. Please check back later.";
            if (arguments is null)
            {
                return BadRequest(responseText);
            }

            if (string.IsNullOrEmpty(arguments.SourceText))
            {
                responseText = "Please specify the source text.";
            }
            else if (arguments.SourceText.Length > 2000)
            {
                responseText = "Only strings shorter than 2000 characters are supported; your input string is " + arguments.SourceText.Length + " characters long.";
            }
            else
            {
                try
                {
                    var quoter = new Quoter
                    {
                        OpenParenthesisOnNewLine = arguments.OpenCurlyOnNewLine,
                        ClosingParenthesisOnNewLine = arguments.CloseCurlyOnNewLine,
                        UseDefaultFormatting = !arguments.PreserveOriginalWhitespace,
                        RemoveRedundantModifyingCalls = !arguments.KeepRedundantApiCalls,
                        ShortenCodeWithUsingStatic = !arguments.AvoidUsingStatic
                    };

                    responseText = quoter.QuoteText(arguments.SourceText, arguments.NodeKind);
                }
                catch (Exception ex)
                {
                    responseText = ex.ToString();

                    prefix = "Congratulations! You've found a bug in Quoter! Please open an issue at <a href=\"https://github.com/KirillOsenkov/RoslynQuoter/issues/new\" target=\"_blank\">https://github.com/KirillOsenkov/RoslynQuoter/issues/new</a> and paste the code you've typed above and this stack:";
                }
            }

            if (arguments.GenerateLinqPad)
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