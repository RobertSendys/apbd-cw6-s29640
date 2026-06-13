using Appointment.DTOs;
using Appointment.Services;
using Microsoft.AspNetCore.Mvc;

namespace Appointment.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppointmentService _appointmentService;

        public AppointmentsController(AppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpGet]
        public async Task<ActionResult<List<AppointmentListDto>>> GetAppointments(
            [FromQuery] string? status,
            [FromQuery] string? patientLastName)
        {
            var appointments = await _appointmentService.GetAppointmentsAsync(status, patientLastName);

            return Ok(appointments);
        }
    }
}
