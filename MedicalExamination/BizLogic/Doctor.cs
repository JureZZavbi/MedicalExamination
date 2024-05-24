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
    }
}
