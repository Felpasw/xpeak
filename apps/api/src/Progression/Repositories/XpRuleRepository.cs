using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;

namespace Xpeak.Api.Progression.Repositories;

public sealed class XpRuleRepository(AppDbContext db) : IXpRuleRepository
{
    public Task<XpRule?> GetByIdAsync(Guid ruleId, CancellationToken ct = default) =>
        db.XpRules
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == ruleId, ct);
}
