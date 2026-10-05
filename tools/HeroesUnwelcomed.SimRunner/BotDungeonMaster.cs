using HeroesUnwelcomed.Core.Definitions;
using HeroesUnwelcomed.Core.Dungeons;
using HeroesUnwelcomed.Core.Simulation;

namespace HeroesUnwelcomed.SimRunner;

/// <summary>
/// A deliberately simple stand-in for the player, so whole runs can be
/// simulated. It is not meant to play well, only consistently, so the charts
/// show the *shape* of hero growth under a reasonable opponent.
/// </summary>
/// <remarks>
/// Every few parties it locks the door, waits for the dungeon to empty, then
/// spends souls:
/// <list type="number">
///   <item>Build a new room in the category the heroes are currently weakest at
///   (the hardest one it can afford), until the dungeon has <see cref="MaxChallengeRooms"/>.</item>
///   <item>Otherwise upgrade the room heroes have beaten most often.</item>
/// </list>
/// Then it reopens the door.
/// </remarks>
public sealed class BotDungeonMaster
{
    public const int PartiesBetweenBuilds = 3;
    public const int MaxChallengeRooms = 8;

    private int _partiesAtLastBuild;

    public void Act(Run run)
    {
        int partiesSent = run.AllParties.Count;

        if (run.Door == DoorState.Open && partiesSent - _partiesAtLastBuild >= PartiesBetweenBuilds)
        {
            run.Execute(new LockDoor());
        }

        if (run.Door == DoorState.Locked)
        {
            SpendSouls(run);
            run.Execute(new UnlockDoor());
            _partiesAtLastBuild = partiesSent;
        }
    }

    private static void SpendSouls(Run run)
    {
        for (int action = 0; action < 20; action++)
        {
            if (!TryBuild(run) && !TryUpgrade(run))
            {
                return;
            }
        }
    }

    private static bool TryBuild(Run run)
    {
        int challengeRooms = run.Dungeon.Rooms.Count(r => r.Kind.Category is RoomCategory.Combat or RoomCategory.Trap);
        if (challengeRooms >= MaxChallengeRooms || FreeCell(run) is not { } cell)
        {
            return false;
        }

        RoomCategory weakest = new[] { RoomCategory.Combat, RoomCategory.Trap }
            .MinBy(category => run.Heroes.All.Average(h => h.Skill(category)));

        RoomKind? kind = run.Definitions.RoomKinds
            .Where(k => k.Buildable && k.Category == weakest && run.Souls.CanAfford(k.PlaceCost))
            .MaxBy(k => k.Difficulty);

        return kind is not null && run.Execute(new PlaceRoom(kind.Id, cell)).Accepted;
    }

    private static bool TryUpgrade(Run run)
    {
        var clearsByRoom = run.Events.OfType<RoomCleared>()
            .GroupBy(c => c.RoomId)
            .ToDictionary(g => g.Key, g => g.Count());

        Room? target = run.Dungeon.Rooms
            .Where(r => r.Kind.IsChallenge && r.Level < r.Kind.MaxLevel && run.Souls.CanAfford(r.Kind.UpgradeCostFrom(r.Level)))
            .OrderByDescending(r => clearsByRoom.GetValueOrDefault(r.Id))
            .ThenBy(r => r.Id)
            .FirstOrDefault();

        return target is not null && run.Execute(new UpgradeRoom(target.Id)).Accepted;
    }

    // First empty grid cell, scanning row by row.
    private static GridPoint? FreeCell(Run run)
    {
        for (int y = 0; y < run.Dungeon.Height; y++)
        {
            for (int x = 0; x < run.Dungeon.Width; x++)
            {
                var cell = new GridPoint(x, y);
                if (run.Dungeon.RoomAt(cell) is null)
                {
                    return cell;
                }
            }
        }
        return null;
    }
}
