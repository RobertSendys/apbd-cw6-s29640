namespace Appointment.Services
{
    public sealed record AppointmentCurrentState(
        int IdDoctor,
        DateTime AppointmentDate,
        string Status
    );
}
