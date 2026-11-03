# Migrating to 2.0

Loading 2.0 requires Unity 6.3 LTS (`6000.3`), Bootstrapper 2.0 and Shared 2.0. Utils is no longer a dependency. The root MIT license remains unchanged. Existing ILoadingService, ILoadingTask, ILoadingOperationRunner, request constructor arguments, events and asset/script GUIDs are preserved.

## Calls and results

Malformed requests return `Loading.InvalidRequest` before tasks, operations or Started notification. This includes a nonfinite minimum duration, an unknown operation value and an empty scene name for a scene operation. Negative minimum duration still behaves as zero. The request snapshots its task list; null tasks are skipped.

Manual readiness is remembered from the beginning of the active Run. Calls outside a Run have no effect. It completes the loading flow after its tasks, operation and minimum duration, without delaying scene activation. Minimum duration uses monotonic real elapsed time and is independent of timeScale.

Operational exceptions and cancellation remain Result failures. Started/Updated observers must catch their own presentation errors or they fail the operation. All Completed/Failed subscribers are notified once; their first exception faults RunAsync after notification. A Completed subscriber error never triggers Failed. Always observe the returned task.

## Cancellation and ownership

Cancellation before an operation starts performs no scene load. After it starts, cancellation can still activate or add the requested scene: Loading unlocks activation and waits for engine completion before returning Cancelled. IsLoading remains true until cleanup ends. Implement consumer rollback separately if needed.

Use Unity calls, Ready, progress and shutdown on the main thread in serial order. Cancellation token callbacks do not invoke Unity APIs. Custom runners must finish their own started work before returning, and should honor cancellation. Progress from an old Run is ignored.

Concrete LoadingService implements IDisposable and ShutdownAsync. Closing requests cancellation and rejects new calls with `Loading.Disposed`; ShutdownAsync waits for cleanup, independently of terminal observer errors on the RunAsync task. Both tasks still need appropriate observation by their owners. The ILoadingService interface has no new implementation requirement.

Bootstrap rejects existing registrations and adapters already running or owned. Shutdown preserves foreign replacements and borrowed components. Adapter destruction releases activation immediately and managed cleanup continues while Unity's loop remains alive, with no guarantee after application exit or Play Mode teardown.

## Sample

Reimport the Basic sample to obtain its UI Toolkit UXML/USS and PanelSettings assets. The sample scene replaces runtime IMGUI with UIDocument. Timed, Manual and failure interactions remain; Cancel and Ready control only the sample's own request. Disabling the consumer cancels that request and removes subscriptions; enabling it binds again.
