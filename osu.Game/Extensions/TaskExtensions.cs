// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace osu.Game.Extensions
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Add a continuation to be performed only after the attached task has completed.
        /// </summary>
        /// <param name="task">The previous task to be awaited on.</param>
        /// <param name="action">The action to run.</param>
        /// <param name="cancellationToken">An optional cancellation token. Will only cancel the provided action, not the sequence.</param>
        /// <returns>A task representing the provided action.</returns>
        public static Task ContinueWithSequential(this Task task, Action action, CancellationToken cancellationToken = default) =>
            task.ContinueWithSequential(() =>
            {
                action();
                return Task.CompletedTask;
            }, cancellationToken);

        /// <summary>
        /// Add a continuation to be performed only after the attached task has completed.
        /// </summary>
        /// <param name="task">The previous task to be awaited on.</param>
        /// <param name="continuationFunction">The continuation to run. Generally should be an async function.</param>
        /// <param name="cancellationToken">An optional cancellation token. Will only cancel the provided action, not the sequence.</param>
        /// <returns>A task representing the provided action.</returns>
        public static Task ContinueWithSequential(this Task task, Func<Task> continuationFunction, CancellationToken cancellationToken = default) =>
            // The continuation itself must not be cancelled before the preceding task completes.
            task.ContinueWith(async _ =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await continuationFunction().ConfigureAwait(false);
                }
                finally
                {
                    // Wait for the operation to finish before exposing cancellation, so the next operation cannot overtake it.
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
    }
}
