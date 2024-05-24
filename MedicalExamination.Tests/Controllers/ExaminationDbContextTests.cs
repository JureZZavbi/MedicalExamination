using MedicalExamination.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
namespace MedicalExamination.Tests
{
    [TestClass]
    public class MedicalExaminationControllerTests1
    {
        private Mock<ILogger<MedicalExaminationController>> _mockLogger;
        private MedicalExaminationController _controller;
        private ExaminationDbContext _context;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogger = new Mock<ILogger<MedicalExaminationController>>();
            _context = TestDbContextFactory.CreateDbContext();

            _controller = new MedicalExaminationController(_mockLogger.Object, _context)
            {
                ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext()
                    {
                        User = GetMockedUser("testUserId")
                    }
                }
            };
        }

        [TestMethod]
        public void GetDoctors_ShouldReturnDoctors_WhenUserIsAuthenticated()
        {
            var doctors = _controller.GetDoctors().ToList();

            Assert.IsNotNull(doctors);
            Assert.IsInstanceOfType(doctors, typeof(IEnumerable<Doctor>));
            Assert.AreEqual(1, doctors.Count());
            Assert.IsTrue(doctors.First().Id == 1);
            Assert.IsTrue(doctors.First().Name == "DocName");
            Assert.IsTrue(doctors.First().Surname == "DocSurname");
        }

        private ClaimsPrincipal GetMockedUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            return new ClaimsPrincipal(identity);
        }
    }
}
