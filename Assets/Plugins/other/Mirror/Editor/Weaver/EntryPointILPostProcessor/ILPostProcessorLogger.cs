using System.Collections.Generic;
using Mono.CecilX;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace Mirror.Weaver
{
    public class ILPostProcessorLogger : Logger
    {
        // can't Debug.Log in ILPostProcessor. need to add to this list.
        internal List<DiagnosticMessage> Logs = new();

        public void Warning(string message)
        {
            Warning(message, null);
        }

        public void Warning(string message, MemberReference mr)
        {
            if (mr != null) message = $"{message} (at {mr})";
            LogDiagnostics(message);
        }

        public void Error(string message)
        {
            Error(message, null);
        }

        public void Error(string message, MemberReference mr)
        {
            if (mr != null) message = $"{message} (at {mr})";
            LogDiagnostics(message, DiagnosticType.Error);
        }

        private void Add(string message, DiagnosticType logType)
        {
            Logs.Add(new DiagnosticMessage
            {
                // TODO add file etc. for double click opening later?
                DiagnosticType = logType, // doesn't have .Log
                File = null,
                Line = 0,
                Column = 0,
                MessageData = message
            });
        }

        public void LogDiagnostics(string message, DiagnosticType logType = DiagnosticType.Warning)
        {
            // DiagnosticMessage can't display \n for some reason.
            // it just cuts it off and we don't see any stack trace.
            // so let's replace all line breaks so we get the stack trace.
            // (Unity 2021.2.0b6 apple silicon)
            //message = message.Replace("\n", "/");

            // lets break it into several messages instead so it's easier readable
            var lines = message.Split('\n');

            // if it's just one line, simply log it
            if (lines.Length == 1)
            {
                // tests assume exact message log.
                // don't include 'Weaver: ...' or similar.
                Add($"{message}", logType);
            }
            // for multiple lines, log each line separately with start/end indicators
            else
            {
                // first line with Weaver: ... first
                Add("----------------------------------------------", logType);
                foreach (var line in lines) Add(line, logType);
                Add("----------------------------------------------", logType);
            }
        }
    }
}