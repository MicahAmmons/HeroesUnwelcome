using HeroesUnwelcomed.Game;

// Desktop launcher: read the command-line arguments, start the shared game.
// Future iOS/Android launchers will be just as small.
var options = LaunchOptions.Parse(args);

using var game = new HeroesGame(options);
game.Run();
