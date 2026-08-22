namespace InventoryManagement.Domain.Constants;

public static class AuthConstants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string NursePractitioner = "NursePractitioner";
        public const string Staff = "Staff";
    }

    public static class Claims
    {
        public const string UserId = "user_id";
        public const string Email = "email";
        public const string Name = "name";
        public const string Role = "role";
        public const string IsAdmin = "is_admin";
        public const string IsProvider = "is_provider";

        /// <summary>Owning tenant of the authenticated user; drives request-scoped tenant isolation.</summary>
        public const string Tenant = "tenant";
    }

    public static class Policies
    {
        public const string AdminOnly = "AdminOnly";
        public const string AdminOrProvider = "AdminOrProvider";
        public const string AllRoles = "AllRoles";
    }
}
