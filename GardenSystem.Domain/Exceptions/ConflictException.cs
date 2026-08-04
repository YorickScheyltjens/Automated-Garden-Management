namespace GardenSystem.Domain.Exceptions;

public sealed class ConflictException(string message) : Exception(message);
