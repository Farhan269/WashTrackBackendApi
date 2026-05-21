using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.Extensions
{
    /// <summary>
    /// Extension methods for IQueryable to support searching, filtering, and sorting
    /// These methods are designed to work with Entity Framework Core LINQ queries
    /// </summary>
    public static class QueryableExtensions
    {
        // ==========================================
        // WORK ORDER SEARCH - Searches across ALL fields
        // ==========================================
        /// <summary>
        /// Fast search across all WorkOrder fields
        /// Searches: WorkOrderNo, Buyer, StyleName, Color, WashType, Factory, Line, Unit, 
        ///          FastReactNo, BuyerDepartment, Marks, OrderQuantity, CutQty, 
        ///          TotalWashReceived, TotalWashDelivery, WashBalance
        /// </summary>
        /// <param name="query">The IQueryable WorkOrder query</param>
        /// <param name="searchTerm">The search term to look for (case-insensitive)</param>
        /// <returns>Filtered IQueryable query</returns>
        public static IQueryable<WorkOrder> Search(
            this IQueryable<WorkOrder> query,
            string? searchTerm)
        {
            // ✅ If search term is empty, return unfiltered query
            if (string.IsNullOrWhiteSpace(searchTerm))
                return query;

            var lowerSearchTerm = searchTerm.ToLower().Trim();

            return query.Where(w =>
                // ✅ Text fields - Case insensitive contains search
                w.WorkOrderNo.ToLower().Contains(lowerSearchTerm) ||
                w.Buyer.ToLower().Contains(lowerSearchTerm) ||
                w.StyleName.ToLower().Contains(lowerSearchTerm) ||
                w.Color.ToLower().Contains(lowerSearchTerm) ||
                w.WashType.ToLower().Contains(lowerSearchTerm) ||
                w.Factory.ToLower().Contains(lowerSearchTerm) ||
                w.Line.ToLower().Contains(lowerSearchTerm) ||
                w.Unit.ToLower().Contains(lowerSearchTerm) ||
                w.FastReactNo.ToLower().Contains(lowerSearchTerm) ||
                w.BuyerDepartment.ToLower().Contains(lowerSearchTerm) ||
                (w.Marks != null && w.Marks.ToLower().Contains(lowerSearchTerm)) ||

                // ✅ Number fields - Convert to string for search
                w.OrderQuantity.ToString().Contains(lowerSearchTerm) ||
                w.CutQty.ToString().Contains(lowerSearchTerm) ||
                w.TotalWashReceived.ToString().Contains(lowerSearchTerm) ||
                w.TotalWashDelivery.ToString().Contains(lowerSearchTerm) ||
                w.WashBalance.ToString().Contains(lowerSearchTerm)
            );
        }

        // ==========================================
        // WORK ORDER ADVANCED FILTERS
        // ==========================================
        /// <summary>
        /// Apply multiple advanced filters to WorkOrder query
        /// </summary>
        /// <param name="query">The IQueryable WorkOrder query</param>
        /// <param name="factory">Filter by factory (exact match)</param>
        /// <param name="buyer">Filter by buyer (contains)</param>
        /// <param name="washType">Filter by wash type (contains)</param>
        /// <param name="line">Filter by line (contains)</param>
        /// <param name="unit">Filter by unit (contains)</param>
        /// <param name="fromDate">Filter from date (inclusive)</param>
        /// <param name="toDate">Filter to date (inclusive)</param>
        /// <returns>Filtered IQueryable query</returns>
        public static IQueryable<WorkOrder> ApplyFilters(
            this IQueryable<WorkOrder> query,
            string? factory,
            string? buyer,
            string? washType,
            string? line,
            string? unit,
            DateTime? fromDate,
            DateTime? toDate)
        {
            // ✅ Factory filter - EXACT match (case-insensitive)
            if (!string.IsNullOrWhiteSpace(factory))
                query = query.Where(w => w.Factory.ToLower() == factory.ToLower());

            // ✅ Buyer filter - CONTAINS match (case-insensitive)
            if (!string.IsNullOrWhiteSpace(buyer))
                query = query.Where(w => w.Buyer.ToLower().Contains(buyer.ToLower()));

            // ✅ WashType filter - CONTAINS match (case-insensitive)
            if (!string.IsNullOrWhiteSpace(washType))
                query = query.Where(w => w.WashType.ToLower().Contains(washType.ToLower()));

            // ✅ Line filter - CONTAINS match (case-insensitive)
            if (!string.IsNullOrWhiteSpace(line))
                query = query.Where(w => w.Line.ToLower().Contains(line.ToLower()));

            // ✅ Unit filter - CONTAINS match (case-insensitive)
            if (!string.IsNullOrWhiteSpace(unit))
                query = query.Where(w => w.Unit.ToLower().Contains(unit.ToLower()));

            // ✅ Date Range Filter - Compare date parts only (ignore time)
            if (fromDate.HasValue)
                query = query.Where(w => w.CreatedAt.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(w => w.CreatedAt.Date <= toDate.Value.Date);

            return query;
        }

        // ==========================================
        // WORK ORDER DYNAMIC SORTING
        // ==========================================
        /// <summary>
        /// Apply dynamic sorting to WorkOrder query
        /// Supported sort fields: workorderno, buyer, stylename, washtype, washtargetdate,
        ///                       orderquantity, washbalance, factory, line, updatedat, createdat
        /// </summary>
        /// <param name="query">The IQueryable WorkOrder query</param>
        /// <param name="sortBy">Field to sort by (case-insensitive)</param>
        /// <param name="sortOrder">Sort order: 'asc' or 'desc'</param>
        /// <returns>Sorted IQueryable query</returns>
        public static IQueryable<WorkOrder> ApplySort(
            this IQueryable<WorkOrder> query,
            string? sortBy,
            string sortOrder)
        {
            // ✅ Default sort field
            if (string.IsNullOrWhiteSpace(sortBy))
                sortBy = "CreatedAt";

            // ✅ Determine sort direction
            var isAscending = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

            // ✅ Switch statement for dynamic sorting
            query = sortBy.ToLower() switch
            {
                "workorderno" => isAscending
                    ? query.OrderBy(w => w.WorkOrderNo)
                    : query.OrderByDescending(w => w.WorkOrderNo),

                "buyer" => isAscending
                    ? query.OrderBy(w => w.Buyer)
                    : query.OrderByDescending(w => w.Buyer),

                "stylename" => isAscending
                    ? query.OrderBy(w => w.StyleName)
                    : query.OrderByDescending(w => w.StyleName),

                "washtype" => isAscending
                    ? query.OrderBy(w => w.WashType)
                    : query.OrderByDescending(w => w.WashType),

                "washtargetdate" => isAscending
                    ? query.OrderBy(w => w.WashTargetDate)
                    : query.OrderByDescending(w => w.WashTargetDate),

                "orderquantity" => isAscending
                    ? query.OrderBy(w => w.OrderQuantity)
                    : query.OrderByDescending(w => w.OrderQuantity),

                "washbalance" => isAscending
                    ? query.OrderBy(w => w.WashBalance)
                    : query.OrderByDescending(w => w.WashBalance),

                "factory" => isAscending
                    ? query.OrderBy(w => w.Factory)
                    : query.OrderByDescending(w => w.Factory),

                "line" => isAscending
                    ? query.OrderBy(w => w.Line)
                    : query.OrderByDescending(w => w.Line),

                "updatedat" => isAscending
                    ? query.OrderBy(w => w.UpdatedAt)
                    : query.OrderByDescending(w => w.UpdatedAt),

                // ✅ Default: Sort by CreatedAt
                _ => isAscending
                    ? query.OrderBy(w => w.CreatedAt)
                    : query.OrderByDescending(w => w.CreatedAt)
            };

            return query;
        }


        // ==========================================
        // WASH TRANSACTION FAST SEARCH
        // ==========================================
        /// <summary>
        /// Fast search across all WashTransaction fields and related WorkOrder/ProcessStage data
        /// Searches: WorkOrderNo, Buyer, StyleName, Factory, Line, Color, ProcessStageName,
        ///          BatchNo, GatePassNo, Remarks, ReceivedBy, DeliveredTo, Quantity
        /// </summary>
        /// <param name="query">The IQueryable WashTransaction query</param>
        /// <param name="searchTerm">The search term to look for (case-insensitive)</param>
        /// <returns>Filtered IQueryable query</returns>
        public static IQueryable<WashTransaction> SearchTransaction(
            this IQueryable<WashTransaction> query,
            string? searchTerm)
        {
            // ✅ If search term is empty, return unfiltered query
            if (string.IsNullOrWhiteSpace(searchTerm))
                return query;

            var lowerSearchTerm = searchTerm.ToLower().Trim();

            return query.Where(t =>
                // ✅ FIXED: Added null checks for navigation properties
                // Related WorkOrder fields
                (t.WorkOrder != null && (
                    t.WorkOrder.WorkOrderNo.ToLower().Contains(lowerSearchTerm) ||
                    t.WorkOrder.Buyer.ToLower().Contains(lowerSearchTerm) ||
                    t.WorkOrder.StyleName.ToLower().Contains(lowerSearchTerm) ||
                    t.WorkOrder.Factory.ToLower().Contains(lowerSearchTerm) ||
                    t.WorkOrder.Line.ToLower().Contains(lowerSearchTerm) ||
                    t.WorkOrder.Color.ToLower().Contains(lowerSearchTerm)
                )) ||

                // ✅ FIXED: Added null check for ProcessStage
                // ProcessStage fields
                (t.ProcessStage != null && t.ProcessStage.Name.ToLower().Contains(lowerSearchTerm)) ||

                // ✅ Transaction fields with null checks for nullable properties
                (t.BatchNo != null && t.BatchNo.ToLower().Contains(lowerSearchTerm)) ||
                (t.GatePassNo != null && t.GatePassNo.ToLower().Contains(lowerSearchTerm)) ||
                (t.Remarks != null && t.Remarks.ToLower().Contains(lowerSearchTerm)) ||
                (t.ReceivedBy != null && t.ReceivedBy.ToLower().Contains(lowerSearchTerm)) ||
                (t.DeliveredTo != null && t.DeliveredTo.ToLower().Contains(lowerSearchTerm)) ||

                // ✅ Number fields - Convert to string for search
                t.Quantity.ToString().Contains(lowerSearchTerm)
            );
        }

        // ==========================================
        // WASH TRANSACTION ADVANCED FILTERS
        // ==========================================
        /// <summary>
        /// Apply multiple advanced filters to WashTransaction query
        /// </summary>
        /// <param name="query">The IQueryable WashTransaction query</param>
        /// <param name="buyer">Filter by buyer (contains)</param>
        /// <param name="factory">Filter by factory (exact match)</param>
        /// <param name="processStageId">Filter by process stage ID</param>
        /// <param name="transactionTypeId">Filter by transaction type: 0=Receive, 1=Delivery</param>
        /// <param name="fromDate">Filter from date (inclusive)</param>
        /// <param name="toDate">Filter to date (inclusive)</param>
        /// <returns>Filtered IQueryable query</returns>
        // ==========================================
        // WASH TRANSACTION ADVANCED FILTERS (UPDATED)
        // ==========================================
        // ==========================================
        // WASH TRANSACTION ADVANCED FILTERS (UPDATED)
        // ==========================================
        public static IQueryable<WashTransaction> ApplyTransactionFilters(
            this IQueryable<WashTransaction> query,
            string? buyer,
            string? factory,
            string? unit, // ✅ ADDED
            int? processStageId,
            int? transactionTypeId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (!string.IsNullOrWhiteSpace(buyer))
                query = query.Where(t => t.WorkOrder.Buyer.ToLower().Contains(buyer.ToLower()));

            if (!string.IsNullOrWhiteSpace(factory))
                query = query.Where(t => t.WorkOrder.Factory.ToLower() == factory.ToLower());

            // ✅ ADDED: Unit filter
            if (!string.IsNullOrWhiteSpace(unit))
                query = query.Where(t => t.WorkOrder.Unit.ToLower() == unit.ToLower());

            if (processStageId.HasValue)
                query = query.Where(t => t.ProcessStageId == processStageId.Value);

            if (transactionTypeId.HasValue)
                query = query.Where(t => (int)t.TransactionType == transactionTypeId.Value);

            if (fromDate.HasValue)
                query = query.Where(t => t.TransactionDate.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(t => t.TransactionDate.Date <= toDate.Value.Date);

            return query;
        }

        // ==========================================
        // WASH TRANSACTION DYNAMIC SORTING
        // ==========================================
        /// <summary>
        /// Apply dynamic sorting to WashTransaction query
        /// Supported sort fields: workorderno, buyer, stylename, quantity, transactiontype,
        ///                       stage, factory, createdat, transactiondate
        /// </summary>
        /// <param name="query">The IQueryable WashTransaction query</param>
        /// <param name="sortBy">Field to sort by (case-insensitive)</param>
        /// <param name="sortOrder">Sort order: 'asc' or 'desc'</param>
        /// <returns>Sorted IQueryable query</returns>
        public static IQueryable<WashTransaction> ApplyTransactionSort(
            this IQueryable<WashTransaction> query,
            string? sortBy,
            string sortOrder)
        {
            // ✅ Default sort field
            if (string.IsNullOrWhiteSpace(sortBy))
                sortBy = "TransactionDate";

            // ✅ Determine sort direction
            var isAscending = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

            // ✅ Switch statement for dynamic sorting
            query = sortBy.ToLower() switch
            {
                "workorderno" => isAscending
                    ? query.OrderBy(t => t.WorkOrder.WorkOrderNo)
                    : query.OrderByDescending(t => t.WorkOrder.WorkOrderNo),

                "buyer" => isAscending
                    ? query.OrderBy(t => t.WorkOrder.Buyer)
                    : query.OrderByDescending(t => t.WorkOrder.Buyer),

                "stylename" => isAscending
                    ? query.OrderBy(t => t.WorkOrder.StyleName)
                    : query.OrderByDescending(t => t.WorkOrder.StyleName),

                "quantity" => isAscending
                    ? query.OrderBy(t => t.Quantity)
                    : query.OrderByDescending(t => t.Quantity),

                "transactiontype" => isAscending
                    ? query.OrderBy(t => t.TransactionType)
                    : query.OrderByDescending(t => t.TransactionType),

                "stage" => isAscending
                    ? query.OrderBy(t => t.ProcessStage.Name)
                    : query.OrderByDescending(t => t.ProcessStage.Name),

                "factory" => isAscending
                    ? query.OrderBy(t => t.WorkOrder.Factory)
                    : query.OrderByDescending(t => t.WorkOrder.Factory),

                "createdat" => isAscending
                    ? query.OrderBy(t => t.CreatedAt)
                    : query.OrderByDescending(t => t.CreatedAt),

                // ✅ ADDED: Explicit "transactiondate" case
                "transactiondate" => isAscending
                    ? query.OrderBy(t => t.TransactionDate)
                    : query.OrderByDescending(t => t.TransactionDate),

                // ✅ Default: Sort by TransactionDate descending (most recent first)
                _ => isAscending
                    ? query.OrderBy(t => t.TransactionDate)
                    : query.OrderByDescending(t => t.TransactionDate)
            };

            return query;
        }
    }
}