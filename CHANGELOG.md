# CHANGELOG

## v1.0.0

* [Added] Added `SWeetJelly.Loading` as an independent Unity package extracted from the previous Core module.
* [Added] Added loading contracts including `ILoadingService`, `ILoadingTask`, `ILoadingOperationRunner`, `LoadingRequest`, and loading event args.
* [Added] Added `LoadingService` as the application-level loading workflow service.
* [Added] Added Unity loading driver support through `LoadingBehaviour`.
* [Added] Added Bootstrapper integration through `LoadingPackageBootstrapperSo`.
* [Added] Added `LoadingErrors` for package-owned loading failure descriptions.
* [Changed] `ILoadingService.RunAsync` now returns `Result`.
* [Changed] Moved loading logic from the unclassified Core module into dedicated Loading assemblies.
* [Changed] Separated Loading into `Contract`, `App`, `Framework/Unity`, and `Framework/Bootstrapper` layers.
* [Changed] Kept loading UI concerns outside the Loading package so UI can remain independently implemented.
