# Loading Dev Sample

This sample shows the consumer-side flow for `com.sweetjelly.loading`.

- `Loading Package Bootstrapper.asset` installs `LoadingPackageBootstrapperSo` through Bootstrapper.
- `Sample Package Bootstrapper Profile.asset` is assigned to `BootstrapperBehaviour`.
- `LoadingSampleConsumerBehaviour` resolves `ILoadingService`, subscribes to loading events, and runs sample requests.

## Setup

Open `Sample Scene` and enter Play Mode.

Use the on-screen buttons to run a timed load, run a manual-completion load, mark a manual load as ready, or trigger a validation failure.
