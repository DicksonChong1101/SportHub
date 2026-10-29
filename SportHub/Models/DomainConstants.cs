namespace SportHub.Models;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Member = "Member";
}

public static class ReservationStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
}

public static class PaymentStatuses
{
    public const string Unpaid = "Unpaid";
    public const string PendingVerification = "Pending Verification";
    public const string Paid = "Paid";
    public const string Refunded = "Refunded";
    public const string Failed = "Failed";
}

public static class PaymentMethods
{
    public const string TouchNGo = "Touch 'n Go eWallet";
    public const string DuitNow = "DuitNow Transfer";
    public const string OnlineBanking = "Online Banking";

    // Legacy values are retained so earlier records remain readable.
    public const string Cash = "Cash";
    public const string Card = "Card";
    public const string OnlineTransfer = "Online Transfer";
}

// Controls the public Home-page grouping without guessing from a facility name.
public static class FacilityTypes
{
    public const string Indoor = "Indoor";
    public const string Outdoor = "Outdoor";
}
