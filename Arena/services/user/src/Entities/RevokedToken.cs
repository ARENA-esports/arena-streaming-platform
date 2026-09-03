using System;

namespace UserService.Entities;

public class RevokedToken
{
    public string Jti { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public DateTime RevokedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
