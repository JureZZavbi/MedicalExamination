using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using System.Text.Json.Serialization;

namespace MedicalExamination
{
    public class Doctor
    {
        
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Surname { get; set; }
        public TimeOnly? StartShift { get; set; }
        public TimeOnly? EndShift { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<TimeSlot>? AvailableTimeslots { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<TimeSlot>? BookedTimeslots { get; set; }


        //TODO dbo timeslot should not be here
        internal void GetAvailableTimeslots(IQueryable<DboTimeSlot> takenSlots, int IdPatient)
        {
           
            var start = StartShift ?? new TimeOnly(0, 0);
            var end = EndShift ?? new TimeOnly(24, 0);
            AvailableTimeslots = new List<TimeSlot>();
            DateOnly today = DateOnly.FromDateTime(DateTime.Now);


            var endSlot = start.AddMinutes(Settings.SlotSize30Minutes);
            while (endSlot < end) 
            {
                var startDT = today.ToDateTime(start);
                var endDt = today.ToDateTime(start.AddMinutes(Settings.SlotSize30Minutes));
                var checkExisting = takenSlots.FirstOrDefault(x => x.StartTime == startDT);
                if (checkExisting != null)
                { 
                    if(checkExisting.IdPatient == IdPatient)
                    {
                        if(BookedTimeslots == null) BookedTimeslots = new List<TimeSlot>();
                        BookedTimeslots.Add(new TimeSlot
                        {
                            Id = checkExisting.Id,
                            StartTime = checkExisting.StartTime,
                            EndTime = checkExisting.EndTime,
                        });
                    }
                    continue; 
                }
                AvailableTimeslots.Add(new TimeSlot
                {
                    StartTime = startDT,
                    EndTime = endDt,
                });
                start = endSlot;
                endSlot = start.AddMinutes(Settings.SlotSize30Minutes);
            }
        }
    }
}
