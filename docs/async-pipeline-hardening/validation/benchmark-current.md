```

BenchmarkDotNet v0.15.8, Linux Arch Linux
AMD Ryzen 5 3550H with Radeon Vega Mobile Gfx 3.68GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]     : .NET 10.0.12 (10.0.12, 42.42.42.42424), X64 RyuJIT x86-64-v3
  Job-IJPESX : .NET 10.0.12 (10.0.12, 42.42.42.42424), X64 RyuJIT x86-64-v3

IterationCount=5  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean     | Error    | StdDev  | Gen0   | Gen1   | Allocated |
|------------------------ |---------:|---------:|--------:|-------:|-------:|----------:|
| Actions                 | 496.2 ns | 26.74 ns | 6.94 ns | 0.1309 | 0.0361 |     376 B |
| CompletedAsyncFunctions | 506.3 ns | 12.75 ns | 3.31 ns | 0.1162 | 0.0244 |     288 B |
