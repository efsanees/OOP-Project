namespace HospitalAppointment.Services;

/// <summary>Raised when a business rule (conflict, availability, permission...) is violated.</summary>
public class AppointmentException(string message) : Exception(message);
