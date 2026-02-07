using BenchmarkDotNet.Running;

namespace Decider.Performance
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
		}
	}
}
