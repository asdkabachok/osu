// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Game.Extensions;
using osu.Game.Utils;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class TaskExtensionsTest
    {
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

        [TestCase(false)]
        [TestCase(true)]
        public async Task TestSynchronousExceptionDoesNotBlockChain(bool asyncFunction)
        {
            var chain = new TaskChain();
            var expected = new InvalidOperationException("Continuation failed synchronously.");

            Task failed = asyncFunction
                ? chain.Add(new Func<Task>(() => throw expected))
                : chain.Add(new Action(() => throw expected));

            bool nextRan = false;
            Task next = chain.Add(() => { nextRan = true; });

            Assert.That(Assert.ThrowsAsync<InvalidOperationException>(async () => await failed.WaitAsync(timeout)), Is.SameAs(expected));
            await next.WaitAsync(timeout);

            Assert.That(failed.IsFaulted, Is.True);
            Assert.That(nextRan, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task TestSynchronousCancellationDoesNotBlockChain(bool asyncFunction)
        {
            var chain = new TaskChain();

            Task cancelled = asyncFunction
                ? chain.Add(new Func<Task>(() => throw new OperationCanceledException()))
                : chain.Add(new Action(() => throw new OperationCanceledException()));
            Task next = chain.Add(() => { });

            Assert.CatchAsync<OperationCanceledException>(async () => await cancelled.WaitAsync(timeout));
            Assert.That(cancelled.IsCanceled, Is.True);
            await next.WaitAsync(timeout);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task TestContinuationRunsAfterUnsuccessfulPredecessor(bool cancelled, bool asyncFunction)
        {
            Task previous = cancelled
                ? Task.FromCanceled(new CancellationToken(true))
                : Task.FromException(new InvalidOperationException());

            bool ran = false;
            Task continuation = asyncFunction
                ? previous.ContinueWithSequential(() =>
                {
                    ran = true;
                    return Task.CompletedTask;
                })
                : previous.ContinueWithSequential(() => { ran = true; });

            await continuation.WaitAsync(timeout);
            Assert.That(ran, Is.True);

            // Observe the antecedent's exception without using it to control the continuation.
            if (!cancelled)
                _ = previous.Exception;
        }

        [Test]
        public async Task TestCancelledContinuationWaitsForPredecessor()
        {
            var previous = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = new CancellationTokenSource();

            bool cancelledActionRan = false;
            Task cancelled = previous.Task.ContinueWithSequential(() => { cancelledActionRan = true; }, cancellation.Token);
            Task next = cancelled.ContinueWithSequential(() => { });

            await cancellation.CancelAsync();

            Assert.That(cancelled.IsCompleted, Is.False);
            Assert.That(next.IsCompleted, Is.False);

            previous.SetResult(true);

            Assert.That(Assert.CatchAsync<OperationCanceledException>(async () => await cancelled.WaitAsync(timeout))!.CancellationToken, Is.EqualTo(cancellation.Token));
            await next.WaitAsync(timeout);

            Assert.That(cancelled.IsCanceled, Is.True);
            Assert.That(cancelledActionRan, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task TestContinuationWaitsForAsyncCompletion(bool cancelDuringExecution)
        {
            var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = new CancellationTokenSource();

            Task continuation = Task.CompletedTask.ContinueWithSequential(() =>
            {
                started.SetResult(true);
                return completed.Task;
            }, cancellation.Token);
            Task next = continuation.ContinueWithSequential(() => { });

            await started.Task.WaitAsync(timeout);

            if (cancelDuringExecution)
                await cancellation.CancelAsync();

            Assert.That(continuation.IsCompleted, Is.False);
            Assert.That(next.IsCompleted, Is.False);

            completed.SetResult(true);

            if (cancelDuringExecution)
            {
                Assert.CatchAsync<OperationCanceledException>(async () => await continuation.WaitAsync(timeout));
                Assert.That(continuation.IsCanceled, Is.True);
            }
            else
                await continuation.WaitAsync(timeout);

            await next.WaitAsync(timeout);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task TestUnsuccessfulChildDoesNotBlockChain(bool cancelled)
        {
            var chain = new TaskChain();
            Task child = cancelled
                ? Task.FromCanceled(new CancellationToken(true))
                : Task.FromException(new InvalidOperationException());

            Task unsuccessful = chain.Add(() => child);
            Task next = chain.Add(() => { });

            if (cancelled)
            {
                Assert.CatchAsync<OperationCanceledException>(async () => await unsuccessful.WaitAsync(timeout));
                Assert.That(unsuccessful.IsCanceled, Is.True);
            }
            else
                Assert.ThrowsAsync<InvalidOperationException>(async () => await unsuccessful.WaitAsync(timeout));

            await next.WaitAsync(timeout);
        }

        [Test]
        public async Task TestChildWithMultipleExceptionsUsesAwaitSemantics()
        {
            var first = new InvalidOperationException("First child failed");
            var second = new ArgumentException("Second child failed");
            Task child = Task.WhenAll(Task.FromException(first), Task.FromException(second));
            var chain = new TaskChain();
            Task failed = chain.Add(() => child);
            Task next = chain.Add(() => { });

            var propagated = Assert.CatchAsync<Exception>(async () => await failed.WaitAsync(timeout));

            Assert.That(propagated, Is.SameAs(first).Or.SameAs(second));
            Assert.That(failed.IsFaulted, Is.True);

            var aggregate = failed.Exception!;
            Assert.That(aggregate.InnerExceptions, Has.Count.EqualTo(1));
            Assert.That(aggregate.InnerException, Is.SameAs(propagated));
            Assert.That(child.Exception!.InnerExceptions, Is.EquivalentTo(new Exception[] { first, second }));

            await next.WaitAsync(timeout);
        }

        [Test]
        public async Task TestCancellationDuringExecutionTakesPrecedenceOverException()
        {
            var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = new CancellationTokenSource();

            Task continuation = Task.CompletedTask.ContinueWithSequential(() =>
            {
                started.SetResult(true);
                return completed.Task;
            }, cancellation.Token);

            await started.Task.WaitAsync(timeout);
            await cancellation.CancelAsync();
            completed.SetException(new InvalidOperationException());

            Assert.CatchAsync<OperationCanceledException>(async () => await continuation.WaitAsync(timeout));
            Assert.That(continuation.IsCanceled, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task TestContinuationUsesThreadPoolScheduler(bool asyncFunction)
        {
            var schedulers = new ConcurrentExclusiveSchedulerPair();

            try
            {
                Task continuation = await Task.Factory.StartNew(() =>
                {
                    Assert.That(TaskScheduler.Current, Is.SameAs(schedulers.ExclusiveScheduler));

                    return asyncFunction
                        ? Task.CompletedTask.ContinueWithSequential(() =>
                        {
                            assertDefaultScheduler();
                            return Task.CompletedTask;
                        })
                        : Task.CompletedTask.ContinueWithSequential(assertDefaultScheduler);
                }, CancellationToken.None, TaskCreationOptions.None, schedulers.ExclusiveScheduler);

                await continuation.WaitAsync(timeout);
            }
            finally
            {
                schedulers.Complete();
                await schedulers.Completion.WaitAsync(timeout);
            }

            static void assertDefaultScheduler()
            {
                Assert.That(TaskScheduler.Current, Is.SameAs(TaskScheduler.Default));
                Assert.That(Thread.CurrentThread.IsThreadPoolThread, Is.True);
            }
        }
    }
}
