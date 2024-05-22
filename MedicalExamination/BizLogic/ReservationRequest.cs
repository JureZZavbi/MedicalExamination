namespace MedicalExamination.Controllers
{
    public class TimeSlotRequest
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime SlotTime { get; set; }
    }
}
