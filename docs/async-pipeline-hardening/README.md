# Async pipeline fixes: reviewer guide

This change fixes four background-processing failures: an unfinished TaskChain after a synchronous delegate throw, online lookup waiters left pending after a failed request, import cancellation misclassified as timeout, and concurrent refreshes corrupting the local metadata cache. The associated task-chain microbenchmark also allocates less memory.

**AI disclosure:** implementation, documentation, and review used OpenAI Codex and AI subagents. This is not a claim of human-only authorship. The upstream [AI contribution policy](../../CONTRIBUTING.md#foreword-on-ai-usage) is acknowledged; acceptance or maintainer endorsement is not claimed.

Base commit: [`bb3e5591908d2525f544d7e8968f84f6f248ec1b`](https://github.com/ppy/osu/commit/bb3e5591908d2525f544d7e8968f84f6f248ec1b). The C# checkout bytes are identical to the build and tests reported below; canonical Git blobs use LF according to .gitattributes, while tested checkout files use CRLF. Both hashes are recorded. [Source hashes](validation/source-sha256.json). [Every changed line interval, with original/new links](line-map.md).

## 1. Sequential tasks

`osu.Game/Extensions/TaskExtensions.cs`, new lines **20–24 and 33–48**. `ContinueWith(async ...).Unwrap()` on `TaskScheduler.Default` replaces the manually completed proxy and nested continuation. A synchronous throw from `Func<Task>` now faults the returned task instead of stranding subsequent work. The Action overload no longer dispatches an extra `Task.Run`. Cancellation cannot let a successor overtake a still-running operation.

Tests: `osu.Game.Tests/NonVisual/TaskExtensionsTest.cs`, **1–222** (16 cases). Benchmark: `osu.Game.Benchmarks/BenchmarkTaskChain.cs`, **1–38**. Baseline reproduction: the Func case times out after 10 seconds; the Action case instead fails on exception shape and does not hang.

Compatibility: normal await exception semantics replace nested AggregateException; a synchronous Action OperationCanceledException yields Canceled. A faulted child WhenAll contributes the await-selected exception, not its complete aggregate. The explicit scheduler changes custom-scheduler behavior. The inspected MultiplayerClient callers await tasks and do not inspect aggregate errors.

## 2. Online lookup completion and cancellation

`osu.Game/Database/OnlineLookupCache.cs`, new lines **69–75, 85, and 99–167**. Pre-cancellation is checked before enqueueing; WaitAsync cancels a caller's wait without cancelling the shared request. TCS uses RunContinuationsAsynchronously. Request creation, API execution, result retrieval, and enumeration are enclosed by cleanup which completes unresolved waiters with null and advances the queue.

Tests: `osu.Game.Tests/Database/OnlineLookupCacheTest.cs`, **1–217**. Includes deterministic shared-batch cancellation isolation, five failure stages, subsequent queued work, and 50-ID batch behavior. The baseline leaves waiters pending in the failure tests.

Existing null caching and the lack of retry/backpressure remain. IDs may still repeat across adjacent batches before the base cache publishes a result; this change does not claim to eliminate that race.

## 3. Import cancellation

`osu.Game/Database/RealmArchiveModelImporter.cs`, new lines **121–125 and 147–154**. The initial pause now lies inside try/finally. Batch cancellation is identified from the loop token state because Parallel.ForEachAsync supplies an internal token. An independently cancelled item remains an internal timeout and does not abort the batch.

Tests: `osu.Game.Tests/Database/TestSceneBeatmapImporterCancellation.cs`, **1–160**. Baseline reproduction: a pre-cancelled paused import leaves its notification Queued, and a user-cancelled active import is logged as timeout.

## 4. Local metadata cache publication

`osu.Game/Beatmaps/LocalCachedBeatmapMetadataSource.cs`, principal new ranges **31–74, 92–113, 162–186, 196–311, 314–347, and 495–522**. The exact insertion/deletion intervals are in the line map.

Multiple source instances share one in-flight refresh and read/write lock per normalized absolute path. Each refresh downloads to its own archive, fully decompresses to a staging file in the destination directory, validates supported schema, required columns, and SQLite quick_check, then publishes by replacing the final file. Readers hold the shared read lock through connection disposal. Publication clears only this database's pool after readers have left. Failed preparation preserves the previous published file and permits retry. ReadOnly connections avoid creating missing databases.

Storage paths are resolved at the beginning of each operation so migration between operations works. Windows case aliases share one representative connection-string path and pool. Disposing one source does not abort another source's shared request.

Tests: `osu.Game.Tests/Beatmaps/LocalCachedBeatmapMetadataSourceTest.cs`, **1–500** (19 pass on Linux; one Windows-specific case not executed). Real SQLite/BZip2 fixtures cover both schemas, lookup values, coalescing, retries, old-cache preservation, corrupt-cache recovery, migration, and lifetime isolation.

Runtime reproduction on the unmodified base: two successful metadata HTTP log entries, BZip2 EOF, then missing online.db.bz2. Fixed fresh startup: one successful download entry, one successful publication, no cache failure; schema 3, 238223 beatmap rows, 89870336 bytes, quick_check=ok, no leftover temporary files. Logs establish the symptoms; the inferred interleaving is supported by source analysis and controlled tests. [Runtime evidence](validation/runtime.json).

Boundaries: coordination is process-local and does not resolve symlink aliases. Migration concurrent with an already-started refresh, cross-process replacement, and power-loss durability are not coordinated. The registry lives for the process lifetime. quick_check is not a full index/uniqueness verification. Windows runtime was not tested.

## Verification

- Debug desktop solution: **0 warnings, 0 errors**, with warnings-as-errors and code-style enforcement. [Build log](validation/build-metadata.log).
- **SingleThread: 540 Passed, 1 Windows NotExecuted. MultiThreaded: 540 Passed, 1 Windows NotExecuted.** [Per-test outcomes](validation/tests.json). The TRX aggregate incorrectly says notExecuted=0; the individual Windows result is NotExecuted and is counted here.
- CodeFileSanity: exit 0 after the final source changes; git diff --check passes.
- InspectCode 2026.2.1: **incomplete**, its Roslyn host cannot load System.Composition.AttributedModel 10.0.0.12. Compiler checks do not substitute for full InspectCode.
- Actual Debug Desktop: Wayland/OpenGL, GTX 1650, MultiThreaded. Imported repository Renatus fixture with audio; autoplay reached SoloResultsScreen at 15:30:38 UTC, SS / 100% / 804 combo / 0 miss. [Gameplay frame](validation/current-autoplay.png), [results](validation/current-results.png).

Run from the repository root with .NET SDK 10.0.112:

```sh
dotnet restore osu.Desktop.slnf
dotnet build osu.Desktop.slnf -c Debug --no-restore -m:2 -warnaserror -p:EnforceCodeStyleInBuild=true
OSU_EXECUTION_MODE=SingleThread dotnet test osu.Game.Tests -c Debug --no-build --no-restore --filter 'FullyQualifiedName~osu.Game.Tests.NonVisual|FullyQualifiedName~OnlineLookupCacheTest|FullyQualifiedName~TestSceneBeatmapImporterCancellation|FullyQualifiedName~StatefulMultiplayerClientTest|FullyQualifiedName~TestSceneBeatmapDifficultyCache|FullyQualifiedName~LocalCachedBeatmapMetadataSourceTest|FullyQualifiedName~BeatmapUpdaterMetadataLookupTest|FullyQualifiedName~BackgroundDataStoreProcessorTests' -- NUnit.ConsoleOut=0
OSU_EXECUTION_MODE=MultiThreaded dotnet test osu.Game.Tests -c Debug --no-build --no-restore --filter 'FullyQualifiedName~osu.Game.Tests.NonVisual|FullyQualifiedName~OnlineLookupCacheTest|FullyQualifiedName~TestSceneBeatmapImporterCancellation|FullyQualifiedName~StatefulMultiplayerClientTest|FullyQualifiedName~TestSceneBeatmapDifficultyCache|FullyQualifiedName~LocalCachedBeatmapMetadataSourceTest|FullyQualifiedName~BeatmapUpdaterMetadataLookupTest|FullyQualifiedName~BackgroundDataStoreProcessorTests' -- NUnit.ConsoleOut=0
DOTNET_ROLL_FORWARD=Major dotnet CodeFileSanity
dotnet run --project osu.Desktop -c Debug --no-build
```

## Microbenchmark evidence

BenchmarkDotNet 0.15.8; SDK 10.0.112/runtime 10.0.12; Ryzen 5 3550H, 4 cores / 8 threads. Actual baseline/current TaskChain and TaskExtensions sources linked into identical harnesses. Each invocation builds and awaits 1000 links; OperationsPerInvoke=1000. Sequential runs: one launch, three warmups, five measured iterations per method.

| Per link | Base mean ± BDN error | Changed mean ± BDN error | Managed allocation |
| --- | --- | --- | --- |
| Action | 1432.2 ± 56.06 ns | 496.2 ± 26.74 ns | 656 → 376 B |
| Completed async function | 921.0 ± 80.11 ns | 506.3 ± 12.75 ns | 496 → 288 B |

Ratios of means: 2.89× and 1.82×; allocation reductions: 42.7% and 41.9%. All four cases have five raw measurements; BDN removes one upper outlier in baseline async (retained N=4 there). Shared interactive laptop, short run, clocks varied. These results do not measure FPS, frame times, input latency, network latency, or real multiplayer join/leave duration. [Raw comparison](validation/benchmarks.json), [baseline report](validation/benchmark-baseline.md), [changed report](validation/benchmark-current.md).

No scoring/pp/star-rating formulas or hit object calculations were changed. Full visual and cross-platform CI were not run.
