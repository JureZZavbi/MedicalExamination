using MedicalExamination.Controllers;
using Microsoft.EntityFrameworkCore;
namespace MedicalExamination.Tests
{
    public class TestDbContextFactory
    {
        public static ExaminationDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ExaminationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            var context = new ExaminationDbContext(options);

            context.DboDoctors.Add(new DboDoctor { Id = 1, Name = "DocName", Surname = "DocSurname", StartShift = new TimeOnly (10,0), EndShift = new TimeOnly (18,0) });
            context.SaveChanges();

            return context;
        }
    }
}