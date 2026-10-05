using System.Globalization;
using System.Text;
using System.Text.Json;

namespace HeroesUnwelcomed.SimRunner;

/// <summary>Turns simulation results into CSV files and a self-contained HTML chart report.</summary>
public static class Report
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToList();
        if (sorted.Count == 0) return 0;
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    }

    public static void WriteCsv(IReadOnlyList<RunResult> results, string folder)
    {
        var runs = new StringBuilder("rule,seed,heroes_won,length_beats,parties,deaths,souls_earned,best_pick_rate\n");
        foreach (RunResult r in results)
        {
            runs.Append(Invariant, $"{r.Rule},{r.Seed},{r.HeroesWon},{r.LengthBeats},{r.Parties},{r.Deaths},{r.SoulsEarned},{r.BestPickRate:0.####}\n");
        }
        File.WriteAllText(Path.Combine(folder, "runs.csv"), runs.ToString());

        var samples = new StringBuilder("rule,seed,beat,avg_combat_skill,avg_trap_skill,avg_boss_skill,souls\n");
        foreach (RunResult r in results)
        {
            foreach (Sample s in r.Samples)
            {
                samples.Append(Invariant, $"{r.Rule},{r.Seed},{s.Beat},{s.CombatSkill:0.###},{s.TrapSkill:0.###},{s.BossSkill:0.###},{s.Souls}\n");
            }
        }
        File.WriteAllText(Path.Combine(folder, "samples.csv"), samples.ToString());
    }

    public static string WriteHtml(IReadOnlyList<RunResult> results, IReadOnlyList<string> rules, double maxBeats, double sampleEvery, string folder)
    {
        var kinds = results.SelectMany(r => r.ByRoomKind.Keys).Distinct().Order().ToList();

        var data = new
        {
            generated = DateTime.Now.ToString("yyyy-MM-dd HH:mm", Invariant),
            maxBeats,
            rules = rules.Select(rule =>
            {
                var mine = results.Where(r => r.Rule == rule).ToList();
                var won = mine.Where(r => r.HeroesWon).ToList();
                return new
                {
                    name = rule,
                    runs = mine.Count,
                    won = won.Count,
                    medianBeats = won.Count > 0 ? Math.Round(Median(won.Select(r => r.LengthBeats))) : (double?)null,
                    bestPick = Math.Round(mine.Average(r => r.BestPickRate), 4),
                    avgDeaths = Math.Round(mine.Average(r => r.Deaths), 1),
                    avgSouls = Math.Round(mine.Average(r => r.SoulsEarned), 1),
                    avgParties = Math.Round(mine.Average(r => r.Parties), 1),
                    survival = Survival(mine, maxBeats),
                    combat = MeanCurve(mine, sampleEvery, maxBeats, s => s.CombatSkill),
                    trap = MeanCurve(mine, sampleEvery, maxBeats, s => s.TrapSkill),
                    kills = kinds.Select(kind =>
                    {
                        int attempts = mine.Sum(r => r.ByRoomKind.TryGetValue(kind, out var k) ? k.Attempts : 0);
                        int killed = mine.Sum(r => r.ByRoomKind.TryGetValue(kind, out var k) ? k.Kills : 0);
                        return new { kind, attempts, rate = attempts == 0 ? 0 : Math.Round(killed / (double)attempts, 4) };
                    }).ToList(),
                };
            }).ToList(),
        };

        string json = JsonSerializer.Serialize(data);
        string path = Path.Combine(folder, "report.html");
        File.WriteAllText(path, HtmlTemplate.Replace("/*DATA*/null", json, StringComparison.Ordinal));
        return path;
    }

    // Share of runs the heroes still hadn't won, at each point in time.
    private static List<double[]> Survival(List<RunResult> runs, double maxBeats)
    {
        var points = new List<double[]>();
        double step = Math.Max(50, Math.Round(maxBeats / 120 / 50) * 50);
        for (double beat = 0; beat <= maxBeats; beat += step)
        {
            double holding = runs.Count(r => !r.HeroesWon || r.LengthBeats > beat) / (double)runs.Count;
            points.Add([beat, Math.Round(holding, 4)]);
        }
        return points;
    }

    // Average across the runs still going at each sample time (needs at least 5 to be meaningful).
    private static List<double[]> MeanCurve(List<RunResult> runs, double sampleEvery, double maxBeats, Func<Sample, double> pick)
    {
        var points = new List<double[]>();
        for (double beat = 0; beat <= maxBeats; beat += sampleEvery)
        {
            var values = runs
                .Select(r => r.Samples.FirstOrDefault(s => Math.Abs(s.Beat - beat) < 1e-6))
                .Where(s => s.Beat == beat && (beat == 0 || s.CombatSkill > 0))
                .Select(pick)
                .ToList();
            if (values.Count >= 5)
            {
                points.Add([beat, Math.Round(values.Average(), 4)]);
            }
        }
        return points;
    }

    // A single self-contained page: data is embedded, charts are drawn as SVG by the script below.
    private const string HtmlTemplate = """
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Balance Report</title>
<style>
  :root {
    color-scheme: light;
    --surface-0: #f4f4f1; --surface-1: #fcfcfb; --border: #e2e1dc; --grid: #ecebe7;
    --text-primary: #0b0b0b; --text-secondary: #52514e; --text-muted: #7a7974;
    --series-1: #2a78d6; --series-2: #eb6834; --series-3: #1baf7a;
  }
  @media (prefers-color-scheme: dark) {
    :root:where(:not([data-theme="light"])) {
      color-scheme: dark;
      --surface-0: #121211; --surface-1: #1a1a19; --border: #2e2e2c; --grid: #262624;
      --text-primary: #ffffff; --text-secondary: #c3c2b7; --text-muted: #8f8e86;
      --series-1: #3987e5; --series-2: #d95926; --series-3: #199e70;
    }
  }
  :root[data-theme="dark"] {
    color-scheme: dark;
    --surface-0: #121211; --surface-1: #1a1a19; --border: #2e2e2c; --grid: #262624;
    --text-primary: #ffffff; --text-secondary: #c3c2b7; --text-muted: #8f8e86;
    --series-1: #3987e5; --series-2: #d95926; --series-3: #199e70;
  }
  * { box-sizing: border-box; }
  body { margin: 0; background: var(--surface-0); color: var(--text-primary);
         font: 15px/1.5 system-ui, -apple-system, "Segoe UI", sans-serif; }
  main { max-width: 1100px; margin: 0 auto; padding: 24px 16px 48px; }
  h1 { font-size: 24px; margin: 0 0 4px; }
  h2 { font-size: 17px; margin: 0 0 2px; }
  .sub { color: var(--text-secondary); margin: 0 0 20px; }
  .note { color: var(--text-secondary); font-size: 13px; margin: 0 0 12px; }
  .card { background: var(--surface-1); border: 1px solid var(--border); border-radius: 10px; padding: 16px; margin-bottom: 16px; }
  .tiles { display: grid; grid-template-columns: repeat(auto-fit, minmax(min(240px, 100%), 1fr)); gap: 16px; margin-bottom: 16px; }
  .tile h3 { margin: 0 0 10px; font-size: 15px; display: flex; align-items: center; gap: 8px; }
  .swatch { width: 12px; height: 12px; border-radius: 3px; display: inline-block; }
  .stats { display: grid; grid-template-columns: 1fr 1fr; gap: 10px 16px; }
  .stat .v { font-size: 22px; font-weight: 600; font-variant-numeric: tabular-nums; }
  .stat .l { color: var(--text-secondary); font-size: 12px; }
  .pair { display: grid; grid-template-columns: repeat(auto-fit, minmax(min(320px, 100%), 1fr)); gap: 16px; }
  .legend { display: flex; gap: 16px; flex-wrap: wrap; color: var(--text-secondary); font-size: 13px; margin: 6px 0 4px; }
  .legend span { display: inline-flex; align-items: center; gap: 6px; }
  svg { display: block; width: 100%; height: auto; overflow: visible; }
  svg text { fill: var(--text-muted); font-size: 11px; font-family: inherit; }
  svg .label { fill: var(--text-secondary); font-size: 12px; }
  .chart { position: relative; }
  .tip { position: absolute; pointer-events: none; background: var(--surface-1); border: 1px solid var(--border);
         border-radius: 8px; padding: 8px 10px; font-size: 12px; box-shadow: 0 4px 14px rgba(0,0,0,.12);
         white-space: nowrap; display: none; z-index: 2; }
  .tip b { display: block; margin-bottom: 2px; color: var(--text-primary); }
  .tip div { color: var(--text-secondary); display: flex; align-items: center; gap: 6px; }
  .table-wrap { overflow-x: auto; }
  table { width: 100%; border-collapse: collapse; font-variant-numeric: tabular-nums; font-size: 14px; }
  th, td { text-align: right; padding: 6px 8px; border-bottom: 1px solid var(--border); }
  th:first-child, td:first-child { text-align: left; }
  th { color: var(--text-secondary); font-weight: 500; }
  @media (max-width: 600px) { .stats { grid-template-columns: 1fr 1fr; } }
</style>
</head>
<body>
<main>
  <h1>Balance report</h1>
  <p class="sub" id="sub"></p>
  <div class="tiles" id="tiles"></div>

  <section class="card">
    <h2>How long the dungeon holds</h2>
    <p class="note">Share of runs where no hero has beaten the treasure room yet, by game beat. Higher and longer is better for the player.</p>
    <div class="legend" id="legend-survival"></div>
    <div class="chart" id="survival"></div>
  </section>

  <section class="card">
    <h2>Hero skill over time</h2>
    <p class="note">Average skill of all heroes (mean across runs still going). Heroes grow most in what kills them.</p>
    <div class="legend" id="legend-skill"></div>
    <div class="pair">
      <div><h2 style="font-size:14px">Combat</h2><div class="chart" id="combat"></div></div>
      <div><h2 style="font-size:14px">Trap</h2><div class="chart" id="trap"></div></div>
    </div>
  </section>

  <section class="card">
    <h2>Kill rate by room kind</h2>
    <p class="note">Share of attempts at each room kind that ended in the hero's death.</p>
    <div class="legend" id="legend-kills"></div>
    <div class="chart" id="kills"></div>
  </section>

  <section class="card">
    <h2>Summary table</h2>
    <div class="table-wrap"><table id="table"></table></div>
  </section>
</main>

<script>
const DATA = /*DATA*/null;
const COLORS = ["var(--series-1)", "var(--series-2)", "var(--series-3)"];
const NS = "http://www.w3.org/2000/svg";
const pct = v => (v * 100).toFixed(0) + "%";
const num = v => v == null ? "–" : Number(v).toLocaleString();

function el(tag, attrs = {}, parent) {
  const node = document.createElementNS(NS, tag);
  for (const [k, v] of Object.entries(attrs)) node.setAttribute(k, v);
  if (parent) parent.appendChild(node);
  return node;
}

function legend(id) {
  document.getElementById(id).innerHTML = DATA.rules.map((r, i) =>
    `<span><i class="swatch" style="background:${COLORS[i]}"></i>${r.name}</span>`).join("");
}

function niceMax(v) { if (v <= 0) return 1; const p = Math.pow(10, Math.floor(Math.log10(v))); return Math.ceil(v / p * 2) / 2 * p; }

// Line chart with crosshair + tooltip. series: [{name, color, points:[[x,y]]}]
function lineChart(id, series, { yMax, yFormat, xFormat }) {
  const host = document.getElementById(id);
  const W = 640, H = 260, m = { l: 44, r: 16, t: 10, b: 28 };
  const xs = series.flatMap(s => s.points.map(p => p[0]));
  const xMax = Math.max(...xs, 1);
  yMax = yMax ?? niceMax(Math.max(...series.flatMap(s => s.points.map(p => p[1]))));
  const X = x => m.l + x / xMax * (W - m.l - m.r);
  const Y = y => H - m.b - y / yMax * (H - m.t - m.b);
  const svg = el("svg", { viewBox: `0 0 ${W} ${H}`, role: "img" }, host);

  for (let i = 0; i <= 4; i++) {
    const v = yMax * i / 4, y = Y(v);
    el("line", { x1: m.l, x2: W - m.r, y1: y, y2: y, stroke: "var(--grid)", "stroke-width": 1 }, svg);
    el("text", { x: m.l - 6, y: y + 4, "text-anchor": "end" }, svg).textContent = yFormat(v);
  }
  for (let i = 0; i <= 5; i++) {
    const v = xMax * i / 5;
    el("text", { x: X(v), y: H - 8, "text-anchor": "middle" }, svg).textContent = xFormat(v);
  }
  series.forEach(s => {
    if (!s.points.length) return;
    el("path", { d: s.points.map((p, i) => (i ? "L" : "M") + X(p[0]) + "," + Y(p[1])).join(""),
                 fill: "none", stroke: s.color, "stroke-width": 2, "stroke-linejoin": "round" }, svg);
  });

  // Hover layer: snap to the nearest x, show every series' value there.
  const cross = el("line", { y1: m.t, y2: H - m.b, stroke: "var(--text-muted)", "stroke-width": 1, visibility: "hidden" }, svg);
  const dots = series.map(s => el("circle", { r: 4, fill: s.color, stroke: "var(--surface-1)", "stroke-width": 2, visibility: "hidden" }, svg));
  const tip = document.createElement("div"); tip.className = "tip"; host.appendChild(tip);
  const hit = el("rect", { x: m.l, y: m.t, width: W - m.l - m.r, height: H - m.t - m.b, fill: "transparent" }, svg);
  hit.addEventListener("pointermove", ev => {
    const box = svg.getBoundingClientRect();
    const x = (ev.clientX - box.left) / box.width * W;
    const target = (x - m.l) / (W - m.l - m.r) * xMax;
    let html = "", any = false, bx = 0;
    series.forEach((s, i) => {
      if (!s.points.length) { dots[i].setAttribute("visibility", "hidden"); return; }
      const p = s.points.reduce((a, b) => Math.abs(b[0] - target) < Math.abs(a[0] - target) ? b : a);
      if (Math.abs(p[0] - target) > xMax / 20) { dots[i].setAttribute("visibility", "hidden"); return; }
      any = true; bx = X(p[0]);
      dots[i].setAttribute("cx", X(p[0])); dots[i].setAttribute("cy", Y(p[1])); dots[i].setAttribute("visibility", "visible");
      html += `<div><i class="swatch" style="background:${s.color}"></i>${s.name}: <b style="display:inline">${yFormat(p[1])}</b></div>`;
    });
    if (!any) { hide(); return; }
    cross.setAttribute("x1", bx); cross.setAttribute("x2", bx); cross.setAttribute("visibility", "visible");
    tip.innerHTML = `<b>${xFormat(target)}</b>` + html;
    tip.style.display = "block";
    const left = (bx / W) * box.width;
    tip.style.left = Math.min(left + 12, box.width - tip.offsetWidth) + "px";
    tip.style.top = "8px";
  });
  const hide = () => { cross.setAttribute("visibility", "hidden"); dots.forEach(d => d.setAttribute("visibility", "hidden")); tip.style.display = "none"; };
  hit.addEventListener("pointerleave", hide);
}

// Grouped bar chart (one bar per rule per group) with hover tooltips.
function barChart(id, groups, series, { yFormat }) {
  const host = document.getElementById(id);
  const W = 640, H = 260, m = { l: 44, r: 16, t: 10, b: 28 };
  const yMax = niceMax(Math.max(...series.flatMap(s => s.values), 0.01));
  const Y = y => H - m.b - y / yMax * (H - m.t - m.b);
  const groupW = (W - m.l - m.r) / groups.length;
  const barW = Math.min(36, (groupW - 24) / series.length - 2);
  const svg = el("svg", { viewBox: `0 0 ${W} ${H}`, role: "img" }, host);
  const tip = document.createElement("div"); tip.className = "tip"; host.appendChild(tip);

  for (let i = 0; i <= 4; i++) {
    const v = yMax * i / 4, y = Y(v);
    el("line", { x1: m.l, x2: W - m.r, y1: y, y2: y, stroke: "var(--grid)", "stroke-width": 1 }, svg);
    el("text", { x: m.l - 6, y: y + 4, "text-anchor": "end" }, svg).textContent = yFormat(v);
  }
  groups.forEach((g, gi) => {
    const cx = m.l + groupW * (gi + 0.5);
    el("text", { x: cx, y: H - 8, "text-anchor": "middle", class: "label" }, svg).textContent = g;
    const total = series.length * barW + (series.length - 1) * 2;
    series.forEach((s, si) => {
      const v = s.values[gi], x = cx - total / 2 + si * (barW + 2), y = Y(v), h = Math.max(0, H - m.b - y);
      const r = Math.min(4, h / 2, barW / 2);
      const d = `M${x},${H - m.b} V${y + r} Q${x},${y} ${x + r},${y} H${x + barW - r} Q${x + barW},${y} ${x + barW},${y + r} V${H - m.b} Z`;
      const bar = el("path", { d, fill: s.color }, svg);
      const hit = el("rect", { x: x - 1, y: m.t, width: barW + 2, height: H - m.t - m.b, fill: "transparent" }, svg);
      const show = () => {
        bar.setAttribute("opacity", 0.8);
        tip.innerHTML = `<b>${g}</b><div><i class="swatch" style="background:${s.color}"></i>${s.name}: ${yFormat(v)} (${num(s.counts[gi])} attempts)</div>`;
        tip.style.display = "block";
        const box = svg.getBoundingClientRect();
        tip.style.left = Math.min((x + barW + 8) / W * box.width, box.width - tip.offsetWidth) + "px";
        tip.style.top = "8px";
      };
      hit.addEventListener("pointerenter", show);
      hit.addEventListener("pointerleave", () => { bar.setAttribute("opacity", 1); tip.style.display = "none"; });
    });
  });
}

document.getElementById("sub").textContent =
  `${DATA.rules.map(r => r.runs).reduce((a, b) => a + b, 0)} simulated runs · ordering rules compared: ${DATA.rules.map(r => r.name).join(" vs ")} · runs stop at ${num(DATA.maxBeats)} beats · generated ${DATA.generated}`;

document.getElementById("tiles").innerHTML = DATA.rules.map((r, i) => `
  <div class="card tile">
    <h3><i class="swatch" style="background:${COLORS[i]}"></i>${r.name}</h3>
    <div class="stats">
      <div class="stat"><div class="v">${pct(r.won / r.runs)}</div><div class="l">runs heroes won</div></div>
      <div class="stat"><div class="v">${num(r.medianBeats)}</div><div class="l">median beats to win</div></div>
      <div class="stat"><div class="v">${pct(r.bestPick)}</div><div class="l">huddles sent the best hero first</div></div>
      <div class="stat"><div class="v">${num(r.avgDeaths)}</div><div class="l">hero deaths per run</div></div>
    </div>
  </div>`).join("");

["legend-survival", "legend-skill", "legend-kills"].forEach(legend);
const beatFmt = v => num(Math.round(v)) + " b";
lineChart("survival", DATA.rules.map((r, i) => ({ name: r.name, color: COLORS[i], points: r.survival })),
  { yMax: 1, yFormat: pct, xFormat: beatFmt });
const skillMax = niceMax(Math.max(...DATA.rules.flatMap(r => [...r.combat, ...r.trap].map(p => p[1]))));
["combat", "trap"].forEach(key => lineChart(key,
  DATA.rules.map((r, i) => ({ name: r.name, color: COLORS[i], points: r[key] })),
  { yMax: skillMax, yFormat: v => v.toFixed(1), xFormat: beatFmt }));
const kinds = DATA.rules[0].kills.map(k => k.kind);
barChart("kills", kinds, DATA.rules.map((r, i) => ({
  name: r.name, color: COLORS[i], values: r.kills.map(k => k.rate), counts: r.kills.map(k => k.attempts) })), { yFormat: pct });

document.getElementById("table").innerHTML =
  `<tr><th>Rule</th><th>Runs</th><th>Heroes won</th><th>Median beats</th><th>Best pick</th><th>Deaths/run</th><th>Parties/run</th><th>Souls/run</th></tr>` +
  DATA.rules.map(r => `<tr><td>${r.name}</td><td>${r.runs}</td><td>${r.won}</td><td>${num(r.medianBeats)}</td><td>${pct(r.bestPick)}</td><td>${r.avgDeaths}</td><td>${r.avgParties}</td><td>${r.avgSouls}</td></tr>`).join("");
</script>
</body>
</html>
""";
}
