
### Benchmark Results

<details><summary>Click here to see CookedRabbit.Core ConnectionPool results!</summary>
<p>

### Connection Count **5**

Exec `\bin\Release\netcoreapp3.1> dotnet .\Nuno.Rabbit.Benchmark.dll`  

``` ini

// * Detailed results *
ConnectionPoolBenchmark.CreateConnections: Job-UFMYZX(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 188.8394 ms, StdErr = 1.2325 ms (0.65%); N = 93, StdDev = 11.8863 ms
Min = 170.7511 ms, Q1 = 179.7125 ms, Median = 187.0516 ms, Q3 = 195.6691 ms, Max = 220.0894 ms
IQR = 15.9566 ms, LowerFence = 155.7776 ms, UpperFence = 219.6040 ms
ConfidenceInterval = [184.6495 ms; 193.0294 ms] (CI 99.9%), Margin = 4.1900 ms (2.22% of Mean)
Skewness = 0.88, Kurtosis = 3.13, MValue = 2.15
-------------------- Histogram --------------------
[168.456 ms ; 173.057 ms) | @
[173.057 ms ; 177.232 ms) | @@@@@@@@@@@@@
[177.232 ms ; 181.823 ms) | @@@@@@@@@@@@@@@@@@@@@
[181.823 ms ; 188.047 ms) | @@@@@@@@@@@@@@@@@@
[188.047 ms ; 195.794 ms) | @@@@@@@@@@@@@@@@@@
[195.794 ms ; 201.443 ms) | @@@@@@@@@
[201.443 ms ; 205.678 ms) | @@@
[205.678 ms ; 210.269 ms) | @@@@
[210.269 ms ; 214.910 ms) | @
[214.910 ms ; 220.678 ms) | @@@@@
---------------------------------------------------

ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Job-UFMYZX(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 13.9807 us, StdErr = 0.0422 us (0.30%); N = 14, StdDev = 0.1579 us
Min = 13.7673 us, Q1 = 13.8263 us, Median = 13.9890 us, Q3 = 14.1037 us, Max = 14.2314 us
IQR = 0.2774 us, LowerFence = 13.4103 us, UpperFence = 14.5197 us
ConfidenceInterval = [13.8026 us; 14.1588 us] (CI 99.9%), Margin = 0.1781 us (1.27% of Mean)
Skewness = 0.05, Kurtosis = 1.43, MValue = 2
-------------------- Histogram --------------------
[13.710 us ; 13.997 us) | @@@@@@@
[13.997 us ; 14.289 us) | @@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Job-UFMYZX(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 33.4051 us, StdErr = 0.1609 us (0.48%); N = 17, StdDev = 0.6634 us
Min = 32.5563 us, Q1 = 32.9200 us, Median = 33.1498 us, Q3 = 33.8951 us, Max = 34.9750 us
IQR = 0.9752 us, LowerFence = 31.4572 us, UpperFence = 35.3579 us
ConfidenceInterval = [32.7590 us; 34.0511 us] (CI 99.9%), Margin = 0.6460 us (1.93% of Mean)
Skewness = 0.77, Kurtosis = 2.52, MValue = 2
-------------------- Histogram --------------------
[32.331 us ; 33.397 us) | @@@@@@@@@@
[33.397 us ; 35.201 us) | @@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.CreateConnections: Job-UFMYZX(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 988.6353 ms, StdErr = 5.9550 ms (0.60%); N = 15, StdDev = 23.0637 ms
Min = 958.5036 ms, Q1 = 975.2646 ms, Median = 984.1579 ms, Q3 = 1,006.3836 ms, Max = 1,051.1668 ms
IQR = 31.1190 ms, LowerFence = 928.5861 ms, UpperFence = 1,053.0621 ms
ConfidenceInterval = [963.9787 ms; 1,013.2918 ms] (CI 99.9%), Margin = 24.6565 ms (2.49% of Mean)
Skewness = 1.15, Kurtosis = 4.03, MValue = 2
-------------------- Histogram --------------------
[ 953.378 ms ;  991.492 ms) | @@@@@@@@@@
[ 991.492 ms ; 1018.009 ms) | @@@@
[1018.009 ms ; 1059.350 ms) | @
---------------------------------------------------

ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Job-UFMYZX(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 68.1280 us, StdErr = 0.2132 us (0.31%); N = 14, StdDev = 0.7977 us
Min = 66.6865 us, Q1 = 67.5830 us, Median = 68.4280 us, Q3 = 68.7288 us, Max = 69.2343 us
IQR = 1.1458 us, LowerFence = 65.8642 us, UpperFence = 70.4475 us
ConfidenceInterval = [67.2282 us; 69.0278 us] (CI 99.9%), Margin = 0.8998 us (1.32% of Mean)
Skewness = -0.66, Kurtosis = 1.97, MValue = 2
-------------------- Histogram --------------------
[66.397 us ; 69.237 us) | @@@@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Job-UFMYZX(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 150.9252 us, StdErr = 0.1815 us (0.12%); N = 14, StdDev = 0.6790 us
Min = 148.8697 us, Q1 = 150.8291 us, Median = 150.9991 us, Q3 = 151.2874 us, Max = 151.7428 us
IQR = 0.4584 us, LowerFence = 150.1415 us, UpperFence = 151.9750 us
ConfidenceInterval = [150.1592 us; 151.6911 us] (CI 99.9%), Margin = 0.7659 us (0.51% of Mean)
Skewness = -1.78, Kurtosis = 6.23, MValue = 2
-------------------- Histogram --------------------
[148.623 us ; 151.989 us) | @@@@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Job-UFMYZX(Runtime=.NET Core 3.1) [x=5000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 682.2921 us, StdErr = 2.1027 us (0.31%); N = 15, StdDev = 8.1435 us
Min = 670.1727 us, Q1 = 676.5195 us, Median = 682.4503 us, Q3 = 687.3601 us, Max = 702.6864 us
IQR = 10.8405 us, LowerFence = 660.2587 us, UpperFence = 703.6208 us
ConfidenceInterval = [673.5861 us; 690.9980 us] (CI 99.9%), Margin = 8.7059 us (1.28% of Mean)
Skewness = 0.62, Kurtosis = 3.32, MValue = 2
-------------------- Histogram --------------------
[667.283 us ; 691.342 us) | @@@@@@@@@@@@@@
[691.342 us ; 705.576 us) | @
---------------------------------------------------

ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Job-UFMYZX(Runtime=.NET Core 3.1) [x=5000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 1.5306 ms, StdErr = 0.0016 ms (0.10%); N = 15, StdDev = 0.0062 ms
Min = 1.5186 ms, Q1 = 1.5269 ms, Median = 1.5319 ms, Q3 = 1.5345 ms, Max = 1.5419 ms
IQR = 0.0076 ms, LowerFence = 1.5155 ms, UpperFence = 1.5459 ms
ConfidenceInterval = [1.5241 ms; 1.5372 ms] (CI 99.9%), Margin = 0.0066 ms (0.43% of Mean)
Skewness = -0.19, Kurtosis = 2.27, MValue = 2
-------------------- Histogram --------------------
[1.516 ms ; 1.544 ms) | @@@@@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Job-UFMYZX(Runtime=.NET Core 3.1) [x=500000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 68.8336 ms, StdErr = 0.3416 ms (0.50%); N = 20, StdDev = 1.5278 ms
Min = 66.5819 ms, Q1 = 67.5161 ms, Median = 69.0056 ms, Q3 = 69.7408 ms, Max = 71.9615 ms
IQR = 2.2247 ms, LowerFence = 64.1790 ms, UpperFence = 73.0778 ms
ConfidenceInterval = [67.5069 ms; 70.1603 ms] (CI 99.9%), Margin = 1.3267 ms (1.93% of Mean)
Skewness = 0.2, Kurtosis = 2, MValue = 2
-------------------- Histogram --------------------
[66.320 ms ; 68.478 ms) | @@@@@@@@
[68.478 ms ; 72.273 ms) | @@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Job-UFMYZX(Runtime=.NET Core 3.1) [x=500000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 148.4256 ms, StdErr = 0.3213 ms (0.22%); N = 14, StdDev = 1.2021 ms
Min = 145.9745 ms, Q1 = 147.9122 ms, Median = 148.6135 ms, Q3 = 149.2087 ms, Max = 150.1022 ms
IQR = 1.2965 ms, LowerFence = 145.9675 ms, UpperFence = 151.1534 ms
ConfidenceInterval = [147.0696 ms; 149.7817 ms] (CI 99.9%), Margin = 1.3560 ms (0.91% of Mean)
Skewness = -0.5, Kurtosis = 2.11, MValue = 2
-------------------- Histogram --------------------
[145.538 ms ; 150.539 ms) | @@@@@@@@@@@@@@
---------------------------------------------------

ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Job-UFMYZX(Runtime=.NET Core 3.1) [x=1000000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 276.4316 ms, StdErr = 1.2183 ms (0.44%); N = 14, StdDev = 4.5585 ms
Min = 269.7410 ms, Q1 = 272.9067 ms, Median = 276.3680 ms, Q3 = 279.3954 ms, Max = 284.5964 ms
IQR = 6.4887 ms, LowerFence = 263.1737 ms, UpperFence = 289.1284 ms
ConfidenceInterval = [271.2893 ms; 281.5738 ms] (CI 99.9%), Margin = 5.1423 ms (1.86% of Mean)
Skewness = 0.07, Kurtosis = 1.78, MValue = 2
-------------------- Histogram --------------------
[268.086 ms ; 275.678 ms) | @@@@@@@
[275.678 ms ; 286.251 ms) | @@@@@@@
---------------------------------------------------

// * Summary *

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17763.914 (1809/October2018Update/Redstone5)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  Job-UFMYZX : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT

Runtime=.NET Core 3.1

|                                         Method |       x |          Mean |         Error |        StdDev | Ratio | RatioSD |     Gen 0 |     Gen 1 | Gen 2 |  Allocated | Completed Work Items | Lock Contentions |
|----------------------------------------------- |-------- |--------------:|--------------:|--------------:|------:|--------:|----------:|----------:|------:|-----------:|---------------------:|-----------------:|
|                              CreateConnections |     100 | 188,839.45 us |  4,189.977 us | 11,886.268 us | 1.000 |    0.00 | 1333.3333 |  333.3333 |     - | 10126547 B |             290.3333 |          16.3333 |
|           GetConnectionFromConnectionPoolAsync |     100 |      13.98 us |      0.178 us |      0.158 us | 0.000 |    0.00 |         - |         - |     - |          - |               0.0003 |                - |
| ConcurrentGetConnectionFromConnectionPoolAsync |     100 |      33.41 us |      0.646 us |      0.663 us | 0.000 |    0.00 |    0.1831 |    0.0610 |     - |     1248 B |               4.0001 |           0.0197 |
|                                                |         |               |               |               |       |         |           |           |       |            |                      |                  |
|                              CreateConnections |     500 | 988,635.27 us | 24,656.546 us | 23,063.748 us | 1.000 |    0.00 | 8000.0000 | 2000.0000 |     - | 50628056 B |            1009.0000 |                - |
|           GetConnectionFromConnectionPoolAsync |     500 |      68.13 us |      0.900 us |      0.798 us | 0.000 |    0.00 |         - |         - |     - |          - |               0.0002 |                - |
| ConcurrentGetConnectionFromConnectionPoolAsync |     500 |     150.93 us |      0.766 us |      0.679 us | 0.000 |    0.00 |         - |         - |     - |     1248 B |               4.0005 |           0.3455 |
|                                                |         |               |               |               |       |         |           |           |       |            |                      |                  |
|           GetConnectionFromConnectionPoolAsync |    5000 |     682.29 us |      8.706 us |      8.144 us |     ? |       ? |         - |         - |     - |        4 B |               0.0068 |           0.0039 |
| ConcurrentGetConnectionFromConnectionPoolAsync |    5000 |   1,530.65 us |      6.590 us |      6.164 us |     ? |       ? |         - |         - |     - |     1249 B |               4.0039 |           2.9668 |
|                                                |         |               |               |               |       |         |           |           |       |            |                      |                  |
|           GetConnectionFromConnectionPoolAsync |  500000 |  68,833.64 us |  1,326.702 us |  1,527.832 us |     ? |       ? |         - |         - |     - |          - |               0.2500 |                - |
| ConcurrentGetConnectionFromConnectionPoolAsync |  500000 | 148,425.62 us |  1,356.045 us |  1,202.099 us |     ? |       ? |         - |         - |     - |     1996 B |               7.5000 |        1004.7500 |
|                                                |         |               |               |               |       |         |           |           |       |            |                      |                  |
| ConcurrentGetConnectionFromConnectionPoolAsync | 1000000 | 276,431.58 us |  5,142.265 us |  4,558.484 us |     ? |       ? |         - |         - |     - |     1560 B |               6.0000 |        1276.0000 |

// * Warnings *
BaselineCustomAnalyzer
  Summary -> A question mark '?' symbol indicates that it was not possible to compute the (Ratio, RatioSD) column(s) because the baseline value is too close to zero.

// * Hints *
Outliers
  ConnectionPoolBenchmark.CreateConnections: Runtime=.NET Core 3.1                              -> 7 outliers were removed (237.28 ms..277.14 ms)
  ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Runtime=.NET Core 3.1           -> 1 outlier  was  removed (14.73 us)
  ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Runtime=.NET Core 3.1           -> 1 outlier  was  removed (71.82 us)
  ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Runtime=.NET Core 3.1 -> 1 outlier  was  removed, 2 outliers were detected (148.87 us, 153.07 us)
  ConnectionPoolBenchmark.GetConnectionFromConnectionPoolAsync: Runtime=.NET Core 3.1           -> 2 outliers were removed (76.96 ms, 82.05 ms)
  ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Runtime=.NET Core 3.1 -> 1 outlier  was  removed (152.73 ms)
  ConnectionPoolBenchmark.ConcurrentGetConnectionFromConnectionPoolAsync: Runtime=.NET Core 3.1 -> 1 outlier  was  removed (296.54 ms)

// * Legends *
  x                    : Value of the 'x' parameter
  Mean                 : Arithmetic mean of all measurements
  Error                : Half of 99.9% confidence interval
  StdDev               : Standard deviation of all measurements
  Ratio                : Mean of the ratio distribution ([Current]/[Baseline])
  RatioSD              : Standard deviation of the ratio distribution ([Current]/[Baseline])
  Gen 0                : GC Generation 0 collects per 1000 operations
  Gen 1                : GC Generation 1 collects per 1000 operations
  Gen 2                : GC Generation 2 collects per 1000 operations
  Allocated            : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  Completed Work Items : The number of work items that have been processed in ThreadPool (per single operation)
  Lock Contentions     : The number of times there was contention upon trying to take a Monitor's lock (per single operation)
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
ChannelPoolBenchmark.CreateConnectionsAndChannels: Job-QBQVYV(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 224.6893 ms, StdErr = 1.1984 ms (0.53%); N = 25, StdDev = 5.9918 ms
Min = 213.7771 ms, Q1 = 221.2931 ms, Median = 223.3545 ms, Q3 = 228.3837 ms, Max = 237.2627 ms
IQR = 7.0906 ms, LowerFence = 210.6571 ms, UpperFence = 239.0196 ms
ConfidenceInterval = [220.2010 ms; 229.1777 ms] (CI 99.9%), Margin = 4.4884 ms (2.00% of Mean)
Skewness = 0.47, Kurtosis = 2.66, MValue = 2
-------------------- Histogram --------------------
[211.984 ms ; 219.348 ms) | @@@@
[219.348 ms ; 225.064 ms) | @@@@@@@@@@@@
[225.064 ms ; 230.949 ms) | @@@@@@
[230.949 ms ; 238.288 ms) | @@@
---------------------------------------------------

ChannelPoolBenchmark.CreateChannelsWithConnectionFromConnectionPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 38.3984 ms, StdErr = 0.3361 ms (0.88%); N = 100, StdDev = 3.3610 ms
Min = 34.6109 ms, Q1 = 35.6835 ms, Median = 36.7644 ms, Q3 = 40.8922 ms, Max = 46.5081 ms
IQR = 5.2086 ms, LowerFence = 27.8706 ms, UpperFence = 48.7051 ms
ConfidenceInterval = [37.2585 ms; 39.5383 ms] (CI 99.9%), Margin = 1.1399 ms (2.97% of Mean)
Skewness = 0.93, Kurtosis = 2.47, MValue = 2.48
-------------------- Histogram --------------------
[33.977 ms ; 35.314 ms) | @@@
[35.314 ms ; 36.581 ms) | @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
[36.581 ms ; 37.890 ms) | @@@@@@@@@@@@
[37.890 ms ; 39.493 ms) | @@@@@@@@@@@@
[39.493 ms ; 41.501 ms) | @@@
[41.501 ms ; 43.224 ms) | @@@@@@@@
[43.224 ms ; 44.491 ms) | @@@@@@@@@@@
[44.491 ms ; 45.480 ms) | @
[45.480 ms ; 46.747 ms) | @@@@
---------------------------------------------------

ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 31.7753 us, StdErr = 0.1529 us (0.48%); N = 18, StdDev = 0.6487 us
Min = 30.5634 us, Q1 = 31.5259 us, Median = 31.8454 us, Q3 = 32.1852 us, Max = 32.8726 us
IQR = 0.6594 us, LowerFence = 30.5368 us, UpperFence = 33.1743 us
ConfidenceInterval = [31.1691 us; 32.3816 us] (CI 99.9%), Margin = 0.6062 us (1.91% of Mean)
Skewness = -0.41, Kurtosis = 2.21, MValue = 2.15
-------------------- Histogram --------------------
[30.347 us ; 30.972 us) | @@@
[30.972 us ; 31.655 us) | @@
[31.655 us ; 33.089 us) | @@@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=100]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 154.0309 us, StdErr = 0.6295 us (0.41%); N = 15, StdDev = 2.4381 us
Min = 149.7299 us, Q1 = 151.8145 us, Median = 154.6247 us, Q3 = 155.5465 us, Max = 156.9986 us
IQR = 3.7320 us, LowerFence = 146.2165 us, UpperFence = 161.1445 us
ConfidenceInterval = [151.4244 us; 156.6374 us] (CI 99.9%), Margin = 2.6065 us (1.69% of Mean)
Skewness = -0.71, Kurtosis = 1.98, MValue = 2
-------------------- Histogram --------------------
[148.865 us ; 153.400 us) | @@@@
[153.400 us ; 157.864 us) | @@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.CreateConnectionsAndChannels: Job-QBQVYV(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 1.1904 s, StdErr = 0.0075 s (0.63%); N = 98, StdDev = 0.0741 s
Min = 1.0822 s, Q1 = 1.1330 s, Median = 1.1655 s, Q3 = 1.2368 s, Max = 1.3933 s
IQR = 0.1038 s, LowerFence = 0.9772 s, UpperFence = 1.3926 s
ConfidenceInterval = [1.1650 s; 1.2158 s] (CI 99.9%), Margin = 0.0254 s (2.13% of Mean)
Skewness = 0.97, Kurtosis = 3.06, MValue = 2.22
-------------------- Histogram --------------------
[1.079 s ; 1.109 s) | @@
[1.109 s ; 1.137 s) | @@@@@@@@@@@@@@@@@@@@@@@@@@
[1.137 s ; 1.174 s) | @@@@@@@@@@@@@@@@@@@@@@@@@@@
[1.174 s ; 1.205 s) | @@@@@@@@@@@@
[1.205 s ; 1.247 s) | @@@@@@@@@@@
[1.247 s ; 1.299 s) | @@@@@@@@@@@
[1.299 s ; 1.344 s) | @@@@
[1.344 s ; 1.375 s) | @@@
[1.375 s ; 1.407 s) | @@
---------------------------------------------------

ChannelPoolBenchmark.CreateChannelsWithConnectionFromConnectionPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 191.7547 ms, StdErr = 1.6854 ms (0.88%); N = 99, StdDev = 16.7692 ms
Min = 177.0504 ms, Q1 = 178.9922 ms, Median = 183.3391 ms, Q3 = 204.1449 ms, Max = 237.2531 ms
IQR = 25.1527 ms, LowerFence = 141.2631 ms, UpperFence = 241.8740 ms
ConfidenceInterval = [186.0369 ms; 197.4725 ms] (CI 99.9%), Margin = 5.7178 ms (2.98% of Mean)
Skewness = 1.11, Kurtosis = 2.87, MValue = 2.48
-------------------- Histogram --------------------
[173.879 ms ; 183.742 ms) | @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
[183.742 ms ; 189.985 ms) | @@@@@@@@@@@@
[189.985 ms ; 195.110 ms) |
[195.110 ms ; 201.453 ms) | @@@@@@@
[201.453 ms ; 209.472 ms) | @@@@@@@@
[209.472 ms ; 216.579 ms) | @@@@
[216.579 ms ; 224.250 ms) | @@@@@@@@@
[224.250 ms ; 231.611 ms) | @@@
[231.611 ms ; 240.425 ms) | @@
---------------------------------------------------

ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 157.1356 us, StdErr = 0.5658 us (0.36%); N = 15, StdDev = 2.1912 us
Min = 153.6344 us, Q1 = 154.7481 us, Median = 157.9797 us, Q3 = 159.2513 us, Max = 159.8721 us
IQR = 4.5032 us, LowerFence = 147.9933 us, UpperFence = 166.0062 us
ConfidenceInterval = [154.7931 us; 159.4781 us] (CI 99.9%), Margin = 2.3425 us (1.49% of Mean)
Skewness = -0.24, Kurtosis = 1.37, MValue = 2
-------------------- Histogram --------------------
[152.857 us ; 160.649 us) | @@@@@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=500]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 826.4894 us, StdErr = 2.7309 us (0.33%); N = 15, StdDev = 10.5769 us
Min = 810.1403 us, Q1 = 815.2854 us, Median = 826.3051 us, Q3 = 836.4006 us, Max = 841.8830 us
IQR = 21.1152 us, LowerFence = 783.6125 us, UpperFence = 868.0734 us
ConfidenceInterval = [815.1821 us; 837.7968 us] (CI 99.9%), Margin = 11.3074 us (1.37% of Mean)
Skewness = -0.03, Kurtosis = 1.61, MValue = 2
-------------------- Histogram --------------------
[806.388 us ; 845.636 us) | @@@@@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=5000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 1.5865 ms, StdErr = 0.0071 ms (0.45%); N = 14, StdDev = 0.0266 ms
Min = 1.5370 ms, Q1 = 1.5684 ms, Median = 1.5924 ms, Q3 = 1.5984 ms, Max = 1.6294 ms
IQR = 0.0300 ms, LowerFence = 1.5233 ms, UpperFence = 1.6435 ms
ConfidenceInterval = [1.5565 ms; 1.6165 ms] (CI 99.9%), Margin = 0.0300 ms (1.89% of Mean)
Skewness = -0.32, Kurtosis = 2.15, MValue = 2
-------------------- Histogram --------------------
[1.527 ms ; 1.578 ms) | @@@@
[1.578 ms ; 1.639 ms) | @@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=5000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 7.4570 ms, StdErr = 0.0192 ms (0.26%); N = 15, StdDev = 0.0742 ms
Min = 7.2215 ms, Q1 = 7.4361 ms, Median = 7.4662 ms, Q3 = 7.5111 ms, Max = 7.5237 ms
IQR = 0.0750 ms, LowerFence = 7.3235 ms, UpperFence = 7.6237 ms
ConfidenceInterval = [7.3776 ms; 7.5363 ms] (CI 99.9%), Margin = 0.0793 ms (1.06% of Mean)
Skewness = -1.97, Kurtosis = 6.9, MValue = 2
-------------------- Histogram --------------------
[7.195 ms ; 7.369 ms) | @
[7.369 ms ; 7.550 ms) | @@@@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=500000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 159.1608 ms, StdErr = 0.7248 ms (0.46%); N = 14, StdDev = 2.7121 ms
Min = 156.1364 ms, Q1 = 157.0192 ms, Median = 158.6941 ms, Q3 = 160.0913 ms, Max = 165.5402 ms
IQR = 3.0720 ms, LowerFence = 152.4111 ms, UpperFence = 164.6993 ms
ConfidenceInterval = [156.1013 ms; 162.2202 ms] (CI 99.9%), Margin = 3.0594 ms (1.92% of Mean)
Skewness = 0.88, Kurtosis = 2.75, MValue = 2
-------------------- Histogram --------------------
[155.152 ms ; 158.325 ms) | @@@@@@@
[158.325 ms ; 166.525 ms) | @@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=500000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 769.6671 ms, StdErr = 2.8580 ms (0.37%); N = 15, StdDev = 11.0690 ms
Min = 748.6483 ms, Q1 = 768.6009 ms, Median = 772.2666 ms, Q3 = 778.2161 ms, Max = 781.0579 ms
IQR = 9.6152 ms, LowerFence = 754.1781 ms, UpperFence = 792.6389 ms
ConfidenceInterval = [757.8337 ms; 781.5005 ms] (CI 99.9%), Margin = 11.8334 ms (1.54% of Mean)
Skewness = -0.96, Kurtosis = 2.35, MValue = 2
-------------------- Histogram --------------------
[744.721 ms ; 783.590 ms) | @@@@@@@@@@@@@@@
---------------------------------------------------

ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=1000000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 325.0630 ms, StdErr = 1.3578 ms (0.42%); N = 15, StdDev = 5.2588 ms
Min = 319.1102 ms, Q1 = 321.8160 ms, Median = 323.3660 ms, Q3 = 330.1112 ms, Max = 336.1790 ms
IQR = 8.2952 ms, LowerFence = 309.3732 ms, UpperFence = 342.5540 ms
ConfidenceInterval = [319.4410 ms; 330.6850 ms] (CI 99.9%), Margin = 5.6220 ms (1.73% of Mean)
Skewness = 0.77, Kurtosis = 2.29, MValue = 2
-------------------- Histogram --------------------
[317.244 ms ; 326.484 ms) | @@@@@@@@@@@
[326.484 ms ; 338.045 ms) | @@@@
---------------------------------------------------

ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Job-QBQVYV(Runtime=.NET Core 3.1) [x=1000000]
Runtime = .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT; GC = Concurrent Workstation
Mean = 1.6499 s, StdErr = 0.0026 s (0.16%); N = 15, StdDev = 0.0101 s
Min = 1.6327 s, Q1 = 1.6414 s, Median = 1.6498 s, Q3 = 1.6590 s, Max = 1.6650 s
IQR = 0.0176 s, LowerFence = 1.6150 s, UpperFence = 1.6853 s
ConfidenceInterval = [1.6391 s; 1.6606 s] (CI 99.9%), Margin = 0.0108 s (0.65% of Mean)
Skewness = -0.2, Kurtosis = 1.53, MValue = 2
-------------------- Histogram --------------------
[1.629 s ; 1.669 s) | @@@@@@@@@@@@@@@
---------------------------------------------------

// * Summary *

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.17763.914 (1809/October2018Update/Redstone5)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  Job-QBQVYV : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT

Runtime=.NET Core 3.1

|                                              Method |       x |            Mean |         Error |        StdDev |          Median | Ratio | RatioSD |     Gen 0 |     Gen 1 | Gen 2 |  Allocated | Completed Work Items | Lock Contentions |
|---------------------------------------------------- |-------- |----------------:|--------------:|--------------:|----------------:|------:|--------:|----------:|----------:|------:|-----------:|---------------------:|-----------------:|
|                        CreateConnectionsAndChannels |     100 |   224,689.35 us |  4,488.373 us |  5,991.849 us |   223,354.50 us | 1.000 |    0.00 | 1666.6667 |  333.3333 |     - | 10764187 B |             206.3333 |           1.3333 |
| CreateChannelsWithConnectionFromConnectionPoolAsync |     100 |    38,398.41 us |  1,139.877 us |  3,360.954 us |    36,764.43 us | 0.167 |    0.01 |   66.6667 |         - |     - |   629968 B |               1.4667 |           0.4667 |
|                      GetChannelFromChannelPoolAsync |     100 |        31.78 us |      0.606 us |      0.649 us |        31.85 us | 0.000 |    0.00 |         - |         - |     - |          - |               0.0001 |                - |
|            ConcurrentGetChannelFromChannelPoolAsync |     100 |       154.03 us |      2.606 us |      2.438 us |       154.62 us | 0.001 |    0.00 |         - |         - |     - |     1477 B |               4.3433 |           0.0283 |
|                                                     |         |                 |               |               |                 |       |         |           |           |       |            |                      |                  |
|                        CreateConnectionsAndChannels |     500 | 1,190,376.68 us | 25,392.413 us | 74,070.861 us | 1,165,524.80 us | 1.000 |    0.00 | 8000.0000 | 2000.0000 |     - | 53812000 B |            1486.0000 |          55.0000 |
| CreateChannelsWithConnectionFromConnectionPoolAsync |     500 |   191,754.70 us |  5,717.763 us | 16,769.210 us |   183,339.13 us | 0.162 |    0.02 |  333.3333 |         - |     - |  3149787 B |               4.0000 |           3.0000 |
|                      GetChannelFromChannelPoolAsync |     500 |       157.14 us |      2.343 us |      2.191 us |       157.98 us | 0.000 |    0.00 |         - |         - |     - |        1 B |               0.0032 |                - |
|            ConcurrentGetChannelFromChannelPoolAsync |     500 |       826.49 us |     11.307 us |     10.577 us |       826.31 us | 0.001 |    0.00 |         - |         - |     - |     1915 B |               5.0479 |           0.2178 |
|                                                     |         |                 |               |               |                 |       |         |           |           |       |            |                      |                  |
|                      GetChannelFromChannelPoolAsync |    5000 |     1,586.49 us |     30.015 us |     26.607 us |     1,592.39 us |     ? |       ? |         - |         - |     - |       12 B |               0.0254 |           0.0039 |
|            ConcurrentGetChannelFromChannelPoolAsync |    5000 |     7,456.96 us |     79.338 us |     74.213 us |     7,466.21 us |     ? |       ? |         - |         - |     - |    11674 B |              21.9531 |           1.2500 |
|                                                     |         |                 |               |               |                 |       |         |           |           |       |            |                      |                  |
|                      GetChannelFromChannelPoolAsync |  500000 |   159,160.78 us |  3,059.447 us |  2,712.120 us |   158,694.05 us |     ? |       ? |         - |         - |     - |      314 B |               0.5000 |                - |
|            ConcurrentGetChannelFromChannelPoolAsync |  500000 |   769,667.07 us | 11,833.401 us | 11,068.970 us |   772,266.60 us |     ? |       ? |         - |         - |     - |  2952096 B |            5375.0000 |         162.0000 |
|                                                     |         |                 |               |               |                 |       |         |           |           |       |            |                      |                  |
|                      GetChannelFromChannelPoolAsync | 1000000 |   325,062.98 us |  5,621.991 us |  5,258.814 us |   323,366.00 us |     ? |       ? |         - |         - |     - |       88 B |               2.0000 |                - |
|            ConcurrentGetChannelFromChannelPoolAsync | 1000000 | 1,649,872.33 us | 10,761.886 us | 10,066.675 us | 1,649,778.10 us |     ? |       ? |         - |         - |     - |  1892192 B |            3448.0000 |         412.0000 |

// * Warnings *
BaselineCustomAnalyzer
  Summary -> A question mark '?' symbol indicates that it was not possible to compute the (Ratio, RatioSD) column(s) because the baseline value is too close to zero.

// * Hints *
Outliers
  ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Runtime=.NET Core 3.1                      -> 3 outliers were removed (34.45 us..34.76 us)
  ChannelPoolBenchmark.CreateConnectionsAndChannels: Runtime=.NET Core 3.1                        -> 2 outliers were removed (1.47 s, 1.62 s)
  ChannelPoolBenchmark.CreateChannelsWithConnectionFromConnectionPoolAsync: Runtime=.NET Core 3.1 -> 1 outlier  was  removed (244.15 ms)
  ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Runtime=.NET Core 3.1                      -> 1 outlier  was  removed (1.74 ms)
  ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Runtime=.NET Core 3.1            -> 1 outlier  was  detected (7.22 ms)
  ChannelPoolBenchmark.GetChannelFromChannelPoolAsync: Runtime=.NET Core 3.1                      -> 1 outlier  was  removed (170.62 ms)
  ChannelPoolBenchmark.ConcurrentGetChannelFromChannelPoolAsync: Runtime=.NET Core 3.1            -> 3 outliers were detected (748.65 ms..750.58 ms)

// * Legends *
  x                    : Value of the 'x' parameter
  Mean                 : Arithmetic mean of all measurements
  Error                : Half of 99.9% confidence interval
  StdDev               : Standard deviation of all measurements
  Median               : Value separating the higher half of all measurements (50th percentile)
  Ratio                : Mean of the ratio distribution ([Current]/[Baseline])
  RatioSD              : Standard deviation of the ratio distribution ([Current]/[Baseline])
  Gen 0                : GC Generation 0 collects per 1000 operations
  Gen 1                : GC Generation 1 collects per 1000 operations
  Gen 2                : GC Generation 2 collects per 1000 operations
  Allocated            : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  Completed Work Items : The number of work items that have been processed in ThreadPool (per single operation)
  Lock Contentions     : The number of times there was contention upon trying to take a Monitor's lock (per single operation)
  1 us                 : 1 Microsecond (0.000001 sec)

// * Diagnostic Output - ThreadingDiagnoser *

// * Diagnostic Output - MemoryDiagnoser *

```

</p>
</details>