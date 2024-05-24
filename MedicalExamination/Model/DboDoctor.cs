namespace MedicalExamination
{
    public class DboDoctor
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Surname{ get; set; }

        public TimeOnly? StartShift{ get; set; }
        public TimeOnly? EndShift { get; set; }

        public Doctor GetDoctor() 
        {  
            return new Doctor() 
            { 
                Id = Id, 
                Name = Name, 
                Surname = Surname,
                StartShift = StartShift,
                EndShift   = EndShift,
            }; 
        }
    }
}
