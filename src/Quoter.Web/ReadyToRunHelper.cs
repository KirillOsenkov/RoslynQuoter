using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuoterWeb
{
    public static class ReadyToRunHelper
    {
        public static string CreateReadyToRunCode(QuoterRequestArgument arguments, string roslynCode)
        {
            return @$"using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;{(arguments.AvoidUsingStatic ? string.Empty : @"

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;")}

/*
{arguments.SourceText}
*/

var tree = SyntaxTree(
//CODE FROM ROSLYN QUOTER:
{roslynCode}
//END
);

var refApis = AppDomain.CurrentDomain.GetAssemblies()
    .Where(a => !a.IsDynamic)
    .Select(a => MetadataReference.CreateFromFile(a.Location));

var compilation = CSharpCompilation.Create(""something"", new[] {{ tree }}, refApis);
var diag = compilation.GetDiagnostics().Where(e => e.Severity == DiagnosticSeverity.Error).ToList();

foreach(var d in diag)
{{
    Console.WriteLine(d);
}}";
        }
    }
}
