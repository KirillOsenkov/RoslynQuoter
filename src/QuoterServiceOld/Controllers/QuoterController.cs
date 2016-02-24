using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace QuoterService.Controllers
{
    public class QuoterController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Get(
            string sourceText,
            bool openCurlyOnNewLine = false,
            bool closeCurlyOnNewLine = false,
            bool preserveOriginalWhitespace = false,
            bool keepRedundantApiCalls = false,
            bool avoidUsingStatic = false)
        {
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

                    responseText = quoter.Quote(sourceText);
                }
                catch (Exception ex)
                {
                    responseText = ex.ToString();
                }
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(responseText, Encoding.UTF8, "text/html");
            return response;
        }
    }
}