using Appointment.DTOs;
using Microsoft.Data.SqlClient;

namespace Appointment.Services
{
    public class AppointmentQueryRepository
    {
        public async Task<List<AppointmentListDto>> GetAppointmentsAsync(
            SqlConnection connection,
            string? status,
            string? patientLastName)
        {
            var appointments = new List<AppointmentListDto>();

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

        public async Task<AppointmentDetailsDto?> GetAppointmentByIdAsync(
            SqlConnection connection,
            int idAppointment)
        {
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
    }
}