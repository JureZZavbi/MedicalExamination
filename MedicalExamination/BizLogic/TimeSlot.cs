using System.Text.Json.Serialization;

namespace MedicalExamination
{
    public class TimeSlot
    {

        //Timeslots are from 7 to 19, devided by 1 hour
        //Timeslots are only occupied when there is a doctor and patient id on it
        //TODO set timeslot id nullable so it doestn show ids for available slots
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; } 
    }
}