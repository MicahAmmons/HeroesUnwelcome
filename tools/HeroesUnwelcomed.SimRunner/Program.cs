using System.Diagnostics;
using System.Globalization;
using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.SimRunner;

// SimRunner: plays many complete runs with no graphics and writes a balance report.
//
//   --runs 200            runs per ordering rule (seeds 1..N)
//   --max-beats 6000      stop a run here if the heroes still haven't won ("the dungeon held")
//   --rules ladder,reputation
//   --data <folder>       game data folder (default: the repo's data/game)
//   --out <folder>        where the report goes (default: the repo's sim-output/)

var options = ParseArgs(args);
string repoRoot = FindRepoRoot();
string dataFolder = options.GetValueOrDefault("data") ?? Path.Combine(repoRoot, "data", "game");
string outFolder = options.GetValueOrDefault("out") ?? Path.Combine(repoRoot, "sim-output");
int runs = int.Parse(options.GetValueOrDefault("runs") ?? "200", CultureInfo.InvariantCulture);
double maxBeats = double.Parse(options.GetValueOrDefault("max-beats") ?? "6000", CultureInfo.InvariantCulture);
string[] rules = (options.GetValueOrDefault("rules") ?? "ladder,reputation").Split(',', StringSplitOptions.TrimEntries);
const double sampleEvery = 50;

GameDefinitions definitions = GameDefinitions.Parse(
    File.ReadAllText(Path.Combine(dataFolder, "rooms.json")),
    File.ReadAllText(Path.Combine(dataFolder, "tuning.json")),
    File.ReadAllText(Path.Combine(dataFolder, "heroes.json")),
    File.Exists(Path.Combine(dataFolder, "classes.json")) ? File.ReadAllText(Path.Combine(dataFolder, "classes.json")) : null);
RunSetup setup = GameDefinitions.ParseSetup(File.ReadAllText(Path.Combine(dataFolder, "run-setup.json")));

Console.WriteLine($"Simulating {runs} runs x {rules.Length} rules (up to {maxBeats} beats each) using {dataFolder}");
var stopwatch = Stopwatch.StartNew();
var batch = new SimulationBatch(definitions, setup, maxBeats, sampleEvery);

// Runs are independent, so they can use every CPU core. Each run is still
// fully deterministic: its seed alone decides everything that happens in it.
var results = rules
    .SelectMany(rule => Enumerable.Range(1, runs).Select(seed => (rule, seed: (ulong)seed)))
    .AsParallel()
    .Select(job => batch.Play(job.rule, job.seed))
    .ToList()
    .OrderBy(r => Array.IndexOf(rules, r.Rule)).ThenBy(r => r.Seed)
    .ToList();

Console.WriteLine($"Done in {stopwatch.Elapsed.TotalSeconds:0.0}s");
foreach (string rule in rules)
{
    var mine = results.Where(r => r.Rule == rule).ToList();
    var won = mine.Where(r => r.HeroesWon).ToList();
    Console.WriteLine(
        $"  {rule,-11} heroes won {won.Count}/{mine.Count}" +
        (won.Count > 0 ? $", median {Report.Median(won.Select(r => r.LengthBeats)):0} beats" : "") +
        $", best-pick {mine.Average(r => r.BestPickRate):P0}, avg deaths {mine.Average(r => r.Deaths):0}");
}

Directory.CreateDirectory(outFolder);
Report.WriteCsv(results, outFolder);
string html = Report.WriteHtml(results, rules, maxBeats, sampleEvery, outFolder);
Console.WriteLine($"Report: {html}");
return 0;

static Dictionary<string, string> ParseArgs(string[] args)
{
    var parsed = new Dictionary<string, string>();
    for (int i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length)
        {
            throw new ArgumentException($"Expected '--name value', got '{args[i]}'.");
        }
        parsed[args[i][2..]] = args[++i];
    }
    return parsed;
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "HeroesUnwelcomed.slnx")))
    {
        dir = dir.Parent;
    }
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}
