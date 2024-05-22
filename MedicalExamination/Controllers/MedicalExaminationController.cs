using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalExamination.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class MedicalExaminationController : ControllerBase
    {
        private readonly ILogger<MedicalExaminationController> _logger;

        public MedicalExaminationController(ILogger<MedicalExaminationController> logger)
        {
            _logger = logger;
        }      

        [HttpGet(Name = "GetDoctors")]
        [Authorize]
        public IEnumerable<Doctor> GetDoctors()
        {
            List<Doctor>? doctors = null;
            using (var context = new ExaminationDbContext())
            { 
                var dboDoctors = context.DboDoctors.ToList();
                doctors = dboDoctors.Select(x=> x.GetDoctor()).ToList();
                return doctors;
            }          
        }

        [HttpGet(Name = "GetDoctorsAvailableSlots")]
        [Authorize]
        //TODO Check dates
        public IActionResult GetDoctorsAvailableSlots([FromQuery] int IdPatient, [FromQuery] int IdDoctor)
        {
            using (var context = new ExaminationDbContext())
            {
                var dboDoctor = context.DboDoctors.FirstOrDefault(x => x.Id == IdDoctor);
                if (dboDoctor == null)
                {
                    return NotFound(new { Message = $"Doctor with ID {IdDoctor} was not found." });
                }
                var doctor = dboDoctor.GetDoctor();
                var takenSlots = context.DboTimeSlots.Where(x => x.IdDoctor == IdDoctor);
                doctor.GetAvailableTimeslots(takenSlots, IdPatient);

                return Ok(doctor);
            }         
        }

        [HttpPost(Name = "ReserveSlot")]
        [Authorize]
        public IActionResult ReserveSlot([FromBody] TimeSlotRequest request)
        {
            request.SlotTime = TrimSecondsAndMilliseconds(request.SlotTime);
            if (!IsValidTimeSlot(request.SlotTime))
            {
                return BadRequest(new { Message = "Invalid time slot. Time must be on the hour or half-hour (e.g., 17:00 or 18:30)." });
            }
        
            using (var context = new ExaminationDbContext())
            {
                var dboDoctor = context.DboDoctors.FirstOrDefault(x => x.Id == request.DoctorId);
                if (dboDoctor == null)
                {
                    return NotFound(new { Message = $"Doctor with ID {request.DoctorId} was not found." });
                }

                var dboPatient = context.DboPatients.FirstOrDefault(x => x.Id == request.PatientId);
                if (dboPatient == null)
                {
                    return NotFound(new { Message = $"Patient with ID {request.PatientId} was not found." });
                }

                var slot = context.DboTimeSlots.FirstOrDefault(x => x.IdDoctor == request.DoctorId && x.StartTime == request.SlotTime && x.IdPatient != request.PatientId);
                if (slot != null)
                {
                    return NotFound(new { Message = $"No available slot found for Doctor with ID {request.DoctorId} at the specified time." });
                }
                slot = new DboTimeSlot()
                {
                    IdPatient = request.PatientId,
                    IdDoctor = request.DoctorId,
                    StartTime = request.SlotTime,
                    EndTime = request.SlotTime.AddMinutes(Settings.SlotSize30Minutes),
                };
                try
                {
                    context.DboTimeSlots.Add(slot);
                    context.SaveChanges();
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex.Message);
                    return BadRequest("Saving failed.");
                }
                return Ok(new { Message = "Slot reserved successfully." });
            }
        }

        [HttpPost(Name = "CancelReservation")]
        [Authorize]
        public IActionResult CancelReservation([FromBody] int IdTimeslot)
        {
            using (var context = new ExaminationDbContext())
            {
                var slot = context.DboTimeSlots.FirstOrDefault(x => x.Id == IdTimeslot);
                if (slot == null)
                {
                    return NotFound(new { Message = $"No reservation found for time slot with ID {IdTimeslot}" });
                }

                context.DboTimeSlots.Remove(slot);
                context.SaveChanges();

                return Ok(new { Message = "Reservation canceled successfully." });
            }
        }

        #region Doctor

        [HttpGet(Name = "GetFutureApointments")]
        [Authorize]
        public IActionResult GetFutureApointments([FromQuery] int IdDoctor)
        {
            using (var context = new ExaminationDbContext())
            {
                var dboDoctor = context.DboDoctors.FirstOrDefault(x => x.Id == IdDoctor);
                if (dboDoctor == null)
                {
                    return NotFound(new { Message = $"Doctor with ID {IdDoctor} was not found." });
                }
                var doctor = dboDoctor.GetDoctor();
                var futureAppointments = context.DboTimeSlots.Where(x => x.IdDoctor == IdDoctor && x.StartTime > DateTime.Now.AddMinutes(-Settings.SlotSize30Minutes))
                    .Join(
                    context.DboPatients, 
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

        }

        [HttpPost(Name = "RemoveAppointment")]
        [Authorize]
        public IActionResult RemoveAppointment([FromBody] TimeSlotRemoveRequest request)
        {
            //TODO consolidate common code
            //TODO verify slot doctor is the same doctor that is making the change
            //TODO slot times do not obey doctor working hours
            //TODO consolidate with remove request for patient - if it can be done - has authority
            using (var context = new ExaminationDbContext())
            {
                var dboDoctor = context.DboDoctors.FirstOrDefault(x => x.Id == request.DoctorId);
                if (dboDoctor == null)
                {
                    return NotFound(new { Message = $"Doctor with ID {request.DoctorId} was not found." });
                }    

                var slot = context.DboTimeSlots.FirstOrDefault(x => x.Id == request.TimeSlotId);
                if (slot == null)
                {
                    return NotFound(new { Message = $"No available slot found ID {request.TimeSlotId}." });
                }
                if(slot.IdDoctor != request.DoctorId)
                {
                    return BadRequest(new { Message = $"Current doctor does not have permission to edit slot with ID {request.TimeSlotId}" });
                }
              
                try
                {
                    context.DboTimeSlots.Remove(slot);
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return BadRequest("Saving failed.");
                }
                return Ok(new { Message = "Appointment canceled successfully." });
            }
        }

        [HttpPost(Name = "UpdateSlotTime")]
        [Authorize]
        public IActionResult UpdateSlotTime([FromBody] TimeSlotChangeRequest request )
        {
            //TODO consolidate common code
            //TODO verify slot doctor is the same doctor that is making the change
            //TODO slot times do not obey doctor working hours
            request.SlotTime = TrimSecondsAndMilliseconds(request.SlotTime);
            if (!IsValidTimeSlot(request.SlotTime))
            {
                return BadRequest(new { Message = "Invalid time slot. Time must be on the hour or half-hour (e.g., 17:00 or 18:30)." });
            }

            using (var context = new ExaminationDbContext())
            {
                var dboDoctor = context.DboDoctors.FirstOrDefault(x => x.Id == request.DoctorId);
                if (dboDoctor == null)
                {
                    return NotFound(new { Message = $"Doctor with ID {request.DoctorId} was not found." });
                }

                var slot = context.DboTimeSlots.FirstOrDefault(x => x.Id == request.TimeSlotId);
                if (slot == null)
                {
                    return NotFound(new { Message = $"No available slot found ID {request.TimeSlotId}." });
                }
                if (slot.IdDoctor != request.DoctorId)
                {
                    return BadRequest(new { Message = $"Current doctor does not have permission to edit slot with ID {request.TimeSlotId}" });
                }
                slot.StartTime = request.SlotTime;
                slot.EndTime = request.SlotTime.AddMinutes(Settings.SlotSize30Minutes);
                try
                {
                    context.DboTimeSlots.Update(slot);
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
                }
                return Ok(new { Message = "Appointment time updated successfully." });
            }
        }
        #endregion Doctor

        #region Admin
        [HttpPost(Name = "RegisterNewPatient")]
        [Authorize]
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
            var dboPatient = new DboPatient()
            {
                Name = patient.Name,
                Surname = patient.Surname
            };
            using (var context = new ExaminationDbContext())
            {
                try
                {
                    context.DboPatients.Add(dboPatient);
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
                }
            }
            return Ok(new { Message = "Patient registered successfully." });
        }

        [HttpPost(Name = "RegisterNewDoctor")]
        [Authorize]
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
            var dboDoctor = new DboDoctor()
            {
                Name = doctor.Name,
                Surname = doctor.Surname,
                StartShift = doctor.StartShift,
                EndShift = doctor.EndShift,
            };
            using (var context = new ExaminationDbContext())
            {
                try
                {
                    context.DboDoctors.Add(dboDoctor);
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return StatusCode(500, new { Message = "An error occurred while processing your request. Please try again later." });
                }
            }
            return Ok(new { Message = "Patient registered successfully." });
        }
        #endregion Admin

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
