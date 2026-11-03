---
status: accepted
---

# Preserve the Run until its engine operation has drained

Unity scene loading cannot be rolled back through a cancellation token. Returning immediately after cancellation while scene activation is locked can stall Unity's operation queue and allow an overlapping Run. Loading therefore releases the activation lock and awaits the started engine operation before returning cancellation. It does not unload or restore scenes automatically, because those policies belong to the consumer. The managed cleanup survives adapter destruction while Unity's loop remains alive; application exit and leaving Play Mode end that guarantee.

Manual readiness is remembered for the active Run and controls the flow's completion, not scene activation. Terminal observers all receive their one terminal notification; their first exception propagates after notification without emitting a second failure event. Ordinary operational failure remains a Result.

Synchronous bootstrap shutdown requests cancellation and releases only matching registrations. Owners that need a drain boundary use concrete LoadingService.ShutdownAsync; the existing ILoadingService implementation contract remains unchanged.
