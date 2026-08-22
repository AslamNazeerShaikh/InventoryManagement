namespace InventoryManagement.Domain.Constants;

/// <summary>Authentication/authorization claim names used across the token and the API.</summary>
public static class AuthConstants
{
    /// <summary>JWT claim names.</summary>
    public static class Claims
    {
        /// <summary>Authenticated user's identifier.</summary>
        public const string UserId = "user_id";

        /// <summary>Authenticated user's email.</summary>
        public const string Email = "email";

        /// <summary>Authenticated user's display name.</summary>
        public const string Name = "name";

        /// <summary>Owning tenant of the authenticated user; drives request-scoped tenant isolation.</summary>
        public const string Tenant = "tenant";

        /// <summary>A granted permission code. Emitted once per effective permission; checked by authorization.</summary>
        public const string Permission = "permission";
    }
}
