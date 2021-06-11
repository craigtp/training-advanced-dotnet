using System;
using System.Linq;
using System.Threading.Tasks;
using Scheduling.Domain.DoctorDay.Events;
using Scheduling.Domain.ReadModel;
using Scheduling.Infrastructure.MongoDb;
using EventHandler = Scheduling.Infrastructure.Projections.EventHandler;

namespace Scheduling.Application
{
    public class AvailableSlotsProjectionV2 : EventHandler
    {
        public AvailableSlotsProjectionV2(MongoDbAvailableSlotsRepositoryV2 availableSlotsRepository)
        {
            When<SlotScheduled>(async (e, m) =>
                {
                    var availableSlots = await availableSlotsRepository.GetAvailableSlotsOn(e.SlotStartTime);
                    if (availableSlots.All(s => s.Id != e.SlotId.ToString()))
                    {
                        await availableSlotsRepository.AddSlot(new MongoDbAvailableSlotV2
                        {
                            Id = e.SlotId.ToString(),
                            DayId = e.DayId,
                            Duration = e.SlotDuration,
                            Date = e.SlotStartTime.ToString("dd-MM-yyyy"),
                            StartTime = e.SlotStartTime.ToString("h:mm tt"),
                            IsBooked = false
                        });
                    }
                });

            When<SlotBooked>((e, m) => availableSlotsRepository.HideSlot(e.SlotId));

            // Add when for SlotBookingCancelled
            When<SlotBookingCancelled>((e, m) => availableSlotsRepository.ShowSlot(e.SlotId));

            When<SlotScheduleCancelled>((e, m) => availableSlotsRepository.DeleteSlot(e.SlotId));
        }
    }
}
