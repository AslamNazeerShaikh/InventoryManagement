namespace InventoryManagement.Domain.Constants;

public static class BusinessConstants
{
    public static class ExpiryAlert
    {
        public const int MinMonthsBefore = 3;
        public const int MaxMonthsBefore = 6;
        public const int DefaultMonthsBefore = 3;
    }

    public static class Pagination
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
    }

    public static class Inventory
    {
        public const int LowStockThreshold = 5;
        public const int MaxBarcodeLength = 50;
        public const int MaxSerialNumberLength = 50;
    }

    public static class Assignment
    {
        public const int DefaultAssignmentDays = 30;
        public const int MaxAssignmentDays = 365;
        public const int DefaultDueSoonDays = 7;
    }

    public static class Maintenance
    {
        public const int DefaultDueWindowDays = 30;
        public const int MaxDueWindowDays = 365;
    }
}
