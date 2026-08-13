namespace InventoryManagement.Domain.Enums;

/// <summary>Coarse-grained user role that drives role-based authorization policies.</summary>
public enum UserRole
{
    /// <summary>Full administrative access.</summary>
    Admin = 1,

    /// <summary>Clinical provider with elevated (non-admin) privileges.</summary>
    NursePractitioner = 2,

    /// <summary>Standard operational user.</summary>
    Staff = 3,
}
