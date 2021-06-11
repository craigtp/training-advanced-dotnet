using System;
using Scheduling.EventSourcing;

namespace Scheduling.Domain.DoctorDay.Commands
{
    public class CancelSlotBooking : Command<CancelSlotBooking>
    {
        public string DayId { get; }

        public Guid SlotId { get; }

        public string Reason { get; }

        public string RequestedBy { get; }

        public CancelSlotBooking(string dayId, Guid slotId, string reason, string requestedBy)
        {
            DayId = dayId;
            SlotId = slotId;
            Reason = reason;
            RequestedBy = requestedBy;
        }
    }
}
