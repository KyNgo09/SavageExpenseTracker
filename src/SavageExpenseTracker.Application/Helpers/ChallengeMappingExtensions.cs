using SavageExpenseTracker.Application.Dtos.Challenge;
using SavageExpenseTracker.Domain.Entities;

namespace SavageExpenseTracker.Application.Helpers
{
    public static class ChallengeMappingExtensions
    {
        public static ChallengeDto ToDto(this Challenge challenge)
        {
            return new ChallengeDto
            {
                Id = challenge.Id,
                Name = challenge.Name,
                DateStart = challenge.DateStart,
                DateEnd = challenge.DateEnd,
                WinnerId = challenge.WinnerId,
                LoserId = challenge.LoserId,
                CreatedAt = challenge.CreatedAt
            };
        }
    }
}