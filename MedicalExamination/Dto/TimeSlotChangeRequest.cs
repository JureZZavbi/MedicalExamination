namespace MedicalExamination.Controllers
{
    public class TimeSlotChangeRequest
    {
        public int TimeSlotId { get; set; }    
        public DateTime SlotTime { get; set; }
    }

    public class TimeSlotRemoveRequest
    {
        public int TimeSlotId { get; set; }
    }
}
