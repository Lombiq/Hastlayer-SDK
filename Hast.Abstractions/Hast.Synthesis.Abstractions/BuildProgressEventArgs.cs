using System;

namespace Hast.Synthesis.Abstractions
{
    public class BuildProgressEventArgs : EventArgs
    {
        public string Message { get; set; }
        public bool IsMajorStep { get; set; }

        public BuildProgressEventArgs(string message = null, bool isMajorStep = false)
        {
            Message = message ?? string.Empty;
            IsMajorStep = isMajorStep;
        }
    }
}
