# Loading Tests

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

실제 scene load 성공 플로우는 테스트용 scene과 `EditorBuildSettings` 구성이 필요하므로, 현재는 안전한 기본 동작만 검증한다.
