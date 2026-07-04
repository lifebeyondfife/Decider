/*
  Copyright © Iain McDonald 2026

  This file is part of Decider.
*/
using System.Collections.Generic;

namespace Decider.Performance.Calibration;

public enum ProbeFamily
{
	Rcpsp,
	Jssp
}

public class ProbeInstance
{
	public string Name { get; private set; }
	public ProbeFamily Family { get; private set; }
	public string FileName { get; private set; }
	public int KnownOptimum { get; private set; }
	public string DifficultyEvidence { get; private set; }

	public ProbeInstance(string name, ProbeFamily family, string fileName, int knownOptimum, string difficultyEvidence)
	{
		this.Name = name;
		this.Family = family;
		this.FileName = fileName;
		this.KnownOptimum = knownOptimum;
		this.DifficultyEvidence = difficultyEvidence;
	}
}

public static class ProbeCatalogue
{
	public static IList<ProbeInstance> Instances { get; } = new List<ProbeInstance>
	{
		new ProbeInstance("j3010_1", ProbeFamily.Rcpsp, "j3010_1.sm", 42, "1995 B&B: 2.5s (anchor)"),
		new ProbeInstance("j309_4", ProbeFamily.Rcpsp, "j309_4.sm", 71, "1995 B&B: 92s"),
		new ProbeInstance("j3014_4", ProbeFamily.Rcpsp, "j3014_4.sm", 50, "1995 B&B: 106s"),
		new ProbeInstance("j3025_3", ProbeFamily.Rcpsp, "j3025_3.sm", 76, "1995 B&B: 115s"),
		new ProbeInstance("j3013_9", ProbeFamily.Rcpsp, "j3013_9.sm", 71, "1995 B&B: 239s"),
		new ProbeInstance("j3029_8", ProbeFamily.Rcpsp, "j3029_8.sm", 80, "1995 B&B: 354s"),
		new ProbeInstance("j309_1", ProbeFamily.Rcpsp, "j309_1.sm", 83, "1995 B&B: 423s"),
		new ProbeInstance("j3013_5", ProbeFamily.Rcpsp, "j3013_5.sm", 67, "1995 B&B: 3330s"),
		new ProbeInstance("j3013_1", ProbeFamily.Rcpsp, "j3013_1.sm", 58, "1995 B&B: 7209s"),
		new ProbeInstance("j6010_1", ProbeFamily.Rcpsp, "j6010_1.sm", 85, "j60 T1 (anchor)"),
		new ProbeInstance("j601_1", ProbeFamily.Rcpsp, "j601_1.sm", 77, "j60 T1"),
		new ProbeInstance("j602_1", ProbeFamily.Rcpsp, "j602_1.sm", 65, "j60 T1"),
		new ProbeInstance("j601_4", ProbeFamily.Rcpsp, "j601_4.sm", 91, "j60 T2"),
		new ProbeInstance("j6017_1", ProbeFamily.Rcpsp, "j6017_1.sm", 86, "j60 T2"),
		new ProbeInstance("j609_3", ProbeFamily.Rcpsp, "j609_3.sm", 100, "j60 T3 (Laborie)"),
		new ProbeInstance("j6025_2", ProbeFamily.Rcpsp, "j6025_2.sm", 98, "j60 T3 (Laborie)"),
		new ProbeInstance("j601_7", ProbeFamily.Rcpsp, "j601_7.sm", 72, "j60 T4 (LCG-closed)"),
		new ProbeInstance("j6021_1", ProbeFamily.Rcpsp, "j6021_1.sm", 103, "j60 T4 (LCG-closed)"),
		new ProbeInstance("j6026_2", ProbeFamily.Rcpsp, "j6026_2.sm", 66, "j60 T4 (LCG-closed)"),

		new ProbeInstance("ft06", ProbeFamily.Jssp, "ft06.txt", 55, "6x6 trivial"),
		new ProbeInstance("la01", ProbeFamily.Jssp, "la01.txt", 666, "10x5 easy"),
		new ProbeInstance("la04", ProbeFamily.Jssp, "la04.txt", 590, "10x5 easy"),
		new ProbeInstance("la06", ProbeFamily.Jssp, "la06.txt", 926, "15x5 easy"),
		new ProbeInstance("la16", ProbeFamily.Jssp, "la16.txt", 945, "10x10 moderate"),
		new ProbeInstance("la19", ProbeFamily.Jssp, "la19.txt", 842, "10x10 moderate"),
		new ProbeInstance("ft10", ProbeFamily.Jssp, "ft10.txt", 930, "10x10 classic hard"),
		new ProbeInstance("abz5", ProbeFamily.Jssp, "abz5.txt", 1234, "10x10 moderate"),
		new ProbeInstance("orb01", ProbeFamily.Jssp, "orb01.txt", 1059, "10x10 moderate-hard"),
		new ProbeInstance("orb02", ProbeFamily.Jssp, "orb02.txt", 888, "10x10 moderate"),
		new ProbeInstance("la21", ProbeFamily.Jssp, "la21.txt", 1046, "15x10 hard"),
		new ProbeInstance("la24", ProbeFamily.Jssp, "la24.txt", 935, "15x10 hard"),
		new ProbeInstance("la27", ProbeFamily.Jssp, "la27.txt", 1235, "20x10 hard"),
		new ProbeInstance("la29", ProbeFamily.Jssp, "la29.txt", 1152, "20x10 very hard"),
		new ProbeInstance("la38", ProbeFamily.Jssp, "la38.txt", 1196, "15x15 very hard"),
		new ProbeInstance("ta01", ProbeFamily.Jssp, "ta01.txt", 1231, "15x15 very hard")
	};
}
