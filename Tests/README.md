# Loading Tests

Unity 6.3 LTS is the supported baseline. Add `com.sweetjelly.loading` to the consumer's manifest `testables` and install the declared dependencies. When using Git dependencies, explicitly list the Bootstrapper and Shared Git URLs as described in the root README.

## Verification boundaries

EditMode tests exercise public request/service behavior, task-list snapshots, early readiness, validation, cancellation, shutdown drain and terminal observer errors. Bootstrap integration tests cover existing registrations, borrowed/replaced adapter ownership, reuse and partial registration failure rollback.

PlayMode tests exercise actual Single/Additive scene operations, cancellation before and after start, progress failure, adapter destruction, unlocked engine queues and timeScale-independent completion. The engine fixture implements IPrebuildSetup/IPostBuildCleanup to add its scene assets before entering Play Mode and restore the previous scene list afterward. If a test run is interrupted, restore its saved setup through the fixture Cleanup before another run; the fixture refuses to overwrite an un-restored snapshot.

Import the Basic sample for UI Toolkit verification. Check Timed, early Manual Ready, Cancel, invalid-request feedback, component/GameObject disable/re-enable and protection of another consumer's Run. Inspect the UIDocument's UXML and PanelSettings assignments and check fresh import diagnostics as well as the rendered screen.

Development-host results do not substitute for a minimal consumer. Selected ordinary Windows Mono and IL2CPP Player checks are recorded separately by the development host; this README does not claim that all platforms or publicly published tags have been tested.

이 폴더는 `com.sweetjelly.loading` 패키지를 현재 Unity 테스트 프로젝트에서 검증하기 위한 테스트를 담는다.

## EditMode

`EditMode` 테스트는 Unity 씬 실행 없이 Loading 패키지의 순수 로직과 패키지 연동 구성을 검증한다.

- `LoadingServiceTest`
  - `LoadingService`가 시작, 진행, 완료 이벤트를 순서대로 발생시키는지 검증한다.
  - 이미 로딩 중일 때 중복 실행을 막는지 검증한다.
  - 로딩 task 실패 시 `LoadingFailed` 이벤트를 발생시키고 `IsLoading` 상태를 복구하는지 검증한다.
  - manual completion 모드에서 `MarkAsReadyToComplete()` 호출 전까지 완료되지 않는지 검증한다.

- `LoadingPackageIntegrationTest`
  - `LoadingPackageBootstrapperSo`이 `LoadingBehaviour`를 holder object에 추가하는지 검증한다.
  - `LoadingPackageBootstrapperSo`이 `ILoadingService`를 `ServiceRegistry`에 등록하는지 검증한다.

## PlayMode

`PlayMode` 테스트는 Unity 런타임 객체인 `LoadingBehaviour`를 실제 GameObject에 붙여 검증한다.

- `LoadingSceneFlowTest`
  - `Simple` loading operation이 완료되는지 검증한다.
  - scene loading 요청에 scene name이 없을 때 `ArgumentException`이 발생하는지 검증한다.

`LoadingEngineLifetimeTest`는 패키지의 실제 씬 fixture와 진입 전 scene-list 준비를 사용해 Single/Additive 성공·취소·예외·파괴 정리를 검증한다.
