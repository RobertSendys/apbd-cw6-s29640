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

        [HttpGet("{idAppointment:int}")]
        public async Task<ActionResult<AppointmentDetailsDto>> GetAppointment(int idAppointment)
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(idAppointment);

            if (appointment == null)
            {
                return NotFound(new ErrorResponseDto
                {
                    Message = "Appointment not found."
                });
            }

            return Ok(appointment);
        }


        [HttpPost]
        public async Task<IActionResult> CreateAppointment(CreateAppointmentRequestDto dto)
        {
            var result = await _appointmentService.CreateAppointmentAsync(dto);

            if (!result.Success)
            {
                var error = new ErrorResponseDto
                {
                    Message = result.ErrorMessage!
                };

                return result.Error switch
                {
                    AppointmentCreateError.BadRequest => BadRequest(error),
                    AppointmentCreateError.NotFound => NotFound(error),
                    AppointmentCreateError.Conflict => Conflict(error),
                    _ => BadRequest(error)
                };
            }

            return Created(
                $"/api/appointments/{result.IdAppointment}",
                new { idAppointment = result.IdAppointment });
        }


        [HttpPut("{idAppointment:int}")]
        public async Task<IActionResult> UpdateAppointment(int idAppointment, UpdateAppointmentRequestDto dto)
        {
            var result = await _appointmentService.UpdateAppointmentAsync(idAppointment, dto);

            if (!result.Success)
            {
                var error = new ErrorResponseDto
                {
                    Message = result.ErrorMessage!
                };

                return result.Error switch
                {
                    AppointmentUpdateError.BadRequest => BadRequest(error),
                    AppointmentUpdateError.NotFound => NotFound(error),
                    AppointmentUpdateError.Conflict => Conflict(error),
                    _ => BadRequest(error)
                };
            }

            return Ok();
        }


        [HttpDelete("{idAppointment:int}")]
        public async Task<IActionResult> DeleteAppointment(int idAppointment)
        {
            var result = await _appointmentService.DeleteAppointmentAsync(idAppointment);

            if (!result.Success)
            {
                var error = new ErrorResponseDto
                {
                    Message = result.ErrorMessage!
                };

                return result.Error switch
                {
                    AppointmentDeleteError.NotFound => NotFound(error),
                    AppointmentDeleteError.Conflict => Conflict(error),
                    _ => BadRequest(error)
                };
            }

            return NoContent();
        }


    }


}
