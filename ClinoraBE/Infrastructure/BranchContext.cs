using System.Security.Claims;
using ClinoraBE.Model.DAL;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ClinoraBE.Infrastructure
{
    public static class BranchContext
    {
        public const string HeaderName = "X-Branch-Id";

        public static int? ResolveBranchId(HttpRequest request, ClaimsPrincipal user)
        {
            if (request.Headers.TryGetValue(HeaderName, out var header)
                && int.TryParse(header.FirstOrDefault(), out var fromHeader)
                && fromHeader > 0)
            {
                return fromHeader;
            }

            var claim = user.FindFirstValue("BranchId");
            return int.TryParse(claim, out var fromClaim) && fromClaim > 0 ? fromClaim : null;
        }

        /// <summary>
        /// When a branch is selected, include that branch and legacy null-branch rows.
        /// </summary>
        public static bool Matches(int? recordBranchId, int? selectedBranchId)
        {
            if (selectedBranchId is null)
            {
                return true;
            }

            return recordBranchId == selectedBranchId || recordBranchId == null;
        }

        public static async Task<HashSet<int>> VisitIdsForBranchAsync(
            ClinoraContext db,
            int? branchId,
            CancellationToken ct = default)
        {
            var query = db.Visit.AsNoTracking().AsQueryable();
            if (branchId is int bid)
            {
                query = query.Where(v => v.BranchId == bid || v.BranchId == null);
            }

            var ids = await query.Select(v => v.Id).ToListAsync(ct);
            return ids.ToHashSet();
        }

        public static async Task<bool> VisitAllowedAsync(
            ClinoraContext db,
            int visitId,
            int? branchId,
            CancellationToken ct = default)
        {
            if (branchId is null)
            {
                return await db.Visit.AsNoTracking().AnyAsync(v => v.Id == visitId, ct);
            }

            return await db.Visit.AsNoTracking()
                .AnyAsync(v => v.Id == visitId && (v.BranchId == branchId || v.BranchId == null), ct);
        }
    }
}
