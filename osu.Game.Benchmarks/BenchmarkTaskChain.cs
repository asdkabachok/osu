// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using osu.Game.Utils;

namespace osu.Game.Benchmarks
{
    public class BenchmarkTaskChain : BenchmarkTest
    {
        private const int chain_length = 1000;

        [Benchmark(OperationsPerInvoke = chain_length)]
        public async Task Actions()
        {
            var chain = new TaskChain();
            Task last = Task.CompletedTask;

            for (int i = 0; i < chain_length; i++)
                last = chain.Add(static () => { });

            await last.ConfigureAwait(false);
        }

        [Benchmark(OperationsPerInvoke = chain_length)]
        public async Task CompletedAsyncFunctions()
        {
            var chain = new TaskChain();
            Task last = Task.CompletedTask;

            for (int i = 0; i < chain_length; i++)
                last = chain.Add(static () => Task.CompletedTask);

            await last.ConfigureAwait(false);
        }
    }
}
