using System;
using System.Collections.Generic;

namespace SWeetJelly.Loading.Contract
{
    public class LoadingRequest
    {
        public LoadingOperationType OperationType { get; }
        public string SceneName { get; }
        public string Message { get; }
        public float MinimumDuration { get; }
        public bool WaitForManualCompletion { get; }
        public IReadOnlyList<ILoadingTask> Tasks { get; }

        public LoadingRequest(
            LoadingOperationType operationType,
            string sceneName = null,
            string message = null,
            float minimumDuration = 0f,
            bool waitForManualCompletion = false,
            IReadOnlyList<ILoadingTask> tasks = null)
        {
            OperationType = operationType;
            SceneName = sceneName;
            Message = message ?? string.Empty;
            MinimumDuration = minimumDuration;
            WaitForManualCompletion = waitForManualCompletion;
            Tasks = tasks ?? Array.Empty<ILoadingTask>();
        }
    }

}