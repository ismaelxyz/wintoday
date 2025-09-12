using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wintoday.Server.Domain.Entities;

public class Player
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = null!; // Original user supplied (case preserved)

    [MaxLength(100)]
    public string NormalizedName { get; set; } = null!; // Upper invariant for uniqueness

    [Column(TypeName = "decimal(18,2)")]
    public decimal Funds { get; set; }

    // Removed [Timestamp] because PostgreSQL does not auto-generate a SQL Server style rowversion.
    // Keeping the column allows future custom concurrency logic if desired.
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<Bet> Bets { get; set; } = new List<Bet>();
    public ICollection<BalanceTransaction> Transactions { get; set; } = new List<BalanceTransaction>();
}
