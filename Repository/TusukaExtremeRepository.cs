using Dapper;
using wsahRecieveDelivary.Dapper;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.IRepository;
using wsahRecieveDelivary.Queries;

namespace wsahRecieveDelivary.Repository
{
    public class TusukaExtremeRepository : ITusukaExtremeRepository
    {
        private readonly TusukaExtremeContext _context;

        public TusukaExtremeRepository(TusukaExtremeContext context)
        {
            _context = context;
        }


        public async Task<List<WashDeliveryDTO>> GetWashDeliveryAsync(
    DateOnly? fromDate,
    DateOnly? toDate,
    List<string>? plant,
    List<string>? washUnit)
        {
            using var connection = _context.CreateConnection();

            var plantList = plant != null && plant.Any(x => !string.IsNullOrWhiteSpace(x))
                ? plant.Where(x => !string.IsNullOrWhiteSpace(x))
                       .Select(x => x.Trim())
                       .ToList()
                : new List<string> { "__NO_PLANT__" };

            var washUnitList = washUnit != null && washUnit.Any(x => !string.IsNullOrWhiteSpace(x))
                ? washUnit.Where(x => !string.IsNullOrWhiteSpace(x))
                          .Select(x => x.Trim())
                          .ToList()
                : new List<string> { "__NO_UNIT__" };

            var parameters = new
            {
                FromDate = fromDate.HasValue
                    ? fromDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,

                ToDate = toDate.HasValue
                    ? toDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,

                Plant = plantList,
                PlantCount = plantList.Contains("__NO_PLANT__") ? 0 : plantList.Count,

                WashUnit = washUnitList,
                WashUnitCount = washUnitList.Contains("__NO_UNIT__") ? 0 : washUnitList.Count
            };

            var result = await connection.QueryAsync<WashDeliveryDTO>(
                TusukaExtremeQuery.GetWashDelivery,
                parameters,
                commandTimeout: 300
            );

            return result.ToList();
        }


        public async Task<PaginatedResponseDTO<WashDeliveryDetailsDTO>> GetWashDeliveryDetailsAsync(
    DateOnly? fromDate,
    DateOnly? toDate,
    List<string>? plant,
    List<string>? washUnit,
    int pageNumber,
    int pageSize)
        {
            using var connection = _context.CreateConnection();

            if (pageNumber <= 0)
                pageNumber = 1;

            if (pageSize <= 0)
                pageSize = 20;

            var plantList = plant != null && plant.Any(x => !string.IsNullOrWhiteSpace(x))
                ? plant.Where(x => !string.IsNullOrWhiteSpace(x))
                       .Select(x => x.Trim())
                       .ToList()
                : new List<string> { "__NO_PLANT__" };

            var washUnitList = washUnit != null && washUnit.Any(x => !string.IsNullOrWhiteSpace(x))
                ? washUnit.Where(x => !string.IsNullOrWhiteSpace(x))
                          .Select(x => x.Trim())
                          .ToList()
                : new List<string> { "__NO_UNIT__" };

            var parameters = new
            {
                FromDate = fromDate.HasValue
                    ? fromDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,

                ToDate = toDate.HasValue
                    ? toDate.Value.ToDateTime(TimeOnly.MinValue)
                    : (DateTime?)null,

                Plant = plantList,
                PlantCount = plantList.Contains("__NO_PLANT__") ? 0 : plantList.Count,

                WashUnit = washUnitList,
                WashUnitCount = washUnitList.Contains("__NO_UNIT__") ? 0 : washUnitList.Count,

                Offset = (pageNumber - 1) * pageSize,
                PageSize = pageSize
            };

            var rows = (await connection.QueryAsync<WashDeliveryDetailsRowDTO>(
                TusukaExtremeQuery.GetWashDeliveryDetails,
                parameters,
                commandTimeout: 300
            )).ToList();

            var totalRecords = rows.FirstOrDefault()?.TotalRecords ?? 0;

            return new PaginatedResponseDTO<WashDeliveryDetailsDTO>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = rows.Select(x => new WashDeliveryDetailsDTO
                {
                    ProductionDate = x.ProductionDate,
                    Factory = x.Factory,
                    Unit = x.Unit,
                    Buyer = x.Buyer,
                    WorkOrderNo = x.WorkOrderNo,
                    StyleName = x.StyleName,
                    FastReactNo = x.FastReactNo,
                    Color = x.Color,
                    OrderQuantity = x.OrderQuantity,
                    WashTargetDate = x.WashTargetDate,
                    TOD = x.TOD,
                    TotalWashReceived = x.TotalWashReceived,
                    TotalWashDelivery = x.TotalWashDelivery,
                    Receive = x.Receive,
                    Delivery = x.Delivery
                }).ToList()
            };
        }
    }
}
