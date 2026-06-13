namespace Appointment.Services
{
    public sealed record AppointmentCurrentState(
        DateTime AppointmentDate,
        string Status
    );
}
