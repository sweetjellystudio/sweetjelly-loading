using System;

namespace SWeetJelly.Loading.Contract
{
    public class LoadingEventArgs : EventArgs
    {
        public float Progress { get; private set; }
        public string Message { get; private set; }

        public LoadingEventArgs(float progress, string message)
        {
            Progress = progress;
            Message = message;
        }
    }
}