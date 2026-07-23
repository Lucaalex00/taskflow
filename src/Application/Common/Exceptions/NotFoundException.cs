namespace TaskFlow.Application.Common.Exceptions;

public sealed class NotFoundException(string entityName, object key)
    : Exception($"Entity \"{entityName}\" ({key}) was not found.");
