using Microsoft.Data.SqlClient;
using System.Data;

namespace Appointment.Services
{
    public class AppointmentLookupRepository
    {
        public async Task<bool> PatientExistsAndIsActiveAsync(
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

        public async Task<bool> DoctorExistsAndIsActiveAsync(
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

        public async Task<bool> DoctorHasScheduledAppointmentAtAsync(
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

        public async Task<bool> DoctorHasOtherScheduledAppointmentAtAsync(
            SqlConnection connection,
            int idDoctor,
            DateTime appointmentDate,
            int idAppointment)
        {
            const string sql = """
            SELECT COUNT(1)
            FROM dbo.Appointments
            WHERE IdDoctor = @IdDoctor
              AND AppointmentDate = @AppointmentDate
              AND Status = N'Scheduled'
              AND IdAppointment <> @IdAppointment;
            """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = idDoctor;
            command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime).Value = appointmentDate;
            command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

            var result = (int?)await command.ExecuteScalarAsync();

            return result > 0;
        }

        public async Task<AppointmentCurrentState?> GetAppointmentCurrentStateAsync(
            SqlConnection connection,
            int idAppointment)
        {
            const string sql = """
            SELECT IdDoctor, AppointmentDate, Status
            FROM dbo.Appointments
            WHERE IdAppointment = @IdAppointment;
            """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new AppointmentCurrentState(
                            reader.GetInt32(reader.GetOrdinal("IdDoctor")),
                            reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
                            reader.GetString(reader.GetOrdinal("Status")));
        }

        public async Task<string?> GetAppointmentStatusAsync(
            SqlConnection connection,
            int idAppointment)
        {
            const string sql = """
            SELECT Status
            FROM dbo.Appointments
            WHERE IdAppointment = @IdAppointment;
            """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

            var result = await command.ExecuteScalarAsync();

            return result as string;
        }
    }
}