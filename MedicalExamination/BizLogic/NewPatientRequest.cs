using Microsoft.AspNetCore.DataProtection;

namespace MedicalExamination.Controllers
{
    public class NewPatientRequest : SecretData
    {
        public string? Name { get; set; }

        public string? Surname { get; set; }
        //public string? Username { get; set; }
        //public string? Password { get; set; }
    }

    public class NewDoctorRequest : SecretData
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public TimeOnly? StartShift { get; set; }
        public TimeOnly? EndShift { get; set; }
    }

    public abstract class SecretData
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }


}