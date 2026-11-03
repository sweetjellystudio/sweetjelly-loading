# Loading architecture

Loading coordinates preparation tasks, an optional scene operation and completion conditions. Game routing, transition animation, rollback and screen policy belong to consumers. [GLOSSARY.md](GLOSSARY.md) defines the agreed terms; [the lifetime decision](docs/adr/0001-run-lifetime-and-scene-cancellation.md) records the cancellation trade-off.

## Responsibilities and dependencies

`Contract` owns ILoadingService, ILoadingTask, ILoadingOperationRunner, requests and lifecycle notifications. `App` implements the single active Run, validation, readiness, elapsed time and Result policy. Both assemblies have `noEngineReferences=true` and use Shared.Result. There is no independent Domain assembly: this package's current rules are a small coordinated workflow, without a separate model that would benefit from another boundary.

`Framework.Unity` implements the operation port with SceneManager and owns component lifetime and scene activation cleanup. It depends on Contract. `Framework.Bootstrapper` is the composition root: it constructs App and Unity implementations, claims an adapter and registers ILoadingService through Bootstrapper's registry. It owns rollback and matching cleanup.

The sample's UI Toolkit document, styling and interaction code live in `Samples~/Basic`. Runtime service assemblies have no UI Toolkit dependency. The built-in UIElements module is declared for the importable sample. No game ViewModel, navigation policy or global service lookup enters App.

## Lifetime constraints

Unity calls, progress, manual readiness, bootstrap and shutdown use the main thread's serial context. Token callbacks only signal managed cancellation; they do not call Unity APIs. Custom runners must keep progress in the caller's context and complete only after their started work is cleaned up. A service cannot force a custom task or runner that ignores cancellation to stop.

One accepted Run remains active through terminal notification and cleanup. Started/Updated observer errors fail the operation. Completed/Failed observers all receive the terminal notification before the first observer error is propagated; a completed notification is never followed by a failure notification for the same Run. Late progress is scoped to its original Run and ignored after shutdown or termination.

Scene cancellation releases activation and drains a started operation. Adapter destruction requests the same cleanup without requiring the component to survive. It cannot undo scene changes or guarantee continuations after application exit/Play Mode teardown.

Bootstrap ownership applies to runtime instances, not exclusive references to a configuration asset. Duplicate ownership is rejected. Shutdown closes only the owned service, unregisters only the same instance and destroys only a created adapter; a borrowed adapter is preserved. External destruction of its GameObject still ends that object's lifetime. Concrete service owners can await ShutdownAsync to observe the drain boundary; registry removal itself does not await it.
