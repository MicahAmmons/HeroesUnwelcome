using HeroesUnwelcomed.Core.Definitions;

namespace HeroesUnwelcomed.Game.Data;

/// <summary>Reads the simulation's data files (data/game/*.json) for Core, which never touches files itself.</summary>
public static class GameDataLoader
{
    public const string DefaultSetup = "game/run-setup.json";

    public static GameDefinitions LoadDefinitions(DataSource data) => GameDefinitions.Parse(
        data.ReadText("game/rooms.json"),
        data.ReadText("game/tuning.json"),
        data.ReadText("game/heroes.json"));

    public static RunSetup LoadSetup(DataSource data, string relativePath = DefaultSetup) =>
        GameDefinitions.ParseSetup(data.ReadText(relativePath));
}
