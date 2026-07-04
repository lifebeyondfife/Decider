using System.Linq;

using BenchmarkDotNet.Running;

namespace Decider.Performance;

internal class Program
{
	private static void Main(string[] args)
	{
		if (args.Length > 0 && args[0] == "calibrate")
		{
			Calibration.CalibrationRunner.Run(args.Skip(1).ToList());
			return;
		}

		BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
	}
}
