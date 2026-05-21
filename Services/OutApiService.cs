using Microsoft.EntityFrameworkCore;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.DTOs;

namespace wsahRecieveDelivary.Services
{
    public class OutApiService : IOutServiceApi
    {
        private readonly ApplicationDbContext _context;

        public OutApiService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DryProcessSummaryDTO>> GetDryProcessSummaryAsync(DateOnly? fromDate,DateOnly? toDate)
        {
            var query =
                from wt in _context.WashTransactions.AsNoTracking()
                join ps in _context.ProcessStages
                    on wt.ProcessStageId equals ps.Id
                join wo in _context.WorkOrders
                    on wt.WorkOrderId equals wo.Id
                where wt.IsActive
                select new
                {
                    wt,
                    ps,
                    wo
                };

            // Date filter
            if (fromDate.HasValue)
                query = query.Where(x => x.wt.ShiftDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.wt.ShiftDate <= toDate.Value);

            // Process filter
            query = query.Where(x =>
                x.ps.Name == "1st Dry" ||
                x.ps.Name == "2nd Dry");

            var result = await query
                .GroupBy(x => new
                {
                    x.ps.Description,
                    x.wt.TransactionType
                })
                .Select(g => new DryProcessSummaryDTO
                {
                    ProcessDescription = g.Key.Description,

                    TransactionType = g.Key.TransactionType.ToString(),

                    TPL = g.Sum(x =>
                        x.wo.Unit == "Unit 1" ||
                        x.wo.Unit == "Unit 2" ||
                        x.wo.Unit == "Unit 3" ||
                        x.wo.Unit == "Unit 4" ||
                        x.wo.Unit == "Unit 5"
                            ? (long)x.wt.Quantity
                            : 0),

                    TWL = g.Sum(x =>
                        x.wo.Unit == "Unit TWL"
                            ? (long)x.wt.Quantity
                            : 0)
                })
                .ToListAsync();

            return result;
        }
    }
}
