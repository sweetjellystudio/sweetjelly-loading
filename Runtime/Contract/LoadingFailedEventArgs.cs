using System;

namespace SWeetJelly.Loading.Contract
{
    public class LoadingFailedEventArgs : EventArgs
    {
        public string ErrorMessage { get; private set; }
        public Exception Exception { get; private set; }

        public LoadingFailedEventArgs(string errorMessage, Exception exception)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }
}