using System;

namespace InventoryManagement.Domain.Entities;

public class IdempotentRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestMethod { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ResponseContentType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsCompleted { get; set; }
}
