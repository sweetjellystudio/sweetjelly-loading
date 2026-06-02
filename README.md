# SWeetJelly Loading

Loading module for SWeetJelly Unity projects.

This module provides a simple loading flow service with support for custom loading tasks, scene loading operations, loading progress events, minimum loading duration, and manual completion control.

## Structure

```plantuml
package "SWeetJelly.Loading.Contract" {
  interface ILoadingService
  interface ILoadingTask
  interface ILoadingOperationRunner
  enum LoadingOperationType
  class LoadingRequest

  class LoadingEventArgs
  class LoadingFailedEventArgs
}

package "SWeetJelly.Loading.App" {
  class LoadingService
}

package "SWeetJelly.Loading.Framework" {
  class LoadingBehaviour
}

package "SWeetJelly.Loading.InitialiserIntegration" {
  class LoadingInitModule
}
```

## Responsibilities

### Contract

Defines the public loading API.

* `ILoadingService`
* `ILoadingTask`
* `ILoadingOperationRunner`
* `LoadingRequest`
* `LoadingOperationType`
* loading event args

### App

Contains the loading flow implementation.

`LoadingService` is responsible for:

* managing loading state
* preventing duplicated loading execution
* running loading tasks
* running the requested loading operation
* raising loading events
* waiting for minimum loading duration
* waiting for manual completion when required

### Framework

Contains Unity-specific loading operation logic.

`LoadingBehaviour` is a Unity framework adapter.
It implements `ILoadingOperationRunner` and handles Unity scene loading through `SceneManager`.

Supported operations include:

* none
* simple loading operation
* single scene loading
* additive scene loading

### Initialiser Integration

`LoadingInitModule` installs the loading module into the Initialiser system.

It creates or finds `LoadingBehaviour`, creates `LoadingService`, and registers it as `ILoadingService`.

## Basic Usage

Resolve `ILoadingService` from the application context and run a loading request.

```csharp
var request = new LoadingRequest(
    operationType: LoadingOperationType.LoadScene,
    sceneName: "GameScene",
    message: "Loading...",
    minimumDuration: 1.0f);

await loadingService.RunAsync(request, cancellationToken);
```

## Loading Events

`ILoadingService` exposes events that can be used by UI or presentation code.

```csharp
loadingService.LoadingStarted += OnLoadingStarted;
loadingService.LoadingUpdated += OnLoadingUpdated;
loadingService.LoadingCompleted += OnLoadingCompleted;
loadingService.LoadingFailed += OnLoadingFailed;
```

The loading module does not provide loading UI directly.
UI should subscribe to these events and render progress independently.

## Custom Loading Tasks

Additional loading work can be added by implementing `ILoadingTask`.

```csharp
public sealed class LoadPlayerDataTask : ILoadingTask
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        // Load data here.
        await Task.CompletedTask;
    }
}
```

Tasks are executed before the main loading operation.

## Manual Completion

When `WaitForManualCompletion` is enabled, the loading flow waits until the service is marked as ready to complete.

```csharp
loadingService.MarkAsReadyToComplete();
```

This is useful when the loading operation is done, but another system needs to finish preparation before the loading screen closes.
