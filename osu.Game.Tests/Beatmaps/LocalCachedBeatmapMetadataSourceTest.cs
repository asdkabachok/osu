// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.IO;
using osu.Game.Models;
using SharpCompress.Compressors.BZip2;
using SQLitePCL;
using CompressionMode = SharpCompress.Compressors.CompressionMode;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class LocalCachedBeatmapMetadataSourceTest
    {
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(10);
        private readonly List<TemporaryNativeStorage> storages = new List<TemporaryNativeStorage>();

        [SetUp]
        public void SetUp() => Batteries_V2.Init();

        [TearDown]
        public void TearDown()
        {
            foreach (var storage in storages)
            {
                clearCachePool(storage.GetFullPath("online.db"));
                storage.Dispose();
            }

            storages.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task TestConcurrentFetchesShareOperationAndPreservePublishedCache(bool sameInstance)
        {
            var storage = createStorage();
            byte[] original = createDatabase(2);
            byte[] replacement = compress(createDatabase(3));
            await File.WriteAllBytesAsync(storage.GetFullPath("online.db"), original);

            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int downloads = 0;

            async Task download(string path)
            {
                Interlocked.Increment(ref downloads);
                await File.WriteAllBytesAsync(path, replacement.AsMemory(0, replacement.Length / 2).ToArray());
                started.TrySetResult();
                await complete.Task;
                await File.WriteAllBytesAsync(path, replacement);
            }

            using var first = new LocalCachedBeatmapMetadataSource(storage, download);
            using var second = sameInstance ? first : new LocalCachedBeatmapMetadataSource(new NativeStorage(storage.GetFullPath(".")), download);

            // Populate the old database's pool before replacing the file.
            Assert.That(first.IsAtLeastVersion(2), Is.True);
            Task firstFetch = first.FetchCache();

            try
            {
                await started.Task.WaitAsync(timeout);
                Task secondFetch = second.FetchCache();

                Assert.That(secondFetch, Is.SameAs(firstFetch));
                Assert.That(first.Available, Is.False);
                Assert.That(second.Available, Is.False);
                Assert.That(await File.ReadAllBytesAsync(storage.GetFullPath("online.db")), Is.EqualTo(original));
            }
            finally
            {
                complete.TrySetResult();
                await firstFetch.WaitAsync(timeout);
            }

            Assert.That(downloads, Is.EqualTo(1));
            Assert.That(first.Available, Is.True);
            Assert.That(second.IsAtLeastVersion(3), Is.True);
            assertNoTemporaryFiles(storage);
        }

        [Test]
        public async Task TestConstructorFetchesJoinAndFreshCacheIsNotFetchedAgain()
        {
            var storage = createStorage();
            byte[] replacement = compress(createDatabase(3));
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int downloads = 0;

            async Task download(string path)
            {
                Interlocked.Increment(ref downloads);
                started.TrySetResult();
                await complete.Task;
                await File.WriteAllBytesAsync(path, replacement);
            }

            using var first = new LocalCachedBeatmapMetadataSource(storage, download, fetchOnConstruction: true);
            await started.Task.WaitAsync(timeout);
            Task fetch = first.FetchCache();

            try
            {
                using var second = new LocalCachedBeatmapMetadataSource(storage, download, fetchOnConstruction: true);
                Assert.That(second.FetchCache(), Is.SameAs(fetch));
                Assert.That(storage.Exists("online.db"), Is.False);
            }
            finally
            {
                complete.TrySetResult();
                await fetch.WaitAsync(timeout);
            }

            using var third = new LocalCachedBeatmapMetadataSource(storage, download, fetchOnConstruction: true);
            Assert.That(third.Available, Is.True);
            Assert.That(downloads, Is.EqualTo(1));
            assertNoTemporaryFiles(storage);
        }

        [TestCase(2)]
        [TestCase(3)]
        public async Task TestPublishedCacheCanServeLookup(int version)
        {
            var storage = createStorage();
            byte[] replacement = compress(createDatabase(version));
            using var source = new LocalCachedBeatmapMetadataSource(storage, path => File.WriteAllBytesAsync(path, replacement));

            await source.FetchCache().WaitAsync(timeout);

            var beatmap = createBeatmap();
            Assert.That(source.TryLookup(beatmap, out var metadata), Is.True);
            Assert.That(metadata!.BeatmapID, Is.EqualTo(123));
            Assert.That(metadata.BeatmapSetID, Is.EqualTo(456));
            Assert.That(metadata.UserTags, Is.EqualTo(version == 3 ? new[] { "test-tag" } : Array.Empty<string>()));
        }

        [Test]
        public async Task TestCorruptedPublishedCacheStartsOneSharedRecovery()
        {
            var storage = createStorage();
            byte[] corrupted = Encoding.UTF8.GetBytes("Corrupted published cache.");
            byte[] replacement = compress(createDatabase(3));
            await File.WriteAllBytesAsync(storage.GetFullPath("online.db"), corrupted);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int downloads = 0;

            async Task download(string path)
            {
                Interlocked.Increment(ref downloads);
                started.TrySetResult();
                await complete.Task;
                await File.WriteAllBytesAsync(path, replacement);
            }

            using var first = new LocalCachedBeatmapMetadataSource(storage, download);
            using var second = new LocalCachedBeatmapMetadataSource(storage, download);
            var beatmap = createBeatmap();

            Assert.That(first.TryLookup(beatmap, out _), Is.False);
            Task fetch = first.FetchCache();

            try
            {
                await started.Task.WaitAsync(timeout);
                Assert.That(second.TryLookup(beatmap, out _), Is.False);
                Assert.That(second.FetchCache(), Is.SameAs(fetch));
                Assert.That(await File.ReadAllBytesAsync(storage.GetFullPath("online.db")), Is.EqualTo(corrupted));
            }
            finally
            {
                complete.TrySetResult();
                await fetch.WaitAsync(timeout);
            }

            Assert.That(downloads, Is.EqualTo(1));
            Assert.That(second.TryLookup(beatmap, out var metadata), Is.True);
            Assert.That(metadata!.BeatmapID, Is.EqualTo(123));
            assertNoTemporaryFiles(storage);
        }

        [Test]
        [Platform("Win")]
        public async Task TestWindowsPathCaseAliasesShareDatabasePool()
        {
            var storage = createStorage();
            await File.WriteAllBytesAsync(storage.GetFullPath("online.db"), createDatabase(2));
            byte[] replacement = compress(createDatabase(3));

            using var first = new LocalCachedBeatmapMetadataSource(storage, path => File.WriteAllBytesAsync(path, replacement));
            using var alias = new LocalCachedBeatmapMetadataSource(new NativeStorage(storage.GetFullPath(".").ToUpperInvariant()), path => File.WriteAllBytesAsync(path, replacement));

            Assert.That(first.IsAtLeastVersion(2), Is.True);
            await alias.FetchCache().WaitAsync(timeout);

            Assert.That(first.IsAtLeastVersion(3), Is.True);
            Assert.That(alias.IsAtLeastVersion(3), Is.True);
            assertNoTemporaryFiles(storage);
        }

        [TestCase("transport")]
        [TestCase("synchronous-transport")]
        [TestCase("truncated")]
        [TestCase("not-sqlite")]
        [TestCase("schema-only")]
        [TestCase("unsupported-version")]
        [TestCase("missing-tags")]
        public async Task TestFailedRefreshPreservesOldCacheAndCanRetry(string failure)
        {
            var storage = createStorage();
            byte[] original = createDatabase(2);
            byte[] replacement = compress(createDatabase(3));
            await File.WriteAllBytesAsync(storage.GetFullPath("online.db"), original);
            int downloads = 0;

            Task download(string path)
            {
                if (Interlocked.Increment(ref downloads) > 1)
                    return File.WriteAllBytesAsync(path, replacement);

                switch (failure)
                {
                    case "transport":
                        return writePartialDownload(path);

                    case "synchronous-transport":
                        throw new IOException("Download failed synchronously.");

                    case "truncated":
                        return File.WriteAllBytesAsync(path, replacement.AsMemory(0, replacement.Length / 2).ToArray());

                    case "not-sqlite":
                        return File.WriteAllBytesAsync(path, compress(Encoding.UTF8.GetBytes("Not a SQLite database.")));

                    case "schema-only":
                        return File.WriteAllBytesAsync(path, compress(createDatabase(3, schemaOnly: true)));

                    case "unsupported-version":
                        return File.WriteAllBytesAsync(path, compress(createDatabase(4)));

                    case "missing-tags":
                        return File.WriteAllBytesAsync(path, compress(createDatabase(3, includeTags: false)));

                    default:
                        throw new ArgumentOutOfRangeException(nameof(failure));
                }
            }

            using var source = new LocalCachedBeatmapMetadataSource(storage, download);
            Assert.That(source.IsAtLeastVersion(2), Is.True);

            await source.FetchCache().WaitAsync(timeout);

            Assert.That(source.Available, Is.True);
            Assert.That(source.IsAtLeastVersion(3), Is.False);
            Assert.That(await File.ReadAllBytesAsync(storage.GetFullPath("online.db")), Is.EqualTo(original));
            assertNoTemporaryFiles(storage);

            await source.FetchCache().WaitAsync(timeout);

            Assert.That(downloads, Is.EqualTo(2));
            Assert.That(source.IsAtLeastVersion(3), Is.True);
            assertNoTemporaryFiles(storage);

            async Task writePartialDownload(string path)
            {
                await File.WriteAllBytesAsync(path, replacement.AsMemory(0, replacement.Length / 2).ToArray());
                throw new IOException("Connection interrupted during download.");
            }
        }

        [Test]
        public async Task TestFailedInitialDownloadCanRetry()
        {
            var storage = createStorage();
            byte[] replacement = compress(createDatabase(3));
            int downloads = 0;

            using var source = new LocalCachedBeatmapMetadataSource(storage, path =>
                ++downloads == 1 ? Task.FromException(new IOException("Download failed.")) : File.WriteAllBytesAsync(path, replacement));

            await source.FetchCache().WaitAsync(timeout);

            Assert.That(source.Available, Is.False);
            Assert.That(storage.Exists("online.db"), Is.False);
            assertNoTemporaryFiles(storage);

            await source.FetchCache().WaitAsync(timeout);

            Assert.That(source.IsAtLeastVersion(3), Is.True);
            Assert.That(downloads, Is.EqualTo(2));
        }

        [Test]
        public async Task TestDifferentPathsMakeIndependentProgress()
        {
            var firstStorage = createStorage();
            var secondStorage = createStorage();
            byte[] replacement = compress(createDatabase(3));
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            using var first = new LocalCachedBeatmapMetadataSource(firstStorage, async path =>
            {
                started.SetResult();
                await complete.Task;
                await File.WriteAllBytesAsync(path, replacement);
            });
            using var second = new LocalCachedBeatmapMetadataSource(secondStorage, path => File.WriteAllBytesAsync(path, replacement));

            Task firstFetch = first.FetchCache();

            try
            {
                await started.Task.WaitAsync(timeout);
                await second.FetchCache().WaitAsync(timeout);

                Assert.That(second.Available, Is.True);
                Assert.That(firstFetch.IsCompleted, Is.False);
                Assert.That(firstStorage.Exists("online.db"), Is.False);
            }
            finally
            {
                complete.TrySetResult();
                await firstFetch.WaitAsync(timeout);
            }
        }

        [Test]
        public async Task TestDisposingOneSourceDoesNotAbortSharedFetch()
        {
            var storage = createStorage();
            byte[] replacement = compress(createDatabase(3));
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            async Task download(string path)
            {
                started.SetResult();
                await complete.Task;
                await File.WriteAllBytesAsync(path, replacement);
            }

            using var first = new LocalCachedBeatmapMetadataSource(storage, download);
            using var second = new LocalCachedBeatmapMetadataSource(storage, download);
            Task fetch = first.FetchCache();

            try
            {
                await started.Task.WaitAsync(timeout);
                Assert.That(second.FetchCache(), Is.SameAs(fetch));
                first.Dispose();
            }
            finally
            {
                complete.TrySetResult();
                await fetch.WaitAsync(timeout);
            }

            Assert.That(second.IsAtLeastVersion(3), Is.True);
        }

        [Test]
        public async Task TestSourceFollowsStorageMigration()
        {
            var originalStorage = createStorage();
            var newStorage = createStorage();
            var migratableStorage = new TestMigratableStorage(originalStorage);
            byte[] replacement = compress(createDatabase(3));
            await File.WriteAllBytesAsync(originalStorage.GetFullPath("online.db"), createDatabase(2));

            using var source = new LocalCachedBeatmapMetadataSource(migratableStorage, path => File.WriteAllBytesAsync(path, replacement));
            Assert.That(source.IsAtLeastVersion(2), Is.True);
            clearCachePool(originalStorage.GetFullPath("online.db"));

            Assert.That(migratableStorage.Migrate(newStorage), Is.True);
            Assert.That(source.Available, Is.True);
            Assert.That(source.IsAtLeastVersion(2), Is.True);

            await source.FetchCache().WaitAsync(timeout);

            Assert.That(source.IsAtLeastVersion(3), Is.True);
            Assert.That(originalStorage.Exists("online.db"), Is.False);
            Assert.That(newStorage.Exists("online.db"), Is.True);
            assertNoTemporaryFiles(newStorage);
        }

        [Test]
        public void TestMissingCacheVersionCheckDoesNotCreateDatabase()
        {
            var storage = createStorage();
            using var source = new LocalCachedBeatmapMetadataSource(storage, _ => throw new InvalidOperationException("Unexpected download."));

            Assert.That(source.IsAtLeastVersion(3), Is.False);
            Assert.That(storage.Exists("online.db"), Is.False);
        }

        [Test]
        public async Task TestPublicSourceDoesNotDownloadInUnitTests()
        {
            var storage = createStorage();
            using var source = new LocalCachedBeatmapMetadataSource(storage);

            await source.FetchCache().WaitAsync(timeout);

            Assert.That(source.Available, Is.False);
            Assert.That(storage.Exists("online.db"), Is.False);
            assertNoTemporaryFiles(storage);
        }

        private TemporaryNativeStorage createStorage()
        {
            var storage = new TemporaryNativeStorage($@"metadata-cache-{Guid.NewGuid():N}");
            storages.Add(storage);
            return storage;
        }

        private static BeatmapInfo createBeatmap()
        {
            var beatmap = new BeatmapInfo { MD5Hash = "test-hash", Hash = "test-file-hash" };
            beatmap.BeatmapSet = new BeatmapSetInfo(new[] { beatmap });
            beatmap.BeatmapSet.Files.Add(new RealmNamedFileUsage(new RealmFile { Hash = beatmap.Hash }, "test.osu"));
            return beatmap;
        }

        private byte[] createDatabase(int version, bool schemaOnly = false, bool includeTags = true)
        {
            var storage = createStorage();
            string path = storage.GetFullPath("fixture.db");

            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = $@"CREATE TABLE schema_version (number INTEGER); INSERT INTO schema_version VALUES ({version});";
                command.ExecuteNonQuery();

                if (!schemaOnly)
                {
                    command.CommandText =
                        """
                        CREATE TABLE osu_beatmaps (beatmapset_id INTEGER, beatmap_id INTEGER, approved INTEGER, user_id INTEGER, checksum TEXT, filename TEXT, last_update TEXT);
                        CREATE TABLE osu_beatmapsets (beatmapset_id INTEGER, submit_date TEXT, approved_date TEXT);
                        INSERT INTO osu_beatmaps VALUES (456, 123, 1, 2, 'test-hash', 'test.osu', '2026-01-01 00:00:00+00:00');
                        INSERT INTO osu_beatmapsets VALUES (456, '2025-01-01 00:00:00+00:00', '2025-02-01 00:00:00+00:00');
                        """;
                    command.ExecuteNonQuery();

                    if (includeTags)
                    {
                        command.CommandText = "CREATE TABLE tags (id INTEGER, name TEXT); CREATE TABLE beatmap_tags (beatmap_id INTEGER, tag_id INTEGER); INSERT INTO tags VALUES (1, 'test-tag'); INSERT INTO beatmap_tags VALUES (123, 1);";
                        command.ExecuteNonQuery();
                    }
                }
            }

            return File.ReadAllBytes(path);
        }

        private static byte[] compress(byte[] contents)
        {
            using var output = new MemoryStream();
            using (var bz2 = BZip2Stream.Create(output, CompressionMode.Compress, decompressConcatenated: false, leaveOpen: true))
                bz2.Write(contents);

            return output.ToArray();
        }

        private static void assertNoTemporaryFiles(Storage storage) =>
            Assert.That(Directory.GetFiles(storage.GetFullPath("."), "*.tmp*"), Is.Empty);

        private static void clearCachePool(string path)
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = true }.ToString());
            SqliteConnection.ClearPool(connection);
        }

        private sealed class TestMigratableStorage : MigratableStorage
        {
            public TestMigratableStorage(Storage storage)
                : base(storage, string.Empty)
            {
            }
        }
    }
}
