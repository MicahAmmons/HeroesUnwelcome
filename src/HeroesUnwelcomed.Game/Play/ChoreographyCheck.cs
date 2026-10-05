using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Game.Animation;

namespace HeroesUnwelcomed.Game.Play;

/// <summary>
/// Checks every animation against the step it plays in. Each step has a fixed
/// length, and a step's clip should fill it exactly so it ends on the beat:
/// longer would be cut off, shorter would end early and freeze. This lists
/// both, which gives artists exact frame budgets ("Fight is exactly 2 beats").
/// </summary>
public static class ChoreographyCheck
{
    /// <summary>Which step each clip plays in (hero and encounter clips share names where they share a step).</summary>
    public static IReadOnlyDictionary<string, Func<Tuning, double>> StepLengths { get; } = new Dictionary<string, Func<Tuning, double>>
    {
        ["EncounterEntrance"] = t => t.EntranceBeats,
        ["HeroEnter"] = t => t.EntranceBeats,
        ["Ready"] = t => t.ReadyBeats,
        ["Fight"] = t => t.ActionBeats,
        ["Disarm"] = t => t.ActionBeats,
        ["Puzzle"] = t => t.ActionBeats,
        ["Magic"] = t => t.ActionBeats,
        ["Interact"] = t => t.ActionBeats,
        ["Victory"] = t => t.ResultBeats,
        ["VictoryReact"] = t => t.ResultBeats,
        ["Death"] = t => t.ResultBeats,
        ["DeathReact"] = t => t.ResultBeats,
        ["Defeat"] = t => t.ResultBeats,
    };

    /// <summary>Human-readable problems, e.g. "Orc.Interact lasts 3 beats but its step is 2".</summary>
    public static IReadOnlyList<string> Problems(AnimationLibrary animations, Tuning tuning)
    {
        var problems = new List<string>();
        foreach (EntityDefinition entity in animations.Entities)
        {
            foreach (ClipDefinition clip in entity.Clips)
            {
                if (clip.Loop || !StepLengths.TryGetValue(clip.Name, out var stepLength))
                {
                    continue;
                }
                double step = stepLength(tuning);
                if (clip.LengthBeats > step + 1e-9)
                {
                    problems.Add($"{entity.Name}.{clip.Name} lasts {clip.LengthBeats:0.##} beats but its step is {step:0.##}; it will be cut off.");
                }
                else if (clip.LengthBeats < step - 1e-9)
                {
                    problems.Add($"{entity.Name}.{clip.Name} lasts {clip.LengthBeats:0.##} beats but its step is {step:0.##}; it ends early and holds its last frame.");
                }
            }
        }
        return problems;
    }
}
