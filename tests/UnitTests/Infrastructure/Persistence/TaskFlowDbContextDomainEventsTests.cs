using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Events;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Events;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.UnitTests.Infrastructure.Persistence;

public class TaskFlowDbContextDomainEventsTests
{
    private sealed class RecordingPublisher : IPublisher
    {
        public List<INotification> Published { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Published.Add((INotification)notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }
    }

    private static TaskFlowDbContext CreateContext(RecordingPublisher publisher)
    {
        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TaskFlowDbContext(options, publisher);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenATaskCompletes_PublishesTaskCompletedEventAndClearsIt()
    {
        var publisher = new RecordingPublisher();
        await using var context = CreateContext(publisher);

        var board = ProjectBoard.Create("Sprint 1", Guid.NewGuid()).Value;
        var task = TaskItem.Create(board.Id, "Ship it", null, TaskPriority.Medium, null).Value;
        context.Boards.Add(board);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        task.TransitionTo(TaskState.InProgress);
        await context.SaveChangesAsync();
        publisher.Published.Should().BeEmpty("moving to InProgress doesn't raise any domain event");

        task.TransitionTo(TaskState.Done);
        await context.SaveChangesAsync();

        var notification = publisher.Published.Should().ContainSingle().Subject
            .Should().BeOfType<DomainEventNotification<TaskCompletedEvent>>().Subject;
        notification.DomainEvent.TaskId.Should().Be(task.Id);
        notification.DomainEvent.BoardId.Should().Be(board.Id);
        task.DomainEvents.Should().BeEmpty("SaveChangesAsync must clear dispatched events so they aren't re-published");
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNoDomainEventsAreRaised_PublishesNothing()
    {
        var publisher = new RecordingPublisher();
        await using var context = CreateContext(publisher);

        var board = ProjectBoard.Create("Sprint 1", Guid.NewGuid()).Value;
        context.Boards.Add(board);

        await context.SaveChangesAsync();

        publisher.Published.Should().BeEmpty();
    }
}
