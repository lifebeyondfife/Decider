# Calibration Instance Shortlist

Research output for building a benchmark tier in the 30–300s band, with external ground
truth so every performance run doubles as a soundness check (a feature that incorrectly
cuts search space fails the run instead of reporting a flattering time).

Two problem families were selected: **RCPSP** (PSPLib — exercises `CumulativeInteger`,
parser already exists in `PspLibParser`) and **Job Shop Scheduling** (JSPLIB — exercises
`DisjunctiveInteger`, parser needed, format is trivial). Both are hard industrial problems
with published, per-instance proven optima.

## Difficulty evidence

- **j30**: all 480 instances closed. The PSPLib optimum file (Demeulemeester/Herroelen)
  includes per-instance CPU time from the original 1995 branch-and-bound (25MHz 486),
  giving an empirical hardness ordering spanning 0.01s → 7,209s. Median is 0.02s — which
  explains the previous "6ms → intractable" cliff: blind sampling almost always draws a
  trivial instance. Parameter group 13 (high resource factor, low resource strength) is
  the hard cluster.
- **j60/j90**: PSPLib publishes lower-bound and heuristic upper-bound files; LB = UB means
  the instance is provably closed (j60: 382 closed / 28 open of those listed; j90: 375/67).
  The LB author attribution yields a four-tier ladder:
  - **T1** — closed 1996 by agreed basic LB methods (easiest)
  - **T2** — closed by Klein & Scholl / Brucker & Knust LP+CP bounds (1997–98)
  - **T3** — closed by Philippe Laborie (CP Optimizer era)
  - **T4** — closed only by Schutt, Feydy, Stuckey, Wallace's **lazy clause generation**
    solver (2009–2013). These are the canonical "clause learning is the difference"
    instances — the eventual target set for the CDCL roadmap (#158).
  Open instances (LB < UB) are unsolved after 30 years and are excluded as intractable.
- **JSSP**: ft/la/orb/abz(5,6)/ta01–10 are all closed with exact optima; difficulty is
  documented over decades (ft10 famously stayed open 25 years). Job/machine ratio drives
  hardness: 10×5 and 20×5 are easy, 10×10 moderate, 15×10 / 20×10 / 15×15 hard.
  ta11+ contain open instances and are excluded.

## RCPSP shortlist (19 probes)

| Instance | Optimum | Tier / evidence | Notes |
|---|---|---|---|
| j3010_1 | 42 | 1995 B&B: 2.5s (already in repo) | anchor; Decider solves in ms |
| j6010_1 | 85 | T1 (already in repo) | anchor |
| j309_4 | 71 | 1995 B&B: 92s | j30 mid |
| j3014_4 | 50 | 1995 B&B: 106s | j30 mid |
| j3025_3 | 76 | 1995 B&B: 115s | j30 mid |
| j3013_9 | 71 | 1995 B&B: 239s | j30 hard |
| j3029_8 | 80 | 1995 B&B: 354s | j30 hard |
| j309_1 | 83 | 1995 B&B: 423s | j30 hard |
| j3013_5 | 67 | 1995 B&B: 3,330s | j30 extreme |
| j3013_1 | 58 | 1995 B&B: 7,209s | j30 extreme |
| j601_1 | 77 | j60 T1 | |
| j602_1 | 65 | j60 T1 | |
| j601_4 | 91 | j60 T2 | |
| j6017_1 | 86 | j60 T2 | |
| j609_3 | 100 | j60 T3 (Laborie) | |
| j6025_2 | 98 | j60 T3 (Laborie) | |
| j601_7 | 72 | j60 T4 (LCG-closed) | stretch |
| j6021_1 | 103 | j60 T4 (LCG-closed) | stretch |
| j6026_2 | 66 | j60 T4 (LCG-closed) | stretch |

j90 stretch candidates if the ladder tops out too early: j901_1 (opt 73), j9037_1 (opt 110).

## JSSP shortlist (16 probes)

| Instance | Size (jobs×machines) | Optimum | Difficulty |
|---|---|---|---|
| ft06 | 6×6 | 55 | trivial |
| la01 | 10×5 | 666 | easy |
| la04 | 10×5 | 590 | easy |
| la06 | 15×5 | 926 | easy |
| la16 | 10×10 | 945 | moderate |
| la19 | 10×10 | 842 | moderate |
| ft10 | 10×10 | 930 | classic hard (open 1963–1989) |
| abz5 | 10×10 | 1234 | moderate |
| orb01 | 10×10 | 1059 | moderate-hard |
| orb02 | 10×10 | 888 | moderate |
| la21 | 15×10 | 1046 | hard |
| la24 | 15×10 | 935 | hard |
| la27 | 20×10 | 1235 | hard |
| la29 | 20×10 | 1152 | very hard (last la closed) |
| la38 | 15×15 | 1196 | very hard |
| ta01 | 15×15 | 1231 | very hard |

## Data sources

- PSPLib instances: `https://www.om-db.wi.tum.de/psplib/download_dataset.php?set={j30|j60|j90}&mode=sm&format=zip`
- PSPLib j30 optima: `https://www.om-db.wi.tum.de/psplib/download_solution.php?set=j30&mode=sm&type=opt`
- PSPLib j60/j90 bounds: same URL pattern with `type=lb` (lower bounds + closed markers) and `type=hrs` (upper bounds)
- JSSP instances + optima: `https://github.com/tamy0612/JSPLIB` (raw files at
  `https://raw.githubusercontent.com/tamy0612/JSPLIB/master/instances/<name>`; optima in `instances.json`)
- JSSP format: first line `jobs machines`, then one line per job of `(machine, duration)` pairs;
  operations run in listed order, one machine holds one job at a time, minimise makespan.

## Probe protocol (implementation phase, not yet built)

1. **Transcription validation**: run every vendored instance through OR-Tools CP-SAT once
   (cap 30s each); the model must reproduce the published optimum, or the data file /
   model translation is wrong. Catches accidentally-trivial or infeasible transcriptions.
2. **Probe sweep**: run each instance through Decider with a hard 60s cap
   (`CancellationToken`; `Search()`/`SearchAllSolutions()` need token support added).
   Record: optimum proven?, best incumbent, gap to known optimum, backtracks, backtracks/sec.
   Worst-case sweep cost: 35 probes × 60s ≈ **35 minutes**, guaranteed by construction.
3. **Classification**: proven < 10s → too easy; proven 10–60s → re-probe with 400s cap to
   place precisely; not proven but gap ≤ ~5% → near-band candidate, re-probe with 400s cap;
   not proven with large gap → too hard for the current tier (park for the CL roadmap).
4. **Fallback dials** if candidates cluster at the extremes: task-prefix truncation of a
   hard instance (take first k activities respecting precedences, sweep k), horizon
   scaling, or warm-start upper bound of optimum+k.
5. Instances landing in 30–300s become the calibrated tier, with their known optima
   asserted in the benchmark itself.

Per CLAUDE.md: probe runs always carry a hard per-run timeout and a stated total sweep
budget; full BenchmarkDotNet runs remain user-triggered.
