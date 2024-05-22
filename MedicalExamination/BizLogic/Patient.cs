namespace MedicalExamination
{
    public class Patient
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Surname;

        public List<TimeSlot>? BookedTimeSlots { get; set; }
    }
}
