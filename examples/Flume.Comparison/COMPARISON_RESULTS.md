## Technical Implementation Details

### Project Structure
```
Flume.Comparison/
├── Program.cs                           # Main comparison logic
├── Flume/                             # Flume-specific classes
│   ├── FlumeRequest.cs               # Flume request definition
│   └── FlumeHandler.cs               # Flume handler implementation
├── MediatR/                      # MediatR-specific classes
│   ├── MediatRRequest.cs        # MediatR request definition
│   └── MediatRHandler.cs        # MediatR handler implementation
├── Benchmarks/                          # Performance benchmarking
│   └── MediatorBenchmarks.cs           # BenchmarkDotNet benchmarks
├── README.md                            # Project documentation
└── COMPARISON_RESULTS.md                # This detailed analysis
```

### Dependencies
- **MediatR**: Version 12.5.0 (exactly as specified)
- **Flume**: Local project reference
- **BenchmarkDotNet**: For performance benchmarking
- **Microsoft.Extensions.DependencyInjection**: For DI container setup

## ShortRun results (net10.0, October 8, 2026)

BenchmarkDotNet 0.13.12 ShortRun (1 launch, 3 warmups, 3 iterations) on .NET 10.0.12. MediatR 12.5.0. Fresh methods resolve a new mediator per iteration. Behavior methods register one pass-through pipeline behavior. The error column is wide; do not treat the means as a ranking.

| Method | Mean | Error | StdDev | Allocated |
| --- | ---: | ---: | ---: | ---: |
| MediatRSend | 104.99 ns | 91.11 ns | 4.99 ns | 288 B |
| FlumeSend | 98.38 ns | 123.71 ns | 6.78 ns | 272 B |
| MediatRSendFreshMediator | 124.57 ns | 160.15 ns | 8.78 ns | 320 B |
| FlumeSendFreshMediator | 108.63 ns | 115.25 ns | 6.32 ns | 304 B |
| MediatRSendWithBehavior | 187.43 ns | 48.77 ns | 2.67 ns | 528 B |
| FlumeSendWithBehavior | 218.47 ns | 106.89 ns | 5.86 ns | 512 B |

### Namespace Resolution
The project uses `global::` namespace qualifiers to avoid conflicts between:
- Library namespaces (e.g., `global::MediatR.IMediator`)
- Local project namespaces (e.g., `Flume.Comparison.Flume`)
