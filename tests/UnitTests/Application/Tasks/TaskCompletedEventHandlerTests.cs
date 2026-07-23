using FluentAssertions;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common.Events;
using TaskFlow.Application.Tasks.EventHandlers;
using TaskFlow.Domain.Events;
using Xunit;

namespace TaskFlow.UnitTests.Application.Tasks;

public class TaskCompletedEventHandlerTests
{
    private sealed class RecordingLogger : ILogger<TaskCompletedEventHandler>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }

    [Fact]
    public async Task Handle_LogsTheCompletedTaskIdBoardIdAndAssignee()
    {
        var logger = new RecordingLogger();
        var handler = new TaskCompletedEventHandler(logger);
        var taskId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var domainEvent = new TaskCompletedEvent(taskId, boardId, assigneeId);

        await handler.Handle(new DomainEventNotification<TaskCompletedEvent>(domainEvent), CancellationToken.None);

        logger.Messages.Should().ContainSingle(m =>
            m.Contains(taskId.ToString()) && m.Contains(boardId.ToString()) && m.Contains(assigneeId.ToString()));
    }
}
