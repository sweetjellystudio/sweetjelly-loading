# CHANGELOG

## v1.0.0

* [Added] Added `SWeetJelly.Loading` as an independent Unity package extracted from the previous Core module.
* [Added] Added loading contracts including `ILoadingService`, `ILoadingTask`, `ILoadingOperationRunner`, `LoadingRequest`, and loading event args.
* [Added] Added `LoadingService` as the application-level loading workflow service.
* [Added] Added Unity loading driver support through `LoadingBehaviour`.
* [Added] Added Initialiser integration through `LoadingInitModule`.
* [Changed] Moved loading logic from the unclassified Core module into dedicated Loading assemblies.
* [Changed] Separated Loading into `Contract`, `App`, `Framework`, and `InitialiserIntegration` layers.
* [Changed] Kept loading UI concerns outside the Loading package so UI can remain independently implemented.
