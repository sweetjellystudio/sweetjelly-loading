# SWeetJelly Loading

Loading module for SWeetJelly Unity projects.

Version 2.0 requires Unity 6.3 LTS. Read [MIGRATION.md](MIGRATION.md) for cancellation and observer behavior changes, [ARCHITECTURE.md](ARCHITECTURE.md) for ownership, and [Tests/README.md](Tests/README.md) for verification. The importable Basic sample uses UI Toolkit.

## Installation

Add the public Git URLs for Loading and its custom dependencies to the consuming project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.sweetjelly.loading": "https://github.com/sweetjellystudio/sweetjelly-loading.git#v2.0.0",
    "com.sweetjelly.bootstrapper": "https://github.com/sweetjellystudio/sweetjelly-bootstrapper.git#v2.0.0",
    "com.sweetjelly.shared": "https://github.com/sweetjellystudio/sweetjelly-shared.git#v2.0.0"
  }
}
```

Use the Loading URL after its tag is published. Local validation before publication does not verify that public tag. UPM package dependencies declare versions; custom Git URLs belong in the consumer manifest, including transitive custom dependencies.

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

package "SWeetJelly.Loading.Framework.Unity" {
  class LoadingBehaviour
}

package "SWeetJelly.Loading.Framework.Bootstrapper" {
  class LoadingPackageBootstrapperSo
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
* `LoadingErrors`
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

### Bootstrapper Integration

`LoadingPackageBootstrapperSo` installs the loading module into the Bootstrapper system.

It creates or finds `LoadingBehaviour`, creates `LoadingService`, and registers it as `ILoadingService`.

## LoadingRequest

- `operationType`: `None`, `Simple`, `LoadScene`, or `LoadAdditiveScene`
- `sceneName`: required for scene loading operations
- `message`: message passed through loading events
- `minimumDuration`: minimum loading time in seconds
- `waitForManualCompletion`: waits until `MarkAsReadyToComplete()` is called
- `tasks`: custom tasks executed before the main loading operation

## Basic Usage

Resolve `ILoadingService` from the application context and run a loading request.

```csharp
var request = new LoadingRequest(
    operationType: LoadingOperationType.LoadScene,
    sceneName: "GameScene",
    message: "Loading...",
    minimumDuration: 1.0f);

var result = await loadingService.RunAsync(request, cancellationToken);

if (result.IsFailure)
{
    // Inspect result.Error.
    return;
}
```

`RunAsync` returns a `Result`. Concrete loading failures are described through `LoadingErrors`.

The returned `Result` is the authoritative command outcome. Loading events are lifecycle notifications for UI, logging, or presentation code.

Unhandled Completed/Failed observer exceptions fault the returned task after all terminal observers have been notified. Started/Updated observer exceptions instead produce an OperationFailed Result. Observe the task and handle your own presentation errors.

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

When loading fails, `LoadingFailedEventArgs.Error` contains the same error returned by `RunAsync`.

Event subscribers are responsible for handling their own exceptions. `LoadingService` only publishes lifecycle events; Unity presenters, views, or adapters should catch and log view update failures in their own layer.

```csharp
private void OnLoadingUpdated(object sender, LoadingEventArgs args)
{
    try
    {
        view.SetProgress(args.Progress, args.Message);
    }
    catch (Exception ex)
    {
        Debug.LogException(ex);
    }
}
```

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

Readiness is remembered from the start of the active Run; calls outside a Run are ignored. Minimum duration measures the whole successful flow using real monotonic time. Neither option delays Unity scene activation.

Scene cancellation can still change the loaded scenes: a started engine operation is unlocked and drained before Cancelled is returned. There is no automatic rollback or unload. Use Unity calls, Ready, progress and shutdown on the main thread. Concrete service owners can Dispose to close new requests and request cancellation, or await ShutdownAsync to wait for cleanup.
