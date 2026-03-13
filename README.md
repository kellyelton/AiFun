# AiFun

An interactive evolutionary simulation where neural network-controlled creatures compete for survival in a 2D ecosystem. Creatures evolve through natural selection, developing strategies for foraging, hunting, and reproduction over generations.

## Features

- **Neural Network Brains** — Each creature is driven by a feedforward neural network with configurable hidden layers, evolved through crossover and Gaussian mutation
- **Multi-Ray Vision** — Creatures perceive their environment through multiple vision rays that detect walls, food, and other creatures, with speed-dependent tunnel vision
- **Energy Economy** — Survival requires foraging food pellets; energy drains through movement, metabolism, and vision. Death occurs at zero energy
- **Genetic Reproduction** — Creatures with sufficient energy can mate, producing offspring that inherit neural network weights and genetic traits (color, vision distance, pregnancy duration) with mutation
- **Tournament Selection** — Generational breeding uses tournament selection from the dead population, maintaining diversity while rewarding fitness
- **Hall of Fame** — Top performers are preserved across generations and re-injected as clones, preventing loss of successful strategies
- **Heavy Mutant Injection** — Genetic diversity is maintained by injecting heavily mutated variants of existing genomes alongside elite offspring
- **Live Evolution Graph** — Real-time chart tracking survival time, distance, vision distance, food eaten, and baby count across generations
- **Performance Optimized** — Spatial indexing for collision detection, pre-allocated neural network buffers, and batched UI updates for large populations

## Tech Stack

- **C# / .NET 9.0** (Windows)
- **WPF** for real-time visualization
- **Encog.NET** for neural network computation
- **xUnit** test suite (28+ test classes)
- **BenchmarkDotNet** for performance profiling

## Getting Started

**Prerequisites:** .NET 9.0 SDK, Windows OS

```bash
dotnet build AiFun.sln
dotnet run --project AiFun/AiFun.csproj
```

Or open `AiFun.sln` in Visual Studio and press F5.

## Controls

| Key | Action |
|-------|-------------------------------|
| Space | Pause / Resume |
| F5 | Reset ecosystem |
| G | Trigger next generation |
| V | Toggle vision ray overlay |
| H | Toggle Hall of Fame indicators |

All ecosystem parameters (population sizes, mutation rate, energy costs, food settings, neural network size, etc.) are adjustable in real-time via the HUD panel.

## Project Structure

```
AiFun/              Main WPF application
  Animal.cs           Creature logic, neural network mapping, vision, reproduction
  Ecosystem.cs        World simulation, collision handling, generational selection
  FoodPellet.cs       Food resources with energy growth mechanics
  NetworkMapper.cs    Neural network I/O mapping with pre-allocated buffers
  MainWindow.xaml     UI layout with dark HUD overlay
AiFun.Tests/        xUnit test suite
BenchmarkSuite1/    Performance benchmarks
docs/               Design notes and roadmap
```

## License

This project does not currently have a license. All rights reserved.
