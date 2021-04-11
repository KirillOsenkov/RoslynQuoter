using RoslynQuoter;

namespace QuoterWeb
{
    public class QuoterRequestArgument
    {
        public string SourceText { get; set; }
        public NodeKind NodeKind { get; set; }
        public bool OpenCurlyOnNewLine { get; set; }
        public bool CloseCurlyOnNewLine { get; set; }
        public bool PreserveOriginalWhitespace { get; set; }
        public bool KeepRedundantApiCalls { get; set; }
        public bool AvoidUsingStatic { get; set; }
        public bool GenerateLinqPad { get; set; }
        public bool ReadyToRun { get; set; }
    }
}