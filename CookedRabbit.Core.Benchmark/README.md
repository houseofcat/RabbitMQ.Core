
### Benchmark Results

<details><summary>Click here to see CookedRabbit.Core ConnectionPool results!</summary>
<p>

### Connection Count **5**

Exec `\bin\Release\netcoreapp3.1> dotnet .\Nuno.Rabbit.Benchmark.dll`  

``` ini

// * Detailed results *
ConnectionPoolBenchmark.OverLoopThroughConnectionPoolAsync: Job-ONKBMQ(Runtime=.NET Core 3.1) [x=5000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 959.9084 us, StdErr = 5.3247 us (0.55%); N = 36, StdDev = 31.9482 us
Min = 927.2766 us, Q1 = 933.0666 us, Median = 948.5885 us, Q3 = 986.6536 us, Max = 1,039.0297 us
IQR = 53.5870 us, LowerFence = 852.6861 us, UpperFence = 1,067.0341 us
ConfidenceInterval = [940.7866 us; 979.0302 us] (CI 99.9%), Margin = 19.1218 us (1.99% of Mean)
Skewness = 0.81, Kurtosis = 2.34, MValue = 2.17
-------------------- Histogram --------------------
[ 925.539 us ;  961.928 us) | @@@@@@@@@@@@@@@@@@@@@@@
[ 961.928 us ;  981.971 us) | @@@
[ 981.971 us ; 1004.437 us) | @@@@@
[1004.437 us ; 1047.496 us) | @@@@@
---------------------------------------------------

ConnectionPoolBenchmark.OverLoopThroughConnectionPoolAsync: Job-ONKBMQ(Runtime=.NET Core 3.1) [x=500000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 94.5670 ms, StdErr = 0.2272 ms (0.24%); N = 15, StdDev = 0.8799 ms
Min = 93.3479 ms, Q1 = 93.7606 ms, Median = 94.6125 ms, Q3 = 94.9492 ms, Max = 96.5233 ms
IQR = 1.1885 ms, LowerFence = 91.9778 ms, UpperFence = 96.7320 ms
ConfidenceInterval = [93.6263 ms; 95.5077 ms] (CI 99.9%), Margin = 0.9407 ms (0.99% of Mean)
Skewness = 0.46, Kurtosis = 2.44, MValue = 2
-------------------- Histogram --------------------
[93.036 ms ; 96.835 ms) | @@@@@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.ConcurrentOverLoopThroughConnectionPoolAsync: Job-ONKBMQ(Runtime=.NET Core 3.1) [x=1000000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 467.1578 ms, StdErr = 1.4711 ms (0.31%); N = 14, StdDev = 5.5042 ms
Min = 457.2518 ms, Q1 = 465.5087 ms, Median = 467.4185 ms, Q3 = 470.2617 ms, Max = 478.5753 ms
IQR = 4.7530 ms, LowerFence = 458.3792 ms, UpperFence = 477.3912 ms
ConfidenceInterval = [460.9488 ms; 473.3669 ms] (CI 99.9%), Margin = 6.2091 ms (1.33% of Mean)
Skewness = 0.09, Kurtosis = 2.54, MValue = 2
-------------------- Histogram --------------------
[455.254 ms ; 480.574 ms) | @@@@@@@@@@@@@@
---------------------------------------------------

// * Summary *

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17134.950 (1803/April2018Update/Redstone4)
Intel Core i5-8250U CPU 1.60GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  Job-ONKBMQ : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT

Runtime=.NET Core 3.1

|                                       Method |       x |         Mean |       Error |      StdDev | Completed Work Items | Lock Contentions | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------------- |-------- |-------------:|------------:|------------:|---------------------:|-----------------:|------:|------:|------:|----------:|
|           OverLoopThroughConnectionPoolAsync |    5000 |     959.9 us |    19.12 us |    31.95 us |               0.0059 |                - |     - |     - |     - |         - |
|           OverLoopThroughConnectionPoolAsync |  500000 |  94,567.0 us |   940.69 us |   879.92 us |              13.2500 |           1.2500 |     - |     - |     - |    3470 B |
| ConcurrentOverLoopThroughConnectionPoolAsync | 1000000 | 467,157.8 us | 6,209.08 us | 5,504.18 us |               6.0000 |        1841.0000 |     - |     - |     - |    1560 B |

// * Hints *
Outliers
  ConnectionPoolBenchmark.OverLoopThroughConnectionPoolAsync: Runtime=.NET Core 3.1           -> 2 outliers were removed (1.15 ms, 1.16 ms)
  ConnectionPoolBenchmark.ConcurrentOverLoopThroughConnectionPoolAsync: Runtime=.NET Core 3.1 -> 1 outlier  was  removed, 2 outliers were detected (457.25 ms, 483.07 ms)

// * Legends *
  x                    : Value of the 'x' parameter
  Mean                 : Arithmetic mean of all measurements
  Error                : Half of 99.9% confidence interval
  StdDev               : Standard deviation of all measurements
  Completed Work Items : The number of work items that have been processed in ThreadPool (per single operation)
  Lock Contentions     : The number of times there was contention upon trying to take a Monitor's lock (per single operation)
  Gen 0                : GC Generation 0 collects per 1000 operations
  Gen 1                : GC Generation 1 collects per 1000 operations
  Gen 2                : GC Generation 2 collects per 1000 operations
  Allocated            : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  1 us                 : 1 Microsecond (0.000001 sec)

// * Diagnostic Output - ThreadingDiagnoser *

// * Diagnostic Output - MemoryDiagnoser *

```
</p>
</details>

<details><summary>Click here to see CookedRabbit.Core ChannelPool results!</summary>
<p>

#### TODO: Reduce High Allocations?

``` ini
// * Detailed results *
ChannelPoolBenchmark.OverLoopThroughChannelPoolAsync: Job-TNXLYZ(Runtime=.NET Core 3.1) [x=5000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 2.3829 ms, StdErr = 0.0149 ms (0.63%); N = 49, StdDev = 0.1046 ms
Min = 2.2150 ms, Q1 = 2.2947 ms, Median = 2.3819 ms, Q3 = 2.4427 ms, Max = 2.7215 ms
IQR = 0.1480 ms, LowerFence = 2.0727 ms, UpperFence = 2.6646 ms
ConfidenceInterval = [2.3305 ms; 2.4352 ms] (CI 99.9%), Margin = 0.0524 ms (2.20% of Mean)
Skewness = 0.65, Kurtosis = 3.41, MValue = 2.71
-------------------- Histogram --------------------
[2.208 ms ; 2.267 ms) | @@@@@
[2.267 ms ; 2.317 ms) | @@@@@@@@@@@@@
[2.317 ms ; 2.385 ms) | @@@@@@@
[2.385 ms ; 2.446 ms) | @@@@@@@@@@@@@@
[2.446 ms ; 2.509 ms) | @@@
[2.509 ms ; 2.610 ms) | @@@@@@
[2.610 ms ; 2.746 ms) | @
---------------------------------------------------

ChannelPoolBenchmark.OverLoopThroughChannelPoolAsync: Job-TNXLYZ(Runtime=.NET Core 3.1) [x=500000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 224.6420 ms, StdErr = 1.2417 ms (0.55%); N = 34, StdDev = 7.2403 ms
Min = 216.1114 ms, Q1 = 219.7390 ms, Median = 221.8491 ms, Q3 = 226.2345 ms, Max = 242.8491 ms
IQR = 6.4955 ms, LowerFence = 209.9957 ms, UpperFence = 235.9777 ms
ConfidenceInterval = [220.1583 ms; 229.1257 ms] (CI 99.9%), Margin = 4.4837 ms (2.00% of Mean)
Skewness = 1.27, Kurtosis = 3.72, MValue = 2
-------------------- Histogram --------------------
[214.156 ms ; 222.423 ms) | @@@@@@@@@@@@@@@@@@
[222.423 ms ; 231.292 ms) | @@@@@@@@@@@
[231.292 ms ; 236.987 ms) | @@
[236.987 ms ; 244.805 ms) | @@@
---------------------------------------------------

ChannelPoolBenchmark.ConcurrentOverLoopThroughChannelPoolAsync: Job-TNXLYZ(Runtime=.NET Core 3.1) [x=1000000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 2.4535 s, StdErr = 0.0116 s (0.47%); N = 18, StdDev = 0.0491 s
Min = 2.3352 s, Q1 = 2.4300 s, Median = 2.4600 s, Q3 = 2.4821 s, Max = 2.5399 s
IQR = 0.0521 s, LowerFence = 2.3518 s, UpperFence = 2.5603 s
ConfidenceInterval = [2.4076 s; 2.4994 s] (CI 99.9%), Margin = 0.0459 s (1.87% of Mean)
Skewness = -0.41, Kurtosis = 2.95, MValue = 2
-------------------- Histogram --------------------
[2.319 s ; 2.372 s) | @
[2.372 s ; 2.422 s) | @@
[2.422 s ; 2.556 s) | @@@@@@@@@@@@@@@
---------------------------------------------------

// * Summary *

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17134.950 (1803/April2018Update/Redstone4)
Intel Core i5-8250U CPU 1.60GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  Job-TNXLYZ : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT

Runtime=.NET Core 3.1

|                                    Method |       x |         Mean |      Error |     StdDev | Completed Work Items | Lock Contentions |     Gen 0 | Gen 1 | Gen 2 |  Allocated |
|------------------------------------------ |-------- |-------------:|-----------:|-----------:|---------------------:|-----------------:|----------:|------:|------:|-----------:|
|           OverLoopThroughChannelPoolAsync |    5000 |     2.383 ms |  0.0524 ms |  0.1046 ms |               0.0352 |                - |         - |     - |     - |       11 B |
|           OverLoopThroughChannelPoolAsync |  500000 |   224.642 ms |  4.4837 ms |  7.2403 ms |               2.0000 |                - |         - |     - |     - |       64 B |
| ConcurrentOverLoopThroughChannelPoolAsync | 1000000 | 2,453.489 ms | 45.9111 ms | 49.1244 ms |           33306.0000 |         294.0000 | 5000.0000 |     - |     - | 18228000 B |

// * Hints *
Outliers
  ChannelPoolBenchmark.OverLoopThroughChannelPoolAsync: Runtime=.NET Core 3.1           -> 4 outliers were removed (2.79 ms..3.47 ms)
  ChannelPoolBenchmark.OverLoopThroughChannelPoolAsync: Runtime=.NET Core 3.1           -> 5 outliers were removed (259.15 ms..315.12 ms)
  ChannelPoolBenchmark.ConcurrentOverLoopThroughChannelPoolAsync: Runtime=.NET Core 3.1 -> 1 outlier  was  detected (2.34 s)

// * Legends *
  x                    : Value of the 'x' parameter
  Mean                 : Arithmetic mean of all measurements
  Error                : Half of 99.9% confidence interval
  StdDev               : Standard deviation of all measurements
  Completed Work Items : The number of work items that have been processed in ThreadPool (per single operation)
  Lock Contentions     : The number of times there was contention upon trying to take a Monitor's lock (per single operation)
  Gen 0                : GC Generation 0 collects per 1000 operations
  Gen 1                : GC Generation 1 collects per 1000 operations
  Gen 2                : GC Generation 2 collects per 1000 operations
  Allocated            : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  1 ms                 : 1 Millisecond (0.001 sec)

// * Diagnostic Output - ThreadingDiagnoser *

// * Diagnostic Output - MemoryDiagnoser *

```

</p>
</details>