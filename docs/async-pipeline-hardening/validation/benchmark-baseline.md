```

BenchmarkDotNet v0.15.8, Linux Arch Linux
AMD Ryzen 5 3550H with Radeon Vega Mobile Gfx 3.61GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.112
  [Host]     : .NET 10.0.12 (10.0.12, 42.42.42.42424), X64 RyuJIT x86-64-v3
  Job-IJPESX : .NET 10.0.12 (10.0.12, 42.42.42.42424), X64 RyuJIT x86-64-v3

IterationCount=5  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean       | Error    | StdDev   | Gen0   | Gen1   | Allocated |
|------------------------ |-----------:|---------:|---------:|-------:|-------:|----------:|
| Actions                 | 1,432.2 ns | 56.06 ns | 14.56 ns | 0.2129 | 0.0762 |     656 B |
| CompletedAsyncFunctions |   921.0 ns | 80.11 ns | 12.40 ns | 0.1826 | 0.0469 |     496 B |
