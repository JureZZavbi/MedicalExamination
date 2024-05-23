namespace MedicalExamination.Controllers
{
    public class TimeSlotRequest
    {
        public int DoctorId { get; set; }
        public DateTime SlotTime { get; set; }
    }
}
