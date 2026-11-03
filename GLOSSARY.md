# Loading

Loading coordinates preparation work and an optional scene operation before reporting that a request is ready for its consumer.

## Language

**Run**: One accepted loading request, including preparation tasks, its operation and completion conditions.

**Manual readiness**: The consumer's permission to finish the current Run. It is separate from scene activation.

**Minimum duration**: The minimum real elapsed time for a successful Run, including its preparation work and operation.

**Loading cancellation**: A request to stop the Run's remaining work. A scene operation that has already started may still change the loaded scenes before the Run ends.
