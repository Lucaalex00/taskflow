using FluentAssertions;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.UnitTests.Domain;

public class BoardInvitationTests
{
    [Fact]
    public void Create_NormalizesTheEmailAndStartsPending()
    {
        var result = BoardInvitation.Create(Guid.NewGuid(), "Ada@Example.COM", null, Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value.InviteeEmail.Should().Be("ada@example.com");
        result.Value.Status.Should().Be(InvitationStatus.Pending);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Create_WithAnInvalidEmail_ReturnsFailure(string email)
    {
        var result = BoardInvitation.Create(Guid.NewGuid(), email, null, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Accept_OnAPendingInvitation_Succeeds()
    {
        var invitation = BoardInvitation.Create(Guid.NewGuid(), "ada@example.com", Guid.NewGuid(), Guid.NewGuid()).Value;

        var result = invitation.Accept();

        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Accepted);
        invitation.RespondedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Decline_OnAPendingInvitation_Succeeds()
    {
        var invitation = BoardInvitation.Create(Guid.NewGuid(), "ada@example.com", Guid.NewGuid(), Guid.NewGuid()).Value;

        var result = invitation.Decline();

        result.IsSuccess.Should().BeTrue();
        invitation.Status.Should().Be(InvitationStatus.Declined);
    }

    [Fact]
    public void Accept_AlreadyRespondedTo_ReturnsFailure()
    {
        var invitation = BoardInvitation.Create(Guid.NewGuid(), "ada@example.com", Guid.NewGuid(), Guid.NewGuid()).Value;
        invitation.Decline();

        var result = invitation.Accept();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void LinkInvitee_SetsTheInviteeUserId()
    {
        var invitation = BoardInvitation.Create(Guid.NewGuid(), "ada@example.com", null, Guid.NewGuid()).Value;
        var userId = Guid.NewGuid();

        invitation.LinkInvitee(userId);

        invitation.InviteeUserId.Should().Be(userId);
    }
}
