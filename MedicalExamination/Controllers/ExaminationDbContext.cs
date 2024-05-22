using Microsoft.EntityFrameworkCore;

namespace MedicalExamination.Controllers
{
    public class ExaminationDbContext : DbContext
    {
        public DbSet<DboDoctor> DboDoctors { get; set; }
        public DbSet<DboPatient> DboPatients { get; set; }
        public DbSet<DboTimeSlot> DboTimeSlots { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // optionsBuilder.UseInMemoryDatabase("MyDB");
            //optionsBuilder.UseSqlServer("Password=sa;Persist Security Info=True;User ID=sa;Initial Catalog=Test1;Data Source=localhost\\sqlexpress;TrustServerCertificate=True");
            optionsBuilder.UseSqlServer(
            "Password=sa;Persist Security Info=True;User ID=sa;Initial Catalog=MedicalExamination;Data Source=localhost\\sqlexpress;TrustServerCertificate=True",
            sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        }

    }
}
