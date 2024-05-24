namespace MedicalExamination
{
    public class TimeSlotRequest
    {
        public int DoctorId { get; set; }
        public DateTime SlotTime { get; set; }
    }

    public class TimeSlotCancelRequest
    {
        public int SlotId { get; set; }
    }
}
