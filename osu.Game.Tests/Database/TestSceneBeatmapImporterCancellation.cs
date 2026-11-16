// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.IO.Archives;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Tests.Visual;

namespace osu.Game.Tests.Database
{
    [HeadlessTest]
    public partial class TestSceneBeatmapImporterCancellation : OsuTestScene
    {
        private TestBeatmapImporter importer = null!;
        private ProgressNotification notification = null!;
        private Task<IEnumerable<Live<BeatmapSetInfo>>> importTask = null!;
        private TaskCompletionSource releaseImport = null!;
        private CancellationLog cancellationLog = null!;

        protected override bool UseFreshStoragePerRun => true;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create importer and notification", () =>
            {
                importer = new TestBeatmapImporter(LocalStorage, Realm);
                importTask = null!;
                releaseImport = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                cancellationLog = new CancellationLog();
                Logger.NewEntry += cancellationLog.OnNewLogEntry;

                Child = new DependencyProvidingContainer
                {
                    CachedDependencies = [(typeof(OverlayColourProvider), new OverlayColourProvider(OverlayColourScheme.Purple))],
                    Child = notification = new ProgressNotification()
                };
            });

            AddUntilStep("notification loaded", () => notification.IsLoaded);
        }

        [TearDown]
        public async Task TearDown()
        {
            var task = importTask;
            var log = cancellationLog;

            if (importer != null)
                importer.PauseImports = false;
            releaseImport?.TrySetResult();

            try
            {
                if (task != null)
                    await task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (task != null && task.IsCanceled)
            {
            }
            finally
            {
                if (log != null)
                    Logger.NewEntry -= log.OnNewLogEntry;
            }
        }

        [Test]
        public void TestCancellationBeforeImportWhilePaused()
        {
            AddStep("cancel notification", () => notification.State = ProgressNotificationState.Cancelled);
            AddUntilStep("cancellation requested", () => notification.CancellationToken.IsCancellationRequested);
            AddStep("start paused import", () =>
            {
                importer.PauseImports = true;
                importTask = importer.Import(notification, [createImportTask()]);
            });

            AddUntilStep("import finished", () => importTask.IsCompleted);
            AddAssert("import cancelled", () => importTask.IsCanceled);
            AddAssert("notification cancelled", () => notification.State, () => Is.EqualTo(ProgressNotificationState.Cancelled));
        }

        [Test]
        public void TestCancellationDuringImportIsNotLoggedAsTimeout()
        {
            var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);

            AddStep("start import", () =>
            {
                var release = releaseImport;
                var log = cancellationLog;
                importer.ImportAction = cancellation =>
                {
                    started.TrySetResult(cancellation);
                    if (!release.Task.Wait(TimeSpan.FromSeconds(10)))
                        throw new TimeoutException("Import was not released by the test.");
                    var exception = new OperationCanceledException(cancellation);
                    log.ExpectedException = exception;
                    throw exception;
                };

                importTask = importer.Import(notification, [createImportTask()]);
            });

            AddUntilStep("import started", () => started.Task.IsCompleted);
            AddAssert("loop uses its own token", () => started.Task.GetResultSafely() != notification.CancellationToken);
            AddStep("cancel import", () => notification.State = ProgressNotificationState.Cancelled);
            AddUntilStep("loop cancellation requested", () => started.Task.GetResultSafely().IsCancellationRequested);
            AddStep("release import", () => releaseImport.TrySetResult());
            AddUntilStep("import finished", () => importTask.IsCompleted);
            AddAssert("import cancelled", () => importTask.IsCanceled);
            AddAssert("cancellation not logged as failure", () => cancellationLog.Count, () => Is.Zero);
        }

        [Test]
        public void TestInternalCancellationDoesNotAbortBatch()
        {
            int attemptedImports = 0;

            AddStep("start import with internal cancellation", () =>
            {
                var log = cancellationLog;
                importer.ImportAction = _ =>
                {
                    if (Interlocked.Increment(ref attemptedImports) == 1)
                    {
                        var exception = new OperationCanceledException(new CancellationToken(true));
                        log.ExpectedException = exception;
                        throw exception;
                    }
                };

                importTask = importer.Import(notification, [createImportTask(), createImportTask()]);
            });

            AddUntilStep("import finished", () => importTask.IsCompleted);
            AddAssert("batch completed successfully", () => importTask.IsCompletedSuccessfully);
            AddAssert("both imports attempted", () => Volatile.Read(ref attemptedImports), () => Is.EqualTo(2));
            AddAssert("internal cancellation logged", () => cancellationLog.Count, () => Is.EqualTo(1));
        }

        private sealed class CancellationLog
        {
            public OperationCanceledException? ExpectedException;

            private int count;

            public int Count => Volatile.Read(ref count);

            public void OnNewLogEntry(LogEntry entry)
            {
                if (entry.Exception != null && ReferenceEquals(entry.Exception, ExpectedException))
                    Interlocked.Increment(ref count);
            }
        }

        private static ImportTask createImportTask() => new ImportTask(new MemoryStream([0]), "test.osu");

        private class TestBeatmapImporter : BeatmapImporter
        {
            public Action<CancellationToken>? ImportAction { get; set; }

            public TestBeatmapImporter(Storage storage, RealmAccess realm)
                : base(storage, realm)
            {
            }

            protected override BeatmapSetInfo CreateModel(ArchiveReader archive, ImportParameters parameters) => new BeatmapSetInfo();

            public override Live<BeatmapSetInfo>? ImportModel(BeatmapSetInfo item, ArchiveReader? archive = null, ImportParameters parameters = default,
                                                           CancellationToken cancellationToken = default)
            {
                ImportAction?.Invoke(cancellationToken);
                return null;
            }
        }
    }
}
