namespace Appointment.Services
{
    public static class AppointmentBusinessRules
    {
        public static bool IsReasonValid(string? reason)
        {
            return !string.IsNullOrWhiteSpace(reason)
                   && reason.Length <= 250;
        }

        public static bool IsInternalNotesValid(string? internalNotes)
        {
            return internalNotes == null || internalNotes.Trim().Length <= 500;
        }

        public static bool IsAppointmentDateValid(DateTime appointmentDate)
        {
            return appointmentDate > DateTime.Now;
        }

        public static bool IsStatusValid(string? status)
        {
            return status is "Scheduled" or "Completed" or "Cancelled";
        }

        public static bool CanChangeAppointmentDate(
            string currentStatus,
            DateTime currentDate,
            DateTime newDate)
        {
            return currentStatus != "Completed"
                   || currentDate == newDate;
        }

        public static bool CanDeleteAppointment(string status)
        {
            return status != "Completed";
        }
    }
}