using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[Table("achievement_player")]
[Index(nameof(UserId))]
public sealed class AchievementPlayer
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required, MaxLength(256)]
    public string AchievementId { get; set; } = string.Empty;

    [Required]
    public DateTime EarnedAt { get; set; }
}
