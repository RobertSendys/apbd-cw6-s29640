using Appointment.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Appointment.Services
{
    public class AppointmentService
    {
        private readonly IConfiguration _configuration;

        public AppointmentService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string ConnectionString =>
            _configuration.GetConnectionString("DefaultConnection")!;

        public async Task<List<AppointmentListDto>> GetAppointmentsAsync(
            string? status,
            string? patientLastName)
        {
            var appointments = new List<AppointmentListDto>();

            await using var connection = new SqlConnection(ConnectionString);

            await connection.OpenAsync();

            const string sql = """
            SELECT
                a.IdAppointment,
                a.AppointmentDate,
                a.Status,
                a.Reason,
                p.FirstName + ' ' + p.LastName AS PatientFullName,
                p.Email AS PatientEmail
            FROM Appointments a
            JOIN Patients p ON a.IdPatient = p.IdPatient
            WHERE (@Status IS NULL OR a.Status = @Status)
              AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
            ORDER BY a.AppointmentDate;
            """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Status",
                (object?)status ?? DBNull.Value);

            command.Parameters.AddWithValue("@PatientLastName",
                (object?)patientLastName ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync();

            var idAppointmentOrdinal = reader.GetOrdinal("IdAppointment");
            var appointmentDateOrdinal = reader.GetOrdinal("AppointmentDate");
            var statusOrdinal = reader.GetOrdinal("Status");
            var reasonOrdinal = reader.GetOrdinal("Reason");
            var patientFullNameOrdinal = reader.GetOrdinal("PatientFullName");
            var patientEmailOrdinal = reader.GetOrdinal("PatientEmail");

            while (await reader.ReadAsync())
            {
                appointments.Add(new AppointmentListDto
                {
                    IdAppointment = reader.GetInt32(idAppointmentOrdinal),
                    AppointmentDate = reader.GetDateTime(appointmentDateOrdinal),
                    Status = reader.GetString(statusOrdinal),
                    Reason = reader.GetString(reasonOrdinal),
                    PatientFullName = reader.GetString(patientFullNameOrdinal),
                    PatientEmail = reader.GetString(patientEmailOrdinal)
                });
            }

            return appointments;
        }

        public async Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(int idAppointment)
        {
            await using var connection = new SqlConnection(ConnectionString);

            await connection.OpenAsync();

            const string sql = """
        SELECT
            a.IdAppointment,
            a.AppointmentDate,
            a.Status,
            a.Reason,
            a.InternalNotes,
            a.CreatedAt,

            p.IdPatient,
            p.FirstName + ' ' + p.LastName AS PatientFullName,
            p.Email AS PatientEmail,
            p.PhoneNumber AS PatientPhone,

            d.IdDoctor,
            d.FirstName + ' ' + d.LastName AS DoctorFullName,
            d.LicenseNumber AS DoctorLicenseNumber,

            s.IdSpecialization,
            s.Name AS SpecializationName
        FROM Appointments a
        JOIN Patients p ON a.IdPatient = p.IdPatient
        JOIN Doctors d ON a.IdDoctor = d.IdDoctor
        JOIN Specializations s ON d.IdSpecialization = s.IdSpecialization
        WHERE a.IdAppointment = @IdAppointment;
        """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@IdAppointment", idAppointment);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            var idAppointmentOrdinal = reader.GetOrdinal("IdAppointment");
            var appointmentDateOrdinal = reader.GetOrdinal("AppointmentDate");
            var statusOrdinal = reader.GetOrdinal("Status");
            var reasonOrdinal = reader.GetOrdinal("Reason");
            var internalNotesOrdinal = reader.GetOrdinal("InternalNotes");
            var createdAtOrdinal = reader.GetOrdinal("CreatedAt");

            var idPatientOrdinal = reader.GetOrdinal("IdPatient");
            var patientFullNameOrdinal = reader.GetOrdinal("PatientFullName");
            var patientEmailOrdinal = reader.GetOrdinal("PatientEmail");
            var patientPhoneOrdinal = reader.GetOrdinal("PatientPhone");

            var idDoctorOrdinal = reader.GetOrdinal("IdDoctor");
            var doctorFullNameOrdinal = reader.GetOrdinal("DoctorFullName");
            var doctorLicenseNumberOrdinal = reader.GetOrdinal("DoctorLicenseNumber");

            var idSpecializationOrdinal = reader.GetOrdinal("IdSpecialization");
            var specializationNameOrdinal = reader.GetOrdinal("SpecializationName");

            return new AppointmentDetailsDto
            {
                IdAppointment = reader.GetInt32(idAppointmentOrdinal),
                AppointmentDate = reader.GetDateTime(appointmentDateOrdinal),
                Status = reader.GetString(statusOrdinal),
                Reason = reader.GetString(reasonOrdinal),
                InternalNotes = reader.IsDBNull(internalNotesOrdinal)
                    ? null
                    : reader.GetString(internalNotesOrdinal),
                CreatedAt = reader.GetDateTime(createdAtOrdinal),

                IdPatient = reader.GetInt32(idPatientOrdinal),
                PatientFullName = reader.GetString(patientFullNameOrdinal),
                PatientEmail = reader.GetString(patientEmailOrdinal),
                PatientPhone = reader.GetString(patientPhoneOrdinal),

                IdDoctor = reader.GetInt32(idDoctorOrdinal),
                DoctorFullName = reader.GetString(doctorFullNameOrdinal),
                DoctorLicenseNumber = reader.GetString(doctorLicenseNumberOrdinal),

                IdSpecialization = reader.GetInt32(idSpecializationOrdinal),
                SpecializationName = reader.GetString(specializationNameOrdinal)
            };
        }



        public async Task<(bool Success, AppointmentCreateError Error, string? ErrorMessage, int? IdAppointment)>
                    CreateAppointmentAsync(CreateAppointmentRequestDto dto)
        {
            if (!IsAppointmentDateValid(dto.AppointmentDate))
                return (false, AppointmentCreateError.BadRequest,
                    "Termin wizyty nie może być w przeszłości.", null);

            if (!IsReasonValid(dto.Reason))
                return (false, AppointmentCreateError.BadRequest,
                    "Opis wizyty jest wymagany i nie może przekraczać 250 znaków.", null);

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            if (!await PatientExistsAndIsActiveAsync(connection, dto.IdPatient))
                return (false, AppointmentCreateError.NotFound,
                    "Pacjent nie istnieje albo jest nieaktywny.", null);

            if (!await DoctorExistsAndIsActiveAsync(connection, dto.IdDoctor))
                return (false, AppointmentCreateError.NotFound,
                    "Lekarz nie istnieje albo jest nieaktywny.", null);

            if (await DoctorHasScheduledAppointmentAtAsync(connection, dto.IdDoctor, dto.AppointmentDate))
                return (false, AppointmentCreateError.Conflict,
                    "Lekarz ma już zaplanowaną wizytę w tym terminie.", null);

            int? idAppointment = await InsertAppointmentAsync(connection, dto);

            return (true, AppointmentCreateError.None, null, idAppointment);
        }




        private static bool IsReasonValid(string? reason)
        {
            return !string.IsNullOrWhiteSpace(reason) && reason.Length <= 250;
        }

        private static bool IsAppointmentDateValid(DateTime appointmentDate)
        {
            return appointmentDate > DateTime.Now;
        }

        private static async Task<bool> PatientExistsAndIsActiveAsync(
            SqlConnection connection,
            int idPatient)
        {
            const string sql = """
        SELECT COUNT(1)
        FROM dbo.Patients
        WHERE IdPatient = @IdPatient
          AND IsActive = 1;
        """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = idPatient;

            var result = (int?)await command.ExecuteScalarAsync();

            return result > 0;
        }

        private static async Task<bool> DoctorExistsAndIsActiveAsync(
            SqlConnection connection,
            int idDoctor)
        {
            const string sql = """
        SELECT COUNT(1)
        FROM dbo.Doctors
        WHERE IdDoctor = @IdDoctor
          AND IsActive = 1;
        """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;

            var result = (int?)await command.ExecuteScalarAsync();

            return result > 0;
        }

        private static async Task<bool> DoctorHasScheduledAppointmentAtAsync(
            SqlConnection connection,
            int idDoctor,
            DateTime appointmentDate)
        {
            const string sql = """
        SELECT COUNT(1)
        FROM dbo.Appointments
        WHERE IdDoctor = @IdDoctor
          AND AppointmentDate = @AppointmentDate
          AND Status = N'Scheduled';
        """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;
            command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime).Value = appointmentDate;

            var result = (int?)await command.ExecuteScalarAsync();

            return result > 0;
        }

        private static async Task<int?> InsertAppointmentAsync(
            SqlConnection connection,
            CreateAppointmentRequestDto dto)
        {
            const string sql = """
        INSERT INTO dbo.Appointments
        (
            IdPatient,
            IdDoctor,
            AppointmentDate,
            Status,
            Reason,
            InternalNotes
        )
        OUTPUT INSERTED.IdAppointment
        VALUES
        (
            @IdPatient,
            @IdDoctor,
            @AppointmentDate,
            N'Scheduled',
            @Reason,
            NULL
        );
        """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = dto.IdPatient;
            command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = dto.IdDoctor;
            command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime).Value = dto.AppointmentDate;
            command.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = dto.Reason.Trim();

            var newId = (int?)await command.ExecuteScalarAsync();

            return newId;
        }
    }
}
