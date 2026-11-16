// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using osu.Framework.Development;
using osu.Framework.IO.Network;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Database;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
using SQLitePCL;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Performs online metadata lookups using a copy of a database containing metadata for a large subset of beatmaps (stored to <see cref="cache_database_name"/>).
    /// The database will be asynchronously downloaded - if not already present locally - when this component is constructed.
    /// </summary>
    public class LocalCachedBeatmapMetadataSource : IOnlineBeatmapMetadataSource
    {
        private readonly Storage storage;

        // Keep one state for each path for the lifetime of the process. Removing completed entries could
        // give a new source a different database lock while an existing source still uses the old one.
        private static readonly ConcurrentDictionary<string, CacheState> cache_states = new ConcurrentDictionary<string, CacheState>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        private readonly Func<string, Task> downloadCache;
        private readonly bool allowFetch;

        private const string cache_database_name = @"online.db";

        public LocalCachedBeatmapMetadataSource(Storage storage)
            : this(storage, downloadCacheFile, !DebugUtils.IsNUnitRunning, true)
        {
        }

        // An injected downloader allows tests to exercise the complete refresh without internet access.
        internal LocalCachedBeatmapMetadataSource(Storage storage, Func<string, Task> downloadCache, bool fetchOnConstruction = false)
            : this(storage, downloadCache, true, fetchOnConstruction)
        {
        }

        private LocalCachedBeatmapMetadataSource(Storage storage, Func<string, Task> downloadCache, bool allowFetch, bool fetchOnConstruction)
        {
            try
            {
                // required to initialise native SQLite libraries on some platforms.
                Batteries_V2.Init();
                raw.sqlite3_config(2 /*SQLITE_CONFIG_MULTITHREAD*/);
            }
            catch
            {
                // may fail if platform not supported.
            }

            this.storage = storage;
            this.downloadCache = downloadCache;
            this.allowFetch = allowFetch;
            if (fetchOnConstruction)
                fetchCache(onlyIfNeeded: true);
        }

        private static bool shouldFetchCache(CacheContext context)
        {
            if (!File.Exists(context.FilePath))
            {
                log(@"Fetching local cache because it does not exist.");
                return true;
            }

            // periodically update the cache to include newer beatmaps.
            var fileInfo = new FileInfo(context.FilePath);

            if (fileInfo.LastWriteTime < DateTime.Now.AddMonths(-1))
            {
                log($@"Refetching local cache because it was last written to on {fileInfo.LastWriteTime}.");
                return true;
            }

            return false;
        }

        public bool Available => isAvailable(getCacheContext());

        private static bool isAvailable(CacheContext context)
        {
            lock (context.State.FetchLock)
                return context.State.FetchTask?.IsCompleted != false && File.Exists(context.FilePath);
        }

        public bool TryLookup(BeatmapInfo beatmapInfo, [NotNullWhen(true)] out OnlineBeatmapMetadata? onlineMetadata)
        {
            var context = getCacheContext();
            context.State.DatabaseLock.EnterReadLock();

            try
            {
                return tryLookup(context, beatmapInfo, out onlineMetadata);
            }
            finally
            {
                context.State.DatabaseLock.ExitReadLock();
            }
        }

        private bool tryLookup(CacheContext context, BeatmapInfo beatmapInfo, [NotNullWhen(true)] out OnlineBeatmapMetadata? onlineMetadata)
        {
            Debug.Assert(beatmapInfo.BeatmapSet != null);

            if (!isAvailable(context))
            {
                onlineMetadata = null;
                return false;
            }

            if (string.IsNullOrEmpty(beatmapInfo.MD5Hash)
                && string.IsNullOrEmpty(beatmapInfo.Path))
            {
                onlineMetadata = null;
                return false;
            }

            try
            {
                using (var db = createConnection(context.FilePath))
                {
                    db.Open();

                    switch (getCacheVersion(db))
                    {
                        case 2:
                            // can be removed 20260123
                            return queryCacheVersion2(db, beatmapInfo, out onlineMetadata);

                        case 3:
                            return queryCacheVersion3(db, beatmapInfo, out onlineMetadata);
                    }
                }

                onlineMetadata = null;
                return false;
            }
            catch (SqliteException sqliteException)
            {
                onlineMetadata = null;

                // There have been cases where the user's local database is corrupt.
                // Let's attempt to identify these cases and re-initialise the local cache.
                switch (sqliteException.SqliteErrorCode)
                {
                    case 26: // SQLITE_NOTADB
                    case 11: // SQLITE_CORRUPT
                        // A different source may already be refreshing this path. Join that operation
                        // without deleting a database which could have been replaced since the failed read.
                        fetchCache(context, onlyIfNeeded: false);
                        return false;
                }

                logForModel(beatmapInfo.BeatmapSet, $@"Cached local retrieval for {beatmapInfo} failed with unhandled sqlite error {sqliteException}.");
                return false;
            }
            catch (Exception ex)
            {
                logForModel(beatmapInfo.BeatmapSet, $@"Cached local retrieval for {beatmapInfo} failed with {ex}.");
                onlineMetadata = null;
                return false;
            }
        }

        private static SqliteConnection createConnection(string path, bool pooling = true) =>
            new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = pooling
            }.ToString());

        /// <summary>
        /// Force an immediate fetch of the latest cache database.
        /// </summary>
        public Task FetchCache() => fetchCache(onlyIfNeeded: false);

        private Task fetchCache(bool onlyIfNeeded) => fetchCache(getCacheContext(), onlyIfNeeded);

        private Task fetchCache(CacheContext context, bool onlyIfNeeded)
        {
            if (!allowFetch)
                return Task.CompletedTask;

            lock (context.State.FetchLock)
            {
                if (context.State.FetchTask?.IsCompleted == false)
                    return context.State.FetchTask;

                // Recheck freshness after acquiring the shared state, as another source may have
                // finished its constructor-initiated fetch before this source reached the lock.
                if (onlyIfNeeded && !shouldFetchCache(context))
                    return Task.CompletedTask;

                return context.State.FetchTask = Task.Run(() => fetchCacheAsync(context));
            }
        }

        private async Task fetchCacheAsync(CacheContext context)
        {
            string temporaryPath = $@"{context.FilePath}.{Guid.NewGuid():N}.tmp";
            string compressedCacheFilePath = $@"{temporaryPath}.bz2";

            try
            {
                await downloadCache(compressedCacheFilePath).ConfigureAwait(false);

                using (var stream = File.OpenRead(compressedCacheFilePath))
                using (var outStream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write))
                using (var bz2 = BZip2Stream.Create(stream, CompressionMode.Decompress, false))
                    bz2.CopyTo(outStream);

                validateCache(temporaryPath);

                context.State.DatabaseLock.EnterWriteLock();

                try
                {
                    // All readers have disposed their connections. Close only this database's idle
                    // connections before replacing it, including on platforms which prohibit replacing open files.
                    using (var connection = createConnection(context.FilePath))
                        SqliteConnection.ClearPool(connection);

                    // Both paths are in the same directory, so publication does not copy partially
                    // extracted bytes over the live database or delete the old file first.
                    File.Move(temporaryPath, context.FilePath, overwrite: true);
                }
                finally
                {
                    context.State.DatabaseLock.ExitWriteLock();
                }

                log(@"Local cache fetch completed successfully.");
            }
            catch (Exception ex)
            {
                // Leave any previously published cache intact when download, extraction or publication fails.
                log($@"Online cache refresh failed: {ex}");
            }
            finally
            {
                deleteTemporaryFile(temporaryPath);
                deleteTemporaryFile(compressedCacheFilePath);
            }
        }

        private static async Task downloadCacheFile(string filename)
        {
            using var request = new FileWebRequest(filename, $@"https://assets.ppy.sh/client-resources/{cache_database_name}.bz2?{DateTimeOffset.UtcNow:yyyyMMdd}");
            await request.PerformAsync().ConfigureAwait(false);
        }

        private static void validateCache(string filename)
        {
            // Do not keep a pooled connection to the staging filename alive after validation.
            using var connection = createConnection(filename, pooling: false);
            connection.Open();

            int version = getCacheVersion(connection);
            if (version != 2 && version != 3)
                throw new InvalidDataException($@"Unsupported local metadata cache version: {version}.");

            using var command = connection.CreateCommand();
            command.CommandText = @"PRAGMA quick_check(1)";
            if (command.ExecuteScalar() as string != "ok")
                throw new InvalidDataException("Local metadata cache failed SQLite quick_check.");

            // Check the columns used by lookups, even if the downloaded database contains no rows.
            command.CommandText =
                """
                SELECT b.beatmapset_id, b.beatmap_id, b.approved, b.user_id, b.checksum, b.filename, b.last_update, s.submit_date, s.approved_date
                FROM osu_beatmaps b JOIN osu_beatmapsets s ON s.beatmapset_id = b.beatmapset_id
                LIMIT 0
                """;
            using (command.ExecuteReader())
            {
            }

            if (version == 3)
            {
                command.CommandText = "SELECT t.name FROM tags t JOIN beatmap_tags bt ON bt.tag_id = t.id WHERE bt.beatmap_id = 0 LIMIT 0";
                using (command.ExecuteReader())
                {
                }
            }
        }

        private static void deleteTemporaryFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                log($@"Failed to remove temporary metadata cache file {path}: {ex}");
            }
        }

        public bool IsAtLeastVersion(int version)
        {
            var context = getCacheContext();
            context.State.DatabaseLock.EnterReadLock();

            try
            {
                return isAvailable(context) && isAtLeastVersion(context, version);
            }
            finally
            {
                context.State.DatabaseLock.ExitReadLock();
            }
        }

        private static bool isAtLeastVersion(CacheContext context, int version)
        {
            try
            {
                using (var connection = createConnection(context.FilePath))
                {
                    connection.Open();
                    return getCacheVersion(connection) >= version;
                }
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 26 || ex.SqliteErrorCode == 11) // SQLITE_NOTADB, SQLITE_CORRUPT
            {
                // if the database is corrupted then return `false` as the consumer may want to just refetch the db themselves
                return false;
            }
        }

        private static int getCacheVersion(SqliteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"SELECT COUNT(1) FROM `sqlite_master` WHERE `type` = 'table' AND `name` = 'schema_version'";

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    throw new InvalidOperationException("Error when attempting to check for existence of `schema_version` table.");

                // No versioning table means that this is the very first version of the schema.
                if (reader.GetInt32(0) == 0)
                    return 1;
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"SELECT `number` FROM `schema_version`";

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    throw new InvalidOperationException("Error when attempting to query schema version.");

                return reader.GetInt32(0);
            }
        }

        public DateTime? GetCacheFetchDate()
        {
            string path = getCacheContext().FilePath;
            var file = new FileInfo(path);
            if (!file.Exists)
                return null;

            return file.LastWriteTime;
        }

        private bool queryCacheVersion2(SqliteConnection db, BeatmapInfo beatmapInfo, out OnlineBeatmapMetadata? onlineMetadata)
        {
            Debug.Assert(beatmapInfo.BeatmapSet != null);

            using var cmd = db.CreateCommand();

            cmd.CommandText =
                """
                SELECT `b`.`beatmapset_id`, `b`.`beatmap_id`, `b`.`approved`, `b`.`user_id`, `b`.`checksum`, `b`.`last_update`, `s`.`submit_date`, `s`.`approved_date`
                FROM `osu_beatmaps` AS `b`
                JOIN `osu_beatmapsets` AS `s` ON `s`.`beatmapset_id` = `b`.`beatmapset_id`
                WHERE (`b`.`checksum` = @MD5Hash OR `b`.`filename` = @Path)
                AND `b`.`approved` in (1, 2, 4)
                """;
            // approved conditional can theoretically be removed as it was fixed in
            // https://github.com/ppy/osu-onlinedb-generator/commit/489ac000775c3ff63bc914efb83cad0f6fbde261
            // but it's also safe to leave it (should not affect performance).

            cmd.Parameters.Add(new SqliteParameter(@"@MD5Hash", beatmapInfo.MD5Hash));
            cmd.Parameters.Add(new SqliteParameter(@"@Path", beatmapInfo.Path));

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                logForModel(beatmapInfo.BeatmapSet, $@"Cached local retrieval for {beatmapInfo} (cache version 2).");

                onlineMetadata = new OnlineBeatmapMetadata
                {
                    BeatmapSetID = reader.GetInt32(0),
                    BeatmapID = reader.GetInt32(1),
                    BeatmapStatus = (BeatmapOnlineStatus)reader.GetByte(2),
                    BeatmapSetStatus = (BeatmapOnlineStatus)reader.GetByte(2),
                    AuthorID = reader.GetInt32(3),
                    MD5Hash = reader.GetString(4),
                    LastUpdated = reader.GetDateTimeOffset(5),
                    DateSubmitted = reader.GetDateTimeOffset(6),
                    DateRanked = reader.GetDateTimeOffset(7),
                };
                return true;
            }

            onlineMetadata = null;
            return false;
        }

        private bool queryCacheVersion3(SqliteConnection db, BeatmapInfo beatmapInfo, out OnlineBeatmapMetadata? onlineMetadata)
        {
            Debug.Assert(beatmapInfo.BeatmapSet != null);

            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText =
                    """
                    SELECT `b`.`beatmapset_id`, `b`.`beatmap_id`, `b`.`approved`, `b`.`user_id`, `b`.`checksum`, `b`.`last_update`, `s`.`submit_date`, `s`.`approved_date`
                    FROM `osu_beatmaps` AS `b`
                    JOIN `osu_beatmapsets` AS `s` ON `s`.`beatmapset_id` = `b`.`beatmapset_id`
                    WHERE (`b`.`checksum` = @MD5Hash OR `b`.`filename` = @Path)
                    """;

                cmd.Parameters.Add(new SqliteParameter(@"@MD5Hash", beatmapInfo.MD5Hash));
                cmd.Parameters.Add(new SqliteParameter(@"@Path", beatmapInfo.Path));

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        onlineMetadata = null;
                        return false;
                    }

                    logForModel(beatmapInfo.BeatmapSet, $@"Cached local retrieval for {beatmapInfo} (cache version 3).");

                    onlineMetadata = new OnlineBeatmapMetadata
                    {
                        BeatmapSetID = reader.GetInt32(0),
                        BeatmapID = reader.GetInt32(1),
                        BeatmapStatus = (BeatmapOnlineStatus)reader.GetByte(2),
                        BeatmapSetStatus = (BeatmapOnlineStatus)reader.GetByte(2),
                        AuthorID = reader.GetInt32(3),
                        MD5Hash = reader.GetString(4),
                        LastUpdated = reader.GetDateTimeOffset(5),
                        DateSubmitted = reader.GetDateTimeOffset(6),
                        DateRanked = reader.GetDateTimeOffset(7),
                    };
                }
            }

            using (var tagsCommand = db.CreateCommand())
            {
                tagsCommand.CommandText = "SELECT `name` FROM `tags` WHERE `id` IN (SELECT `tag_id` FROM `beatmap_tags` WHERE `beatmap_id` = @BeatmapID)";
                tagsCommand.Parameters.Add(new SqliteParameter(@"@BeatmapID", onlineMetadata.BeatmapID));

                using (var tagsReader = tagsCommand.ExecuteReader())
                {
                    while (tagsReader.Read())
                        onlineMetadata.UserTags.Add(tagsReader.GetString(0));
                }
            }

            return true;
        }

        private static void log(string message)
            => Logger.Log($@"[{nameof(LocalCachedBeatmapMetadataSource)}] {message}", LoggingTarget.Database);

        private void logForModel(BeatmapSetInfo set, string message) =>
            RealmArchiveModelImporter<BeatmapSetInfo>.LogForModel(set, $@"[{nameof(LocalCachedBeatmapMetadataSource)}] {message}");

        public void Dispose()
        {
            // A refresh is shared by all sources for this path and owns its request until completion.
        }

        private CacheContext getCacheContext()
        {
            // Storage can change its target during a live data migration. Each operation keeps a
            // consistent path and lock, while later operations follow the new storage target.
            string path = Path.GetFullPath(storage.GetFullPath(cache_database_name));
            var state = cache_states.GetOrAdd(path, static p => new CacheState(p));
            return new CacheContext(state.FilePath, state);
        }

        private readonly record struct CacheContext(string FilePath, CacheState State);

        private sealed class CacheState
        {
            // Share the spelling of the path as well as its lock, so case aliases on Windows
            // cannot create separate SQLite connection pools for the same database.
            public readonly string FilePath;
            public readonly Lock FetchLock = new Lock();
            public readonly ReaderWriterLockSlim DatabaseLock = new ReaderWriterLockSlim();
            public Task? FetchTask;

            public CacheState(string filePath)
            {
                FilePath = filePath;
            }
        }
    }
}
