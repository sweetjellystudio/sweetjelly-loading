using System;
using SWeetJelly.Shared.Result;

namespace SWeetJelly.Loading.Contract
{
    public static class LoadingErrors
    {
        public static readonly Error InvalidRequest =
            new Error("Loading.InvalidRequest", "Loading request cannot be null.");

        public static readonly Error AlreadyRunning =
            new Error("Loading.AlreadyRunning", "Loading is already running.");

        public static readonly Error Cancelled =
            new Error("Loading.Cancelled", "Loading was cancelled.");

        public static Error OperationFailed(Exception exception)
        {
            return new Error("Loading.OperationFailed", exception.Message);
        }
    }
}
