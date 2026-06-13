using Appointment.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Appointment.Services
{
    public class AppointmentCommandRepository
    {
        public async Task<int?> InsertAppointmentAsync(
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

        public async Task UpdateAppointmentAsync(
            SqlConnection connection,
            int idAppointment,
            UpdateAppointmentRequestDto dto)
        {
            const string sql = """
            UPDATE dbo.Appointments
            SET
                IdPatient = @IdPatient,
                IdDoctor = @IdDoctor,
                AppointmentDate = @AppointmentDate,
                Status = @Status,
                Reason = @Reason,
                InternalNotes = @InternalNotes
            WHERE IdAppointment = @IdAppointment;
            """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;
            command.Parameters.Add("@IdPatient", SqlDbType.Int).Value = dto.IdPatient;
            command.Parameters.Add("@IdDoctor", SqlDbType.Int).Value = dto.IdDoctor;
            command.Parameters.Add("@AppointmentDate", SqlDbType.DateTime).Value = dto.AppointmentDate;
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = dto.Status;
            command.Parameters.Add("@Reason", SqlDbType.NVarChar, 250).Value = dto.Reason.Trim();
            command.Parameters.Add("@InternalNotes", SqlDbType.NVarChar, 1000).Value =
                string.IsNullOrWhiteSpace(dto.InternalNotes)
                    ? DBNull.Value
                    : dto.InternalNotes.Trim();

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAppointmentAsync(
            SqlConnection connection,
            int idAppointment)
        {
            const string sql = """
            DELETE FROM dbo.Appointments
            WHERE IdAppointment = @IdAppointment;
            """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@IdAppointment", SqlDbType.Int).Value = idAppointment;

            await command.ExecuteNonQueryAsync();
        }
    }
}