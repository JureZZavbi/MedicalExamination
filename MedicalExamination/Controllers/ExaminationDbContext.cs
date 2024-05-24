using Microsoft.EntityFrameworkCore;

namespace MedicalExamination.Controllers
{
    public class ExaminationDbContext : DbContext
    {
        public DbSet<DboDoctor> DboDoctors { get; set; }
        public DbSet<DboPatient> DboPatients { get; set; }
        public DbSet<DboTimeSlot> DboTimeSlots { get; set; }
        public DbSet<DboSecret> DboSecrets { get; set; }
        public ExaminationDbContext(DbContextOptions<ExaminationDbContext> options) : base(options) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
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
}
