using System;
using SWeetJelly.Shared.Result;

namespace SWeetJelly.Loading.Contract
{
    public class LoadingFailedEventArgs : EventArgs
    {
        public Error Error { get; }
        public string ErrorMessage => Error.Message;
        public Exception Exception { get; }

        public LoadingFailedEventArgs(Error error, Exception exception = null)
        {
            Error = error;
            Exception = exception;
        }
    }
}
