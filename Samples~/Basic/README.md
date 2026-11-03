# Loading UI Toolkit sample

This sample shows the consumer-side flow for `com.sweetjelly.loading`.

- `Loading Package Bootstrapper.asset` installs `LoadingPackageBootstrapperSo` through Bootstrapper.
- `Sample Package Bootstrapper Profile.asset` is assigned to `BootstrapperBehaviour`.
- `LoadingSampleConsumerBehaviour` resolves `ILoadingService`, subscribes to loading events, and runs sample requests.
- `LoadingSample.uxml` and `LoadingSample.uss` define the runtime UI; the scene's UIDocument references `LoadingPanelSettings.asset`.
- `LoadingSampleTheme.tss` ships the sample's runtime theme, so importing the sample does not depend on a generated theme elsewhere in the project.

## Setup

Open `Sample Scene` and enter Play Mode.

Use the buttons to run a timed load, run a manual-completion load, mark it ready early, cancel your request, or trigger an InvalidRequest failure. Ready and Cancel control only the request started by this sample. Disable/re-enable the consumer or its GameObject to check cancellation and subscription cleanup.

Minimum duration and manual readiness finish the overall flow; they do not delay scene activation. Actual Single/Additive engine flows are covered by the package tests. The sample deliberately keeps screen and navigation policy in consumer code.
