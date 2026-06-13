using Appointment.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Appointment.Services
{
    public class AppointmentService
    {
        private readonly IConfiguration _configuration;
        private readonly AppointmentQueryRepository _queryRepository;
        private readonly AppointmentLookupRepository _lookupRepository;
        private readonly AppointmentCommandRepository _commandRepository;

        public AppointmentService(
            IConfiguration configuration,
            AppointmentQueryRepository queryRepository,
            AppointmentLookupRepository lookupRepository,
            AppointmentCommandRepository commandRepository)
        {
            _configuration = configuration;
            _queryRepository = queryRepository;
            _lookupRepository = lookupRepository;
            _commandRepository = commandRepository;
        }

        private string ConnectionString =>
            _configuration.GetConnectionString("DefaultConnection")!;


        public async Task<List<AppointmentListDto>> GetAppointmentsAsync(
                            string? status, 
                            string? patientLastName, 
                            string? doctorLastName)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            return await _queryRepository.GetAppointmentsAsync(
                connection,
                status,
                patientLastName,
                doctorLastName);
        }

        public async Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int idAppointment)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            return await _queryRepository.GetAppointmentByIdAsync(
                connection,
                idAppointment);
        }


        public async Task<(bool Success, AppointmentCreateError Error, string? ErrorMessage, int? IdAppointment)>
                CreateAppointmentAsync(CreateAppointmentRequestDto dto)
        {
            if (!AppointmentBusinessRules.IsAppointmentDateValid(dto.AppointmentDate))
                return (false, AppointmentCreateError.BadRequest,
                    "Termin wizyty nie może być w przeszłości.", null);

            if (!AppointmentBusinessRules.IsReasonValid(dto.Reason))
                return (false, AppointmentCreateError.BadRequest,
                    "Opis wizyty jest wymagany i nie może przekraczać 250 znaków.", null);

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            if (!await _lookupRepository.PatientExistsAndIsActiveAsync(connection, dto.IdPatient))
                return (false, AppointmentCreateError.NotFound,
                    "Pacjent nie istnieje albo jest nieaktywny.", null);

            if (!await _lookupRepository.DoctorExistsAndIsActiveAsync(connection, dto.IdDoctor))
                return (false, AppointmentCreateError.NotFound,
                    "Lekarz nie istnieje albo jest nieaktywny.", null);

            if (await _lookupRepository.DoctorHasScheduledAppointmentAtAsync(
                    connection,
                    dto.IdDoctor,
                    dto.AppointmentDate))
            {
                return (false, AppointmentCreateError.Conflict,
                    "Lekarz ma już zaplanowaną wizytę w tym terminie.", null);
            }

            var idAppointment = await _commandRepository.InsertAppointmentAsync(connection, dto);

            return (true, AppointmentCreateError.None, null, idAppointment);
        }



        public async Task<(bool Success, AppointmentUpdateError Error, string? ErrorMessage)>
                UpdateAppointmentAsync(int idAppointment, UpdateAppointmentRequestDto dto)
        {
            if (!AppointmentBusinessRules.IsStatusValid(dto.Status))
                return (false, AppointmentUpdateError.BadRequest,
                    "Status wizyty musi mieć jedną z wartości: Scheduled, Completed, Cancelled.");

            if (!AppointmentBusinessRules.IsReasonValid(dto.Reason))
                return (false, AppointmentUpdateError.BadRequest,
                    "Opis wizyty jest wymagany i nie może przekraczać 250 znaków.");

            if (!AppointmentBusinessRules.IsInternalNotesValid(dto.InternalNotes))
                return (false, AppointmentUpdateError.BadRequest,
                    "Notatki wewnętrzne nie mogą przekraczać 500 znaków.");

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            var currentState = await _lookupRepository.GetAppointmentCurrentStateAsync(connection, idAppointment);

            if (currentState is null)
                return (false, AppointmentUpdateError.NotFound,
                    "Wizyta nie istnieje.");

            if (!await _lookupRepository.PatientExistsAndIsActiveAsync(connection, dto.IdPatient))
                return (false, AppointmentUpdateError.NotFound,
                    "Pacjent nie istnieje albo jest nieaktywny.");

            if (!await _lookupRepository.DoctorExistsAndIsActiveAsync(connection, dto.IdDoctor))
                return (false, AppointmentUpdateError.NotFound,
                    "Lekarz nie istnieje albo jest nieaktywny.");

            if (currentState.Status == "Completed"
                && currentState.AppointmentDate != dto.AppointmentDate)
            {
                return (false, AppointmentUpdateError.Conflict,
                    "Nie można zmienić terminu wizyty, która ma status Completed.");
            }

            var appointmentDateChanged = currentState.AppointmentDate != dto.AppointmentDate;
            var doctorChanged = currentState.IdDoctor != dto.IdDoctor;

            if ((appointmentDateChanged || doctorChanged)
                    && await _lookupRepository.DoctorHasOtherScheduledAppointmentAtAsync(
                                    connection,
                                    dto.IdDoctor,
                                    dto.AppointmentDate,
                                    idAppointment))
            {
                return (false, AppointmentUpdateError.Conflict,
                    "Lekarz ma już inną zaplanowaną wizytę w tym terminie.");
            }

            await _commandRepository.UpdateAppointmentAsync(connection, idAppointment, dto);

            return (true, AppointmentUpdateError.None, null);
        }




        public async Task<(bool Success, AppointmentDeleteError Error, string? ErrorMessage)>
                DeleteAppointmentAsync(int idAppointment)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            var status = await _lookupRepository.GetAppointmentStatusAsync(connection, idAppointment);

            if (status is null)
            {
                return (false, AppointmentDeleteError.NotFound,
                    "Wizyta nie istnieje.");
            }

            if (status == "Completed")
            {
                return (false, AppointmentDeleteError.Conflict,
                    "Nie można usunąć wizyty o statusie Completed.");
            }

            await _commandRepository.DeleteAppointmentAsync(connection, idAppointment);

            return (true, AppointmentDeleteError.None, null);
        }

    }
}
