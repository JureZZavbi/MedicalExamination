using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace MedicalExamination.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class MedicalExaminationController : ControllerBase
    {
        private readonly ILogger<MedicalExaminationController> _logger;
        private readonly IPasswordHasher<DboSecret> _passwordHasher;
        private readonly ExaminationDbContext _context;

        public MedicalExaminationController(ILogger<MedicalExaminationController> logger)
        {
            _logger = logger;
            _passwordHasher = new PasswordHasher<DboSecret>();
            _context = new ExaminationDbContext();
        }
       
        #region Patient
        [HttpGet(Name = "GetDoctors")]
        [Authorize]
        public IEnumerable<Doctor> GetDoctors()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return new List<Doctor>();
            }

            List<Doctor>? doctors = null;

            var dboDoctors = _context.DboDoctors.ToList();
            doctors = dboDoctors.Select(x=> x.GetDoctor()).ToList();
            return doctors;        
        }

        [HttpGet(Name = "GetDoctorsAvailableSlots")]
        [Authorize]
        public IActionResult GetDoctorsAvailableSlots([FromQuery] int IdDoctor, [FromQuery] DateOnly SlotDate)
        {
            var dateValid = CheckDateValidity(SlotDate);
            if (dateValid is not OkResult) return dateValid;

            var result = GetPatientid(out var IdPatient);
            if (result is not OkResult || IdPatient == null)
            {
                return result;
            }
            var dboDoctor = _context.DboDoctors.FirstOrDefault(x => x.Id == IdDoctor);
            if (dboDoctor == null)
            {
                return NotFound(new { Message = $"Doctor with ID {IdDoctor} was not found." });
            }
            var doctor = dboDoctor.GetDoctor();
            var takenSlots = _context.DboTimeSlots.Where(x => x.IdDoctor == IdDoctor);
            GetAvailableTimeslots(doctor, takenSlots, IdPatient.Value, SlotDate);

            return Ok(doctor);
        }

        [HttpPost(Name = "ReserveSlot")]
        [Authorize]
        public IActionResult ReserveSlot([FromBody] TimeSlotRequest request)
        {
            var dateValid = CheckDateValidity(DateOnly.FromDateTime(request.SlotTime));
            if (dateValid is not OkResult) return dateValid;           

            var result = GetPatientid(out var IdPatient);
            if (result is not OkResult || IdPatient == null)
            {
                return result;
            }

            request.SlotTime = TrimSecondsAndMilliseconds(request.SlotTime);
            if (!IsValidTimeSlot(request.SlotTime))
            {
                return BadRequest(new { Message = "Invalid time slot. Time must be on the hour or half-hour (e.g., 17:00 or 18:30)." });
            }

            if (request.SlotTime <= DateTime.Now) 
                return BadRequest(new { Message = "Time slot is invalid (before current time)." }); ;

            var dboDoctor = _context.DboDoctors.FirstOrDefault(x => x.Id == request.DoctorId);
            if (dboDoctor == null)
            {
                return NotFound(new { Message = $"Doctor with ID {request.DoctorId} was not found." });
            }

            var dboPatient = _context.DboPatients.FirstOrDefault(x => x.Id == IdPatient);
            if (dboPatient == null)
            {
                return NotFound(new { Message = $"Patient with ID {IdPatient} was not found." });
            }
            //Check if time slot is within doctor's working hours
            if ((TimeOnly.FromDateTime(request.SlotTime) < dboDoctor.StartShift) ||
                TimeOnly.FromDateTime(request.SlotTime.AddMinutes(Settings.SlotSize30Minutes)) >= dboDoctor.EndShift)
            {
                return NotFound(new { Message = $"No available slot found for Doctor with ID {request.DoctorId} at the specified time." });
            }

            var slot = _context.DboTimeSlots.FirstOrDefault(x => x.IdDoctor == request.DoctorId && x.StartTime == request.SlotTime && x.IdPatient != IdPatient);
            if (slot != null)
            {
                return NotFound(new { Message = $"No available slot found for Doctor with ID {request.DoctorId} at the specified time." });
            }

            slot = new DboTimeSlot()
            {
                IdPatient = IdPatient.Value,
                IdDoctor = request.DoctorId,
                StartTime = request.SlotTime,
                EndTime = request.SlotTime.AddMinutes(Settings.SlotSize30Minutes),
            };
            try
            {
                _context.DboTimeSlots.Add(slot);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest("Saving failed.");
            }
            return Ok(new { Message = "Slot reserved successfully." });
        }

        [HttpPost(Name = "CancelReservation")]
        [Authorize]
        public IActionResult CancelReservation([FromBody] TimeSlotCancelRequest cancelReq)
        {
            var result = GetPatientid(out var IdPatient);
            if (result is not OkResult || IdPatient == null)
            {
                return result;
            }

            var slot = _context.DboTimeSlots.FirstOrDefault(x => x.Id == cancelReq.SlotId);
            if (slot == null)
            {
                return NotFound(new { Message = $"No reservation found for time slot with ID {cancelReq.SlotId}" });
            }

           
            if (slot.IdPatient != IdPatient)
            {
                return NotFound(new { Message = $"You are not allowed to delete appointments that are not your own!" });
            }

            _context.DboTimeSlots.Remove(slot);
            _context.SaveChanges();

            return Ok(new { Message = "Reservation canceled successfully." });
        }
        private static bool Weekend(DateOnly slotTime)
        {
            return slotTime.DayOfWeek == DayOfWeek.Saturday || slotTime.DayOfWeek == DayOfWeek.Sunday;
        }
        private IActionResult GetPatientid(out int? IdPatient)
        {
            IdPatient = null;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogError("Logged in user is missing a name identifier.");
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
            var secret = _context.DboSecrets.FirstOrDefault(x => x.Username == userId);
            if (secret == null)
            {
                _logger.LogError($"Secret not found for username : {userId}.");
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
            if (secret.UserType != (int)UserType.Patient)
            {
                return NotFound("This view is for patients only!");
            }
            IdPatient = _context.DboPatients.FirstOrDefault(x => x.Id == secret.IdU)?.Id;
            if (IdPatient == null)
            {
                _logger.LogError($"Patient not found for username : {userId}.");
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
            return Ok();
        }

        private void GetAvailableTimeslots(Doctor doctor, IQueryable<DboTimeSlot> takenSlots, int IdPatient, DateOnly date)
        {
            var start = doctor.StartShift ?? new TimeOnly(0, 0);

            start = AdjustForNow(date, start);

            var end = doctor.EndShift ?? new TimeOnly(24, 0);
            doctor.AvailableTimeslots = new List<TimeSlot>();

            var endSlot = start.AddMinutes(Settings.SlotSize30Minutes);
            while (endSlot <= end)
            {
                var startDT = date.ToDateTime(start);
                var endDt = date.ToDateTime(start.AddMinutes(Settings.SlotSize30Minutes));
                var checkExisting = takenSlots.FirstOrDefault(x => x.StartTime == startDT);
                if (checkExisting != null)
                {
                    if (checkExisting.IdPatient == IdPatient)
                    {
                        if (doctor.BookedTimeslots == null) doctor.BookedTimeslots = new List<TimeSlot>();
                        doctor.BookedTimeslots.Add(new TimeSlot
                        {
                            Id = checkExisting.Id,
                            StartTime = checkExisting.StartTime,
                            EndTime = checkExisting.EndTime,
                        });
                    }
                }
                else
                {
                    doctor.AvailableTimeslots.Add(new TimeSlot
                    {
                        StartTime = startDT,
                        EndTime = endDt,
                    });
                }
                start = endSlot;
                endSlot = start.AddMinutes(Settings.SlotSize30Minutes);
            }
        }

        private static TimeOnly AdjustForNow(DateOnly date, TimeOnly start)
        {
            if (date == DateOnly.FromDateTime(DateTime.Now) && start <= TimeOnly.FromDateTime(DateTime.Now))
            {
                start = TimeOnly.FromDateTime(DateTime.Now);
                if (start.Minute < 30)
                {
                    start = new TimeOnly(start.Hour, Settings.SlotSize30Minutes);
                }
                else
                {
                    start = new TimeOnly(start.Hour + 1, 0);
                }
            }

            return start;
        }

        private IActionResult CheckDateValidity(DateOnly SlotDate)
        {
            if (Weekend(SlotDate))
            {
                return BadRequest("No slots are available on weekends.");
            }
            if (DateOnly.FromDateTime(DateTime.Now) > SlotDate)
            {
                return BadRequest("Date is invalid");
            }
            return Ok();
        }
        #endregion Patient

        #region Doctor

        [HttpGet(Name = "GetFutureApointments")]
        [Authorize]
        public IActionResult GetFutureApointments()
        {
            var result = GetDoctorId(out var IdDoctor);
            if (result is not OkResult || IdDoctor == null)
            {
                return result;
            }
            var dboDoctor = _context.DboDoctors.FirstOrDefault(x => x.Id == IdDoctor);
            if (dboDoctor == null)
            {
                return NotFound(new { Message = $"Doctor with ID {IdDoctor} was not found." });
            }
            var doctor = dboDoctor.GetDoctor();
            var futureAppointments = _context.DboTimeSlots.Where(x => x.IdDoctor == IdDoctor && x.StartTime > DateTime.Now.AddMinutes(-Settings.SlotSize30Minutes))
                .Join(
                _context.DboPatients, 
                slot => slot.IdPatient,
                patient => patient.Id,
                (slot, patient) => new
                {
                    slotId = slot.Id,
                    patientId = patient.Id,
                    patient.Name,
                    patient.Surname,
                    slot.StartTime,
                    slot.EndTime,
                }).ToList();
                return Ok(futureAppointments);
        }

        [HttpPost(Name = "RemoveAppointment")]
        [Authorize]
        public IActionResult RemoveAppointment([FromBody] TimeSlotRemoveRequest request)
        {
            //TODO verify slot doctor is the same doctor that is making the change
            var result = GetDoctorId(out var IdDoctor);
            if (result is not OkResult || IdDoctor == null)
            {
                return result;
            }

            var dboDoctor = _context.DboDoctors.FirstOrDefault(x => x.Id == IdDoctor);
            if (dboDoctor == null)
            {
                return NotFound(new { Message = $"Doctor with ID {IdDoctor} was not found." });
            }    

            var slot = _context.DboTimeSlots.FirstOrDefault(x => x.Id == request.TimeSlotId);
            if (slot == null)
            {
                return NotFound(new { Message = $"No available slot found ID {request.TimeSlotId}." });
            }
            if(slot.IdDoctor != IdDoctor)
            {
                return BadRequest(new { Message = $"Current doctor does not have permission to edit slot with ID {request.TimeSlotId}" });
            }
              
            try
            {
                _context.DboTimeSlots.Remove(slot);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest("Saving failed.");
            }
            return Ok(new { Message = "Appointment canceled successfully." });
        }

        [HttpPost(Name = "UpdateSlotTime")]
        [Authorize]
        public IActionResult UpdateSlotTime([FromBody] TimeSlotChangeRequest request )
        {
            //TODO verify slot doctor is the same doctor that is making the change
            var result = GetDoctorId(out var IdDoctor);
            if (result is not OkResult || IdDoctor == null)
            {
                return result;
            }

            request.SlotTime = TrimSecondsAndMilliseconds(request.SlotTime);
            if (!IsValidTimeSlot(request.SlotTime))
            {
                return BadRequest(new { Message = "Invalid time slot. Time must be on the hour or half-hour (e.g., 17:00 or 18:30)." });
            }
            var dboDoctor = _context.DboDoctors.FirstOrDefault(x => x.Id == IdDoctor);
            if (dboDoctor == null)
            {
                return NotFound(new { Message = $"Doctor with ID {IdDoctor} was not found." });
            }

            var slot = _context.DboTimeSlots.FirstOrDefault(x => x.Id == request.TimeSlotId);
            if (slot == null)
            {
                return NotFound(new { Message = $"No available slot found ID {request.TimeSlotId}." });
            }
            if (slot.IdDoctor != IdDoctor)
            {
                return BadRequest(new { Message = $"Current doctor does not have permission to edit slot with ID {request.TimeSlotId}" });
            }
            slot.StartTime = request.SlotTime;
            slot.EndTime = request.SlotTime.AddMinutes(Settings.SlotSize30Minutes);
            try
            {
                _context.DboTimeSlots.Update(slot);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
            return Ok(new { Message = "Appointment time updated successfully." });            
        }
        private IActionResult GetDoctorId(out int? idDoctor)
        {
            idDoctor = null;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogError("Logged in user is missing a name identifier.");
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
            var secret = _context.DboSecrets.FirstOrDefault(x => x.Username == userId);
            if (secret == null)
            {
                _logger.LogError($"Secret not found for username : {userId}.");
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
            if (secret.UserType != (int)UserType.Doctor)
            {
                return NotFound("This view is for doctors only!");
            }
            idDoctor = _context.DboDoctors.FirstOrDefault(x => x.Id == secret.IdU)?.Id;
            if (idDoctor == null)
            {
                _logger.LogError($"Patient not found for username : {userId}.");
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
            return Ok();
        }
        #endregion Doctor

        #region Public
        [HttpPost(Name = "RegisterNewPatient")]
       // [Authorize]
        public IActionResult RegisterNewPatient([FromBody] NewPatientRequest patient)
        {
            if (string.IsNullOrEmpty(patient.Name))
            {
                return BadRequest(new { Message = "Patient name missing!" });
            }
            if (string.IsNullOrEmpty(patient.Surname))
            {
                return BadRequest(new { Message = "Patient surname missing!" });
            }
            if (UsernameExists(patient.Username))
            {
                return BadRequest(new { Message = "Username already exists!" });
            }
            var dboPatient = new DboPatient()
            {
                Name = patient.Name,
                Surname = patient.Surname
            };
           
            var dboSecret = new DboSecret()
            {
                Username = patient.Username,
                UserType = (int)UserType.Patient
            };
            dboSecret.Password = _passwordHasher.HashPassword(dboSecret, patient.Password ?? string.Empty);
            try
            {
                _context.DboPatients.Add(dboPatient);                  
                _context.SaveChanges();
                    
                dboSecret.IdU = dboPatient.Id;
                _context.DboSecrets.Add(dboSecret);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
            return Ok(new { Message = "Patient registered successfully." });
        }

        [HttpPost(Name = "RegisterNewDoctor")]
       // [Authorize]
        public IActionResult RegisterNewDoctor([FromBody] NewDoctorRequest doctor)
        {
            if (string.IsNullOrEmpty(doctor.Name))
            {
                return BadRequest(new { Message = "Doctor's name missing!" });
            }
            if (string.IsNullOrEmpty(doctor.Surname))
            {
                return BadRequest(new { Message = "Doctor's surname missing!" });
            }
            if (doctor.StartShift == null || doctor.StartShift > doctor.EndShift)
            {
                return BadRequest(new { Message = "StartShift is missing or invalid!" });
            }
            if (doctor.EndShift == null)
            {
                return BadRequest(new { Message = "EndShift is missing!" });
            }
            if(UsernameExists(doctor.Username))
            {
                return BadRequest(new { Message = "Username already exists!" });
            }
            var dboDoctor = new DboDoctor()
            {
                Name = doctor.Name,
                Surname = doctor.Surname,
                StartShift = doctor.StartShift,
                EndShift = doctor.EndShift,
            };
           
            var dboSecret = new DboSecret()
            {
                Username = doctor.Username,
                UserType = (int)UserType.Doctor,
            };
            dboSecret.Password = _passwordHasher.HashPassword(dboSecret, doctor.Password ?? string.Empty);
            //TODO register db context from program.cs
            try
            {
                _context.DboDoctors.Add(dboDoctor);
                _context.SaveChanges();

                dboSecret.IdU = dboDoctor.Id;
                _context.DboSecrets.Add(dboSecret);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
            }
            return Ok(new { Message = "Doctor registered successfully." });
        }

        private bool UsernameExists(string? username)
        {
            if (username.IsNullOrEmpty()) return true;
            if (_context.DboSecrets.Any(s => s.Username == username)) return true;
            return false;
        }
        #endregion Public

        private bool IsValidTimeSlot(DateTime dateTime)
        {
            return dateTime.Minute == 0 || dateTime.Minute == Settings.SlotSize30Minutes;
        }
        public static DateTime TrimSecondsAndMilliseconds(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, 0, dateTime.Kind);
        }

    }
}
