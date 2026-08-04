namespace GardenSystem.Domain.Exceptions;

public sealed class OvercrowdingException(string message) : Exception(message);