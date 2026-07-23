using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
