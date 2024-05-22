namespace MedicalExamination.Controllers
{
    public class NewPatientRequest
    {
        public string? Name { get; set; }

        public string? Surname { get; set; }
    }

    public class NewDoctorRequest
    {
        public string? Name { get; set; }

        public string? Surname { get; set; }
        public TimeOnly? StartShift { get; set; }
        public TimeOnly? EndShift { get; set; }
    }
}