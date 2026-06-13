using Appointment.DTOs;
using Microsoft.Data.SqlClient;

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
            JOIN Patients p
                ON a.IdPatient = p.IdPatient
            WHERE
                (@Status IS NULL OR a.Status = @Status)
            AND
                (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
            ORDER BY a.AppointmentDate;
            """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@Status",
                (object?)status ?? DBNull.Value);

            command.Parameters.AddWithValue("@PatientLastName",
                (object?)patientLastName ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                appointments.Add(new AppointmentListDto
                {
                    IdAppointment = reader.GetInt32(0),
                    AppointmentDate = reader.GetDateTime(1),
                    Status = reader.GetString(2),
                    Reason = reader.GetString(3),
                    PatientFullName = reader.GetString(4),
                    PatientEmail = reader.GetString(5)
                });
            }

            return appointments;
        }
    }
}
