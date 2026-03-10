using System;

namespace StandAlonePlan.Features.PlanSelection.Domain.Models
{
    /// <summary>
    /// Business entity representing a plan as shown to the user.
    /// Name comes from R11FILE master. IsDeleted / ExpirationDate come from R18FILE patient-plan record.
    /// </summary>
    public class Plan
    {
        public string    PlanCode       { get; init; } = ""; // PL-CODE PIC X(6) / R11-PLAN-CODE PIC X(6)
        public string    Name           { get; init; } = ""; // PL-NAME PIC X(25) / R11-NAME PIC X(25)
        public DateTime? ExpirationDate { get; init; }       // R18-EXP-DATE PIC 9(8)
        public bool      IsDeleted      { get; init; }       // R11-DELETED or R18-DELETED PIC X

        // R5-PP = 'C' (Cash) or 'A' (Cash alternate key)
        public bool IsCash => PlanCode.Trim() == "C" || PlanCode.Trim() == "A";

        // Computed from R11-DELETED + R18-EXP-DATE vs SYS-DATE
        public PlanStatus Status
        {
            get
            {
                if (IsDeleted) return PlanStatus.Disabled;
                if (ExpirationDate.HasValue && ExpirationDate.Value < DateTime.Today)
                    return PlanStatus.Expired;
                return PlanStatus.Active;
            }
        }

        // COBOL: IF R18-DELETED → "  **DISABLED**" / IF expired → "  **EXPIRED**" / ELSE " " + R11-NAME
        public string DisplayName => Status switch
        {
            PlanStatus.Expired  => "  **EXPIRED**",
            PlanStatus.Disabled => "  **DISABLED**",
            _                   => Name
        };
    }

    public enum PlanStatus { Active, Expired, Disabled }
}
