namespace HeroesUnwelcomed.Core.Simulation;

/// <summary>One change to the player's souls.</summary>
public readonly record struct SoulTransaction(double Beat, int Amount, string Reason, int? RoomId);

/// <summary>
/// The player's souls, with every gain and spend recorded. Prestige formulas
/// and balance reports can be worked out from the history later.
/// </summary>
public sealed class SoulLedger
{
    private readonly List<SoulTransaction> _history = [];

    public int Balance { get; private set; }

    public IReadOnlyList<SoulTransaction> History => _history;

    public int TotalEarned => _history.Where(t => t.Amount > 0).Sum(t => t.Amount);

    public int TotalSpent => -_history.Where(t => t.Amount < 0).Sum(t => t.Amount);

    public bool CanAfford(int cost) => Balance >= cost;

    internal void Add(SoulTransaction transaction)
    {
        if (Balance + transaction.Amount < 0)
        {
            throw new InvalidOperationException($"Can't spend {-transaction.Amount} souls with only {Balance}.");
        }
        Balance += transaction.Amount;
        _history.Add(transaction);
    }
}
