using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class UserLike
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public int ScootyId { get; set; }

    public DateTime LikedAt { get; set; }

    public virtual ScootyInventory Scooty { get; set; } = null!;
}
