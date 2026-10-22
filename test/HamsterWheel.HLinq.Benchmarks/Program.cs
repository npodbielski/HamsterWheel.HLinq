using BenchmarkDotNet.Running;

namespace HamsterWheel.HLinq.Benchmarks;

internal static class Program
{
    private static void Main(string[] args) =>
        // benchmark classes carry [ShortRunJob], run e.g. `--filter *Tokenizer*` to narrow down
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
