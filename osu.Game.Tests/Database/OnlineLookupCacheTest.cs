// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Tests.Database
{
    [TestFixture]
    public partial class OnlineLookupCacheTest
    {
        private static readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

        [TestCase("create")]
        [TestCase("perform")]
        [TestCase("perform-synchronously")]
        [TestCase("retrieve")]
        [TestCase("enumerate")]
        public async Task TestFailedBatchCompletesAndAllowsFurtherLookups(string stage)
        {
            var api = new Mock<IAPIProvider>();
            api.Setup(p => p.PerformAsync(It.IsAny<APIRequest>())).Returns<APIRequest>(request =>
            {
                if (stage == "perform")
                    return Task.FromException(new InvalidOperationException("Request failed"));

                if (stage == "perform-synchronously")
                    throw new InvalidOperationException("Request failed synchronously");

                succeed(request, api.Object);
                return Task.CompletedTask;
            });

            using var cache = createCache(api.Object);
            cache.FailureStage = stage;
            Assert.That(await cache.GetUserAsync(1).WaitAsync(timeout), Is.Null);

            cache.FailureStage = null;
            api.Setup(p => p.PerformAsync(It.IsAny<APIRequest>())).Returns<APIRequest>(request =>
            {
                succeed(request, api.Object);
                return Task.CompletedTask;
            });

            Assert.That((await cache.GetUserAsync(2).WaitAsync(timeout))?.Id, Is.EqualTo(2));
        }

        [Test]
        public async Task TestQueuedBatchContinuesAfterRequestFailure()
        {
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var response = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var api = new Mock<IAPIProvider>();
            api.Setup(p => p.PerformAsync(It.IsAny<APIRequest>())).Returns<APIRequest>(request =>
            {
                if (((LookupUsersRequest)request).UserIds.Contains(1))
                {
                    started.SetResult();
                    return response.Task;
                }

                succeed(request, api.Object);
                return Task.CompletedTask;
            });

            using var cache = createCache(api.Object);
            var first = cache.GetUserAsync(1);
            await started.Task.WaitAsync(timeout);
            var queued = cache.GetUserAsync(2);
            response.SetException(new InvalidOperationException("Request failed"));

            Assert.That(await first.WaitAsync(timeout), Is.Null);
            Assert.That((await queued.WaitAsync(timeout))?.Id, Is.EqualTo(2));
        }

        [Test]
        public async Task TestCancellationDoesNotCancelOtherLookups()
        {
            var blockerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var blockerResponse = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var response = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int requestsForUser = 0;
            var api = new Mock<IAPIProvider>();
            api.Setup(p => p.PerformAsync(It.IsAny<APIRequest>())).Returns<APIRequest>(request =>
            {
                succeed(request, api.Object);

                if (((LookupUsersRequest)request).UserIds.Contains(999))
                {
                    blockerStarted.SetResult();
                    return blockerResponse.Task;
                }

                Interlocked.Increment(ref requestsForUser);
                started.SetResult();
                return response.Task;
            });

            using var cache = createCache(api.Object);
            using var cancellation = new CancellationTokenSource();

            var blocker = cache.GetUserAsync(999);
            await blockerStarted.Task.WaitAsync(timeout);

            var cancelled = cache.GetUserAsync(1, cancellation.Token);
            var other = cache.GetUserAsync(1);

            try
            {
                blockerResponse.SetResult();
                await started.Task.WaitAsync(timeout);

                await cancellation.CancelAsync();
                Assert.CatchAsync<OperationCanceledException>(async () => await cancelled.WaitAsync(timeout));
                Assert.That(other.IsCompleted, Is.False);
            }
            finally
            {
                blockerResponse.TrySetResult();
                response.TrySetResult();
            }

            await blocker.WaitAsync(timeout);
            Assert.That((await other.WaitAsync(timeout))?.Id, Is.EqualTo(1));
            Assert.That(requestsForUser, Is.EqualTo(1));
        }

        [Test]
        public void TestPreCancelledLookupDoesNotPerformRequest()
        {
            var api = new Mock<IAPIProvider>();
            using var cache = createCache(api.Object);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.That(Assert.CatchAsync<OperationCanceledException>(async () => await cache.GetUserAsync(1, cancellation.Token).WaitAsync(timeout))!.CancellationToken,
                Is.EqualTo(cancellation.Token));
            api.Verify(p => p.PerformAsync(It.IsAny<APIRequest>()), Times.Never());
        }

        [Test]
        public async Task TestBatchingPreservesOrderAndMissingValues()
        {
            var api = new Mock<IAPIProvider>();
            api.Setup(p => p.PerformAsync(It.IsAny<APIRequest>())).Returns<APIRequest>(request =>
            {
                var lookup = (LookupUsersRequest)request;
                Assert.That(lookup.UserIds.Length, Is.InRange(1, 50));
                lookup.AttachAPI(api.Object);
                lookup.TriggerSuccess(new GetUsersResponse
                {
                    Users = lookup.UserIds.Where(id => id != 7).Reverse().Select(id => new APIUser { Id = id }).ToList()
                });
                return Task.CompletedTask;
            });

            using var cache = createCache(api.Object);
            int[] ids = Enumerable.Range(1, 125).Concat(new[] { 3, 7, 3 }).ToArray();
            var users = await cache.GetUsersAsync(ids).WaitAsync(timeout);
            Assert.That(users.Select(user => user?.Id), Is.EqualTo(ids.Select(id => id == 7 ? (int?)null : id)));
        }

        private static TestLookupCache createCache(IAPIProvider api)
        {
            var dependencies = new DependencyContainer();
            dependencies.CacheAs(api);
            var cache = new TestLookupCache();
            dependencies.Inject(cache);
            return cache;
        }

        private static void succeed(APIRequest request, IAPIProvider api)
        {
            var lookup = (LookupUsersRequest)request;
            lookup.AttachAPI(api);
            lookup.TriggerSuccess(new GetUsersResponse { Users = lookup.UserIds.Select(id => new APIUser { Id = id }).ToList() });
        }

        private partial class TestLookupCache : UserLookupCache
        {
            public string? FailureStage { get; set; }

            protected override LookupUsersRequest CreateRequest(IEnumerable<int> ids)
            {
                if (FailureStage == "create")
                    throw new InvalidOperationException("Request creation failed");

                return base.CreateRequest(ids);
            }

            protected override IEnumerable<APIUser>? RetrieveResults(LookupUsersRequest request)
            {
                if (FailureStage == "retrieve")
                    throw new InvalidOperationException("Result retrieval failed");

                return FailureStage == "enumerate" ? failEnumeration() : base.RetrieveResults(request);
            }

            private static IEnumerable<APIUser> failEnumeration()
            {
                yield return new APIUser { Id = -1 };
                throw new InvalidOperationException("Result enumeration failed");
            }
        }
    }
}
