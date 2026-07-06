# Calibration Sweep Results — 3 July 2026

All runs on the `calibration-benchmark-tier` branch via `dotnet run -c Release -- calibrate <mode>`,
every run under a hard per-run timeout with a stated worst-case sweep budget (per CLAUDE.md).

## 1. CP-SAT transcription validation (35 instances, 30s caps; re-runs at 300s)

33 of 35 instances validated by local CP-SAT proof: model + data reproduce the published
optimum exactly. Two could not be proven locally within 300s even with 8 workers
(j6025_2, la29) — their transcriptions are consistent (incumbents within 0.1–0.7% above
the published optimum, never below); their optima stand on PSPLib LB=UB closure and the
JSSP literature respectively.

**Validation caught a corrupted data file**: the repo's pre-existing `j6010_1.sm` was not
the official PSPLib instance (different basedata, precedences, demands, capacities) —
CP-SAT proved makespan 78 against the published optimum of 85. Replaced with the official
archive copy, which validates at 85. The other pre-existing files (j3010_1, j3020_10)
match the official archives.

## 2. Decider probe sweep (35 instances × 60s cap)

| Instance | Family | Evidence | Known | Status | Incumbent | Gap % | Backtracks | Time (s) | Sound |
|---|---|---|---|---|---|---|---|---|---|
| j3010_1 | Rcpsp | 1995 B&B: 2.5s (anchor) | 42 | PROVEN | 42 | 0.0 | 2 | 0.1 | OK |
| j309_4 | Rcpsp | 1995 B&B: 92s | 71 | INCUMBENT | 73 | 2.8 | 882,846 | 60.0 | OK |
| j3014_4 | Rcpsp | 1995 B&B: 106s | 50 | PROVEN | 50 | 0.0 | 82,752 | 4.9 | OK |
| j3025_3 | Rcpsp | 1995 B&B: 115s | 76 | INCUMBENT | 86 | 13.2 | 677,502 | 60.0 | OK |
| j3013_9 | Rcpsp | 1995 B&B: 239s | 71 | INCUMBENT | 72 | 1.4 | 1,353,736 | 60.0 | OK |
| j3029_8 | Rcpsp | 1995 B&B: 354s | 80 | INCUMBENT | 85 | 6.2 | 1,169,054 | 60.0 | OK |
| j309_1 | Rcpsp | 1995 B&B: 423s | 83 | NOSOLUTION | - | - | 1,108,521 | 60.0 | OK |
| j3013_5 | Rcpsp | 1995 B&B: 3330s | 67 | INCUMBENT | 72 | 7.5 | 779,595 | 60.0 | OK |
| j3013_1 | Rcpsp | 1995 B&B: 7209s | 58 | INCUMBENT | 63 | 8.6 | 1,503,736 | 60.0 | OK |
| j6010_1 | Rcpsp | j60 T1 (anchor) | 85 | PROVEN | 85 | 0.0 | 0 | 0.0 | OK |
| j601_1 | Rcpsp | j60 T1 | 77 | PROVEN | 77 | 0.0 | 0 | 0.0 | OK |
| j602_1 | Rcpsp | j60 T1 | 65 | PROVEN | 65 | 0.0 | 0 | 0.0 | OK |
| j601_4 | Rcpsp | j60 T2 | 91 | PROVEN | 91 | 0.0 | 11 | 0.0 | OK |
| j6017_1 | Rcpsp | j60 T2 | 86 | PROVEN | 86 | 0.0 | 19,849 | 9.3 | OK |
| j609_3 | Rcpsp | j60 T3 (Laborie) | 100 | INCUMBENT | 125 | 25.0 | 191,686 | 60.0 | OK |
| j6025_2 | Rcpsp | j60 T3 (Laborie) | 98 | NOSOLUTION | - | - | 314,612 | 60.0 | OK |
| j601_7 | Rcpsp | j60 T4 (LCG-closed) | 72 | NOSOLUTION | - | - | 141,633 | 60.0 | OK |
| j6021_1 | Rcpsp | j60 T4 (LCG-closed) | 103 | INCUMBENT | 117 | 13.6 | 668,830 | 60.0 | OK |
| j6026_2 | Rcpsp | j60 T4 (LCG-closed) | 66 | PROVEN | 66 | 0.0 | 3,413 | 2.7 | OK |
| ft06 | Jssp | 6x6 trivial | 55 | PROVEN | 55 | 0.0 | 194 | 0.0 | OK |
| la01 | Jssp | 10x5 easy | 666 | INCUMBENT | 667 | 0.2 | 5,105,074 | 60.0 | OK |
| la04 | Jssp | 10x5 easy | 590 | INCUMBENT | 616 | 4.4 | 1,255,316 | 60.0 | OK |
| la06 | Jssp | 15x5 easy | 926 | PROVEN | 926 | 0.0 | 246 | 0.1 | OK |
| la16 | Jssp | 10x10 moderate | 945 | INCUMBENT | 1061 | 12.3 | 1,027,481 | 60.0 | OK |
| la19 | Jssp | 10x10 moderate | 842 | PROVEN | 905 | 7.5 | 1,920 | 0.8 | **UNSOUND** |
| ft10 | Jssp | 10x10 classic hard | 930 | PROVEN | 1083 | 16.5 | 353 | 0.4 | **UNSOUND** |
| abz5 | Jssp | 10x10 moderate | 1234 | INCUMBENT | 1339 | 8.5 | 1,426,447 | 60.0 | OK |
| orb01 | Jssp | 10x10 moderate-hard | 1059 | PROVEN | 1299 | 22.7 | 325,125 | 49.9 | **UNSOUND** |
| orb02 | Jssp | 10x10 moderate | 888 | INCUMBENT | 995 | 12.0 | 1,219,933 | 60.0 | OK |
| la21 | Jssp | 15x10 hard | 1046 | INCUMBENT | 1229 | 17.5 | 1,797,895 | 60.0 | OK |
| la24 | Jssp | 15x10 hard | 935 | PROVEN | 1049 | 12.2 | 2,573 | 2.7 | **UNSOUND** |
| la27 | Jssp | 20x10 hard | 1235 | INCUMBENT | 1800 | 45.7 | 739,361 | 60.0 | OK |
| la29 | Jssp | 20x10 very hard | 1152 | PROVEN | 1725 | 49.7 | 2,605 | 1.4 | **UNSOUND** |
| la38 | Jssp | 15x15 very hard | 1196 | INCUMBENT | 1435 | 20.0 | 348,521 | 60.0 | OK |
| ta01 | Jssp | 15x15 very hard | 1231 | PROVEN | 1407 | 14.3 | 210,856 | 53.1 | **UNSOUND** |

### RCPSP calibration outcome

The tier structure worked exactly as designed:

- **Anchors/trivial**: j3010_1, all j60 T1 (0–11 backtracks, pure propagation)
- **In-band or near-band proofs**: j3014_4 (4.9s), j6017_1 (9.3s), j6026_2 (2.7s) —
  candidates for the 30–300s tier via re-probe at 400s
- **Near-band incumbents** (likely provable in 30–300s): j3013_9 (gap 1.4%), j309_4 (2.8%)
- **Above band, measurable gap**: j3029_8, j3013_5, j3013_1, j6021_1, j609_3 — headroom
  metrics for future solver improvements
- **Cliff markers**: j309_1, j6025_2, j601_7 (no incumbent in 60s) — the eventual
  clause-learning acceptance set, alongside the j60 T4 tier

### JSSP outcome: completeness bug discovered

Six instances returned **PROVEN at values above the published optimum** — Decider's
search claimed optimality while strictly better schedules exist (verified: CP-SAT proves
them and the transcription was validated in step 1). This is precisely the historical
failure mode 2(b): search space incorrectly cut, valid solutions omitted.

## 3. Bug diagnosis

Decision-mode probes (post `makespan <= knownOptimum`, ask for any solution):

- la19 at ≤ 842 → **Unsatisfiable** (false: CP-SAT proves 842 feasible). Confirms in-search
  pruning of valid solutions, independent of the optimisation loop.

Three disjunction encodings, bisected on job-prefixes with per-prefix CP-SAT ground truth:

| Encoding | Smallest failing case | Verdict |
|---|---|---|
| `DisjunctiveInteger` (Global) | la19 8-job prefix (false UNSAT at 801); all ft06 prefixes correct | unsound, needs 8×10 structure |
| Pairwise `\|` or-expressions | ft06 2-job prefix (false UNSAT at 47) | unsound, minimal repro |
| Big-M linear with 0/1 selector | ft06 4-job prefix (false UNSAT at 47) | unsound |

Further isolation:

- **Micro-tests pass**: single or-constraint and single big-M pair over small domains
  match brute-force solution counts exactly — the primitives are sound in isolation;
  the failures need chained precedence + bound structure to trigger.
- **Fixed-schedule verification**: fixing every start variable to CP-SAT's optimal k=8
  la19 schedule, Decider's constraint network (including `DisjunctiveInteger`) **accepts**
  it — `Check`/final propagation are correct. The false UNSAT therefore arises from
  **dynamic pruning during search** (constraint filtering on partial assignments and/or
  the backjumping machinery), not from the constraint's accept/reject semantics.
- **Cumulative-capacity-1 discriminator**: (results below) a disjunctive machine is a
  cumulative resource with capacity 1; `CumulativeInteger` was sound on all RCPSP probes.
  If the swap fixes la19-k8, `DisjunctiveInteger` filtering is convicted; if not, the
  core search (e.g. backjumping) is implicated.

### Verdict: unsound conflict-directed backjumping

- **Cumulative discriminator**: replacing each machine's `DisjunctiveInteger` with
  `CumulativeInteger` (capacity 1, unit demands — semantically identical, and sound on
  every RCPSP probe) reproduces the **identical** false-UNSAT pattern on la19 (k=8 UNSAT,
  k=9 timeout, k=10 UNSAT). Four independent encodings failing identically exonerates the
  individual constraints and implicates the shared search machinery.
- **Backjumping toggle**: with a new `StateInteger.BackjumpingEnabled = false`,
  the or-expression and big-M encodings solve **every** ft06 prefix correctly (previously
  false-UNSAT from k=2 / k=4), and la19's false UNSATs become inconclusive timeouts
  (chronological search is far slower — backjumping was a large speed win, but unsound).

**Root cause**: `ComputeConflictDepth` (StateInteger.cs) derives the backjump target as
the deepest *assignment* among the violated constraint's **own** variables only. A sound
jump target must consider the full conflict set — decisions on other variables whose
effects reached the violated constraint through propagation (precedence chains, bound
propagation from the makespan constraint, disjunctive filtering). When the true conflict
depends on such a decision, the jump skips decision levels whose untried alternatives
contained valid (including optimal) solutions. The `DepthConflictAccumulator` machinery
then propagates the too-shallow target on unwind, compounding the effect.

This also explains the historical pattern of features "incorrectly cutting search space,
omitting valid solutions" (rollback reason 2b): the unsoundness is intermittent and
structure-dependent (all 157 unit/acceptance tests pass; NQueens/RCPSP appear unaffected;
JSSP-like propagation-chain structures trigger it), so it surfaced as unexplained wrong
results in whatever feature was under test at the time.

**Affected results**: the six UNSOUND JSSP probe rows above. The RCPSP rows stand: every
RCPSP PROVEN matched its published optimum (verified sound post-hoc by ground truth), and
INCUMBENT rows are valid best-found-in-60s measurements.

## 4. Re-probes at 400s (near-band candidates)

| Instance | Known | Status | Incumbent | Gap % | Backtracks | Time (s) |
|---|---|---|---|---|---|---|
| j3013_9 | 71 | INCUMBENT | 72 | 1.4 | 10,159,407 | 400 |
| j309_4 | 71 | INCUMBENT | 72 | 1.4 | 7,130,889 | 400 |
| j3029_8 | 80 | INCUMBENT | 85 | 6.2 | 11,678,710 | 400 |

None proved at 400s. For the current solver, RCPSP proofs land either under ~10s or beyond
400s — the historical cliff, now precisely bracketed with gap metrics instead of binary
timeouts. Perspective: the 1995 branch-and-bound proved j309_4 in 92s on a 25MHz 486;
Decider cannot within 400s on modern hardware. That quantifies the search-strength gap
independently of hardware and gives the improvement roadmap sharp, measurable targets.

## Calibrated tier recommendation

- **Regression guards (assert optimum, fast)**: j3010_1, j601_1/j602_1/j601_4 (propagation-
  solved), j3014_4 (4.9s, 82,752 backtracks), j6026_2 (2.7s), j6017_1 (9.3s), ft06, la06
- **Frontier targets (assert no UNSOUND; track gap % and time-to-proof)**: j309_4 and
  j3013_9 (prove 71 — nearest rungs above current capability), j3029_8, j3013_5, j3013_1,
  j6021_1, la01 (JSSP, gap 0.2% — once backjumping is fixed)
- **Clause-learning acceptance set (currently NOSOLUTION/large gap)**: j309_1, j6025_2,
  j601_7, j609_3 + the j60 T4 tier — instances closed in the literature only by
  lazy-clause-generation solvers
- **Blocked on backjumping fix**: the whole JSSP family for optimisation use; JSSP rows
  currently serve as the bug's regression tests
- The 30–300s band is currently empty for proofs; the job-prefix truncation dial
  (`bisect` machinery) can synthesise intermediate rungs once the frontier starts moving.

## Follow-ups

- Backjumping unsoundness: GitHub issue [#164](https://github.com/lifebeyondfife/Decider/issues/164); fix options are
  disable-by-default or a real conflict-set jump level via the clause-learning
  explanation infrastructure (#158 dovetail)
- j6025_2 and la29 remain validated-by-consistency only (local CP-SAT could not re-prove
  within 300s); optima stand on PSPLib LB=UB closure and the JSSP literature

## Reproduction commands

```bash
dotnet run -c Release -- calibrate validate                      # CP-SAT transcription check
dotnet run -c Release -- calibrate probe                         # full Decider sweep, 60s caps
dotnet run -c Release -- calibrate decision --instance la19      # false-UNSAT repro
dotnet run -c Release -- calibrate bisect --instance la19 --encoding global
dotnet run -c Release -- calibrate verify --instance la19 --jobs 8
dotnet run -c Release -- calibrate micro
```

---

# Post-fix re-baseline — 5 July 2026

Issue [#164](https://github.com/lifebeyondfife/Decider/issues/164) fixed. The unsound
`ComputeConflictDepth`/`DepthConflictAccumulator` backjumping (jump target from the violated
constraint's own variables only) is removed entirely. The default search backtracks
chronologically. With `ClauseLearningEnabled`, conflict analysis now drives a sound
CDCL-style backjump: the analyser reports the assertion level and whether the learned
clause is asserting (exactly one literal at the conflict level); `BackjumpToLevel` unwinds
to the assertion level, asserts the unit literal with an eager explanation, and lets
unit propagation take over. `BackjumpingEnabled` now gates only this sound path and is
inert without clause learning. Backjumping is bypassed during `SearchAllSolutions`
(a jump's unwind restores refutation prunes of already-enumerated subtrees, producing
duplicate solutions; learning stays active, backtracking is chronological).

Wiring the backjump exposed four latent bugs in the clause-learning infrastructure, all
fixed and each confirmed by a witness-tracing harness against known NQueens solutions:

1. **`DomainTrail.Backtrack` multi-level restore skipped** — when the level immediately
   above the target had no recorded domain change, it returned without restoring deeper
   levels. Harmless chronologically (one level at a time), state-corrupting on a
   multi-level backjump. Now scans forward to the first marked level.
2. **`AllDifferentInteger` Hall-set explanations unsound** — the per-variable explanation
   cache was never cleared (reasons accumulated across propagations and branches) and the
   SCC-based reason sets could claim false implications (e.g. `v3∈[1,5] ⟹ v3=4`).
   `Explain` now returns the pre-propagation bounds of all the constraint's variables —
   sound because clause-learning mode keeps domains hole-free. Precise Hall-set
   explanations remain future work (#160); the dead witness-generation machinery from
   #154 is removed.
3. **Clause-store notifications interleaved with propagation recording** — a forced
   literal fired mid-loop in `RecordBoundChanges` could change other variables of the
   same constraint, whose changes were then re-recorded with a false constraint
   explanation. Recording now completes before any notification.
4. **Refutation prunes invisible to conflict analysis** — the value removed after an
   exhausted subtree was never recorded on the propagation trail, so
   `FindDecisionLevel` mislabelled facts derived from it as level 0 and computed
   too-shallow assertion levels (the livelock seen when the backjump was first wired).
   `BackTrackVariable` now records the resulting bound change as a decision-kind entry.

If conflict analysis ever produces a non-asserting clause at its computed assertion
level (impossible with a consistent trail), `BackjumpToLevel` throws `DeciderException`
rather than silently continuing (livelock) or cutting search space (unsound).

## Acceptance (all criteria from #164 met)

- `calibrate bisect --instance ft06` (30s caps): every prefix × every encoding reads
  `Solved(<CP-SAT optimum>)` — previously false-UNSAT from k=2 (pairwise) / k=4 (big-M).
- `calibrate bisect --instance la19 --encoding global` (30s caps): k=2–6
  `Solved(<CP-SAT optimum>)`, k=7–10 honest timeouts — the k=8/k=10 false UNSATs at
  801/842 are gone.
- `calibrate probe --family jssp` (60s caps): **zero UNSOUND rows** (table below).
- NQueens `SearchAllSolutions` counts, n=8–11: 92 / 352 / 724 / 2,680 — exact solution
  sets (not just counts) identical across default, clause-learning, and
  clause-learning+backjumping modes, no duplicates.
- Full test suite: 156 passing (147 unit + 9 acceptance), including the exact NQueens
  backtrack counts of the chronological search — evidence the removed backjump never
  actually fired on NQueens/RCPSP structures, which is why the unsoundness hid there.

## JSSP probe sweep post-fix (16 instances × 60s cap)

| Instance | Known | Status | Incumbent | Gap % | Backtracks | Time (s) | Sound |
|---|---|---|---|---|---|---|---|
| ft06 | 55 | PROVEN | 55 | 0.0 | 194 | 0.1 | OK |
| la01 | 666 | INCUMBENT | 667 | 0.2 | 5,088,463 | 60.0 | OK |
| la04 | 590 | INCUMBENT | 627 | 6.3 | 3,867,118 | 60.0 | OK |
| la06 | 926 | PROVEN | 926 | 0.0 | 246 | 0.1 | OK |
| la16 | 945 | INCUMBENT | 1079 | 14.2 | 1,697,466 | 60.0 | OK |
| la19 | 842 | INCUMBENT | 893 | 6.1 | 339,121 | 60.0 | OK |
| ft10 | 930 | INCUMBENT | 1021 | 9.8 | 1,000,958 | 60.0 | OK |
| abz5 | 1234 | INCUMBENT | 1373 | 11.3 | 1,245,936 | 60.0 | OK |
| orb01 | 1059 | INCUMBENT | 1305 | 23.2 | 1,061,802 | 60.0 | OK |
| orb02 | 888 | INCUMBENT | 1011 | 13.9 | 399,163 | 60.0 | OK |
| la21 | 1046 | INCUMBENT | 1276 | 22.0 | 130,590 | 60.0 | OK |
| la24 | 935 | INCUMBENT | 1101 | 17.8 | 1,817,109 | 60.0 | OK |
| la27 | 1235 | INCUMBENT | 1800 | 45.7 | 748,895 | 60.0 | OK |
| la29 | 1152 | INCUMBENT | 1725 | 49.7 | 605,457 | 60.0 | OK |
| la38 | 1196 | INCUMBENT | 1437 | 20.2 | 342,332 | 60.0 | OK |
| ta01 | 1231 | INCUMBENT | 1407 | 14.3 | 145,780 | 60.0 | OK |

The six instances that previously claimed PROVEN above the published optimum (la19,
ft10, orb01, la24, la29, ta01) are all honest incumbents now. The JSSP family is
**unquarantined**: these rows are the sound baseline for the clause-learning work
(#158/#159/#160). la01 at gap 0.2% is the nearest JSSP frontier target.

## RCPSP probe sweep post-fix (19 instances × 60s cap)

| Instance | Known | Status | Incumbent | Gap % | Backtracks | Time (s) | Sound |
|---|---|---|---|---|---|---|---|
| j3010_1 | 42 | PROVEN | 42 | 0.0 | 2 | 0.1 | OK |
| j309_4 | 71 | INCUMBENT | 73 | 2.8 | 850,531 | 60.0 | OK |
| j3014_4 | 50 | PROVEN | 50 | 0.0 | 82,752 | 5.0 | OK |
| j3025_3 | 76 | INCUMBENT | 86 | 13.2 | 672,258 | 60.0 | OK |
| j3013_9 | 71 | INCUMBENT | 72 | 1.4 | 1,394,074 | 60.0 | OK |
| j3029_8 | 80 | INCUMBENT | 85 | 6.2 | 1,204,726 | 60.0 | OK |
| j309_1 | 83 | NOSOLUTION | - | - | 1,124,598 | 60.0 | OK |
| j3013_5 | 67 | INCUMBENT | 72 | 7.5 | 792,495 | 60.0 | OK |
| j3013_1 | 58 | INCUMBENT | 63 | 8.6 | 1,524,397 | 60.0 | OK |
| j6010_1 | 85 | PROVEN | 85 | 0.0 | 0 | 0.0 | OK |
| j601_1 | 77 | PROVEN | 77 | 0.0 | 0 | 0.0 | OK |
| j602_1 | 65 | PROVEN | 65 | 0.0 | 0 | 0.0 | OK |
| j601_4 | 91 | PROVEN | 91 | 0.0 | 11 | 0.0 | OK |
| j6017_1 | 86 | PROVEN | 86 | 0.0 | 19,849 | 9.1 | OK |
| j609_3 | 100 | INCUMBENT | 125 | 25.0 | 194,503 | 60.0 | OK |
| j6025_2 | 98 | NOSOLUTION | - | - | 316,792 | 60.0 | OK |
| j601_7 | 72 | NOSOLUTION | - | - | 143,513 | 60.0 | OK |
| j6021_1 | 103 | INCUMBENT | 117 | 13.6 | 680,366 | 60.0 | OK |
| j6026_2 | 66 | PROVEN | 66 | 0.0 | 3,413 | 2.7 | OK |

Statistically identical to the 3 July sweep, row for row: same proofs at the same
optima in the same times, same incumbents, same cliff markers. Removing the unsound
backjump cost RCPSP nothing — direct evidence the old jump never actually fired on
these structures. The calibrated tier recommendation from 3 July stands unchanged,
with la01 (gap 0.2%) added as the first sound JSSP frontier target.

## Reproduction commands

```bash
dotnet run -c Release -- calibrate bisect --instance ft06
dotnet run -c Release -- calibrate bisect --instance la19 --encoding global
dotnet run -c Release -- calibrate probe --family jssp
dotnet run -c Release -- calibrate probe --family rcpsp
```
