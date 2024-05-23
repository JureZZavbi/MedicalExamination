namespace MedicalExamination.Controllers
{
    public class TimeSlotChangeRequest
    {
        public int TimeSlotId { get; set; }
      //  public int DoctorId { get; set; }       
        public DateTime SlotTime { get; set; }
    }

    public class TimeSlotRemoveRequest
    {
        public int TimeSlotId { get; set; }
       // public int DoctorId { get; set; } //will be removed once login works
    }
}
