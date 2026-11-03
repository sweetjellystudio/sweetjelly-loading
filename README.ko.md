# SWeetJelly Loading

SWeetJelly Unity 프로젝트를 위한 loading module입니다.

2.0의 최소 지원 환경은 Unity 6.3 LTS입니다. 설치 Git URL과 의존성은 [영문 README](README.md), 변경된 취소·이벤트·종료 계약은 [MIGRATION.md](MIGRATION.md), 책임과 소유권은 [ARCHITECTURE.md](ARCHITECTURE.md)를 확인하세요. Basic 샘플은 UI Toolkit으로 전환합니다.

커스텀 loading task, scene loading operation, progress event, 최소 loading duration, manual completion control을 지원하는 단순한 loading flow service를 제공합니다.

## 구조

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

## 책임

### Contract

공개 loading API를 정의합니다.

- `ILoadingService`
- `ILoadingTask`
- `ILoadingOperationRunner`
- `LoadingRequest`
- `LoadingOperationType`
- `LoadingErrors`
- loading event args

### App

loading flow 구현을 포함합니다.

`LoadingService`의 책임:

- loading state 관리
- 중복 loading 실행 방지
- loading task 실행
- 요청된 loading operation 실행
- loading event 발행
- 최소 loading duration 대기
- 필요한 경우 manual completion 대기

### Framework

Unity 전용 loading operation logic을 포함합니다.

`LoadingBehaviour`는 Unity framework adapter입니다.
`ILoadingOperationRunner`를 구현하고 `SceneManager`를 통해 Unity scene loading을 처리합니다.

지원 operation:

- none
- simple loading operation
- single scene loading
- additive scene loading

### Bootstrapper Integration

`LoadingPackageBootstrapperSo`은 loading module을 Bootstrapper system에 설치합니다.

`LoadingBehaviour`를 생성하거나 찾고, `LoadingService`를 만든 뒤 `ILoadingService`로 등록합니다.

## LoadingRequest

- `operationType`: `None`, `Simple`, `LoadScene`, `LoadAdditiveScene`
- `sceneName`: scene loading operation에 필요
- `message`: loading event로 전달되는 메시지
- `minimumDuration`: 최소 loading 시간, 초 단위
- `waitForManualCompletion`: `MarkAsReadyToComplete()` 호출 전까지 대기
- `tasks`: main loading operation 전에 실행할 custom task

## 기본 사용법

application context에서 `ILoadingService`를 resolve한 뒤 loading request를 실행합니다.

```csharp
var request = new LoadingRequest(
    operationType: LoadingOperationType.LoadScene,
    sceneName: "GameScene",
    message: "Loading...",
    minimumDuration: 1.0f);

var result = await loadingService.RunAsync(request, cancellationToken);

if (result.IsFailure)
{
    // result.Error를 확인합니다.
    return;
}
```

`RunAsync`는 `Result`를 반환합니다. 구체적인 loading 실패는 `LoadingErrors`로 설명됩니다.

반환된 `Result`가 command outcome의 기준입니다. Loading event는 UI, logging, presentation code를 위한 lifecycle notification입니다.

Completed/Failed 구독자 예외는 나머지 종료 구독자에게도 알림을 보낸 뒤 반환 task에 전파됩니다. Started/Updated 구독자 예외는 OperationFailed Result가 됩니다. 호출자는 반환 task를 관찰하고 UI 구독자는 자기 표현 오류를 처리해야 합니다.

## Loading Event

`ILoadingService`는 UI나 presentation code가 사용할 수 있는 event를 노출합니다.

```csharp
loadingService.LoadingStarted += OnLoadingStarted;
loadingService.LoadingUpdated += OnLoadingUpdated;
loadingService.LoadingCompleted += OnLoadingCompleted;
loadingService.LoadingFailed += OnLoadingFailed;
```

loading module은 loading UI를 직접 제공하지 않습니다.
UI는 이 event를 구독하고 독립적으로 progress를 렌더링해야 합니다.

loading이 실패하면 `LoadingFailedEventArgs.Error`에는 `RunAsync`가 반환한 것과 같은 error가 들어 있습니다.

event subscriber는 자기 예외를 스스로 처리해야 합니다. `LoadingService`는 lifecycle event를 발행할 뿐이며, Unity presenter, view, adapter는 view update 실패를 자기 layer에서 catch하고 log해야 합니다.

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

## Custom Loading Task

추가 loading 작업은 `ILoadingTask`를 구현해서 넣을 수 있습니다.

```csharp
public sealed class LoadPlayerDataTask : ILoadingTask
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        // 데이터를 로드합니다.
        await Task.CompletedTask;
    }
}
```

task는 main loading operation 전에 실행됩니다.

## Manual Completion

`WaitForManualCompletion`이 켜져 있으면 loading flow는 service가 완료 준비 상태로 표시될 때까지 기다립니다.

```csharp
loadingService.MarkAsReadyToComplete();
```

loading operation은 끝났지만 다른 시스템 준비가 더 필요할 때 유용합니다.

Ready는 활성 Run 시작부터 기억하고 실행이 없을 때는 무시합니다. 최소 시간은 전체 성공 흐름의 단조 증가 실제 시간으로 측정하며 timeScale과 무관합니다. 두 조건은 Unity scene activation을 늦추지 않습니다.

이미 시작된 씬 작업은 취소해도 씬을 바꿀 수 있습니다. 활성화 잠금을 풀고 engine 작업이 끝난 후 Cancelled를 반환하며 자동 rollback/unload는 제공하지 않습니다. Unity 호출·Ready·progress·Shutdown은 main thread의 직렬 문맥에서 사용합니다. 구체 LoadingService 소유자는 Dispose로 새 요청을 닫고 취소를 요청하며, ShutdownAsync로 정리 완료를 기다릴 수 있습니다.
