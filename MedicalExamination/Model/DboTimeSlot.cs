namespace MedicalExamination
{
    public class DboTimeSlot
    {    
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int IdDoctor { get; set; }
        public int IdPatient { get; set; }      

    }
}