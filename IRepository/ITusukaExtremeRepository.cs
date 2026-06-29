using wsahRecieveDelivary.DTOs;

namespace wsahRecieveDelivary.IRepository
{
    public interface ITusukaExtremeRepository
    {

        Task<List<WashDeliveryDTO>> GetWashDeliveryAsync(
DateOnly? fromDate,
DateOnly? toDate,
List<string>? plant,
List<string>? washUnit);

        Task<PaginatedResponseDTO<WashDeliveryDetailsDTO>> GetWashDeliveryDetailsAsync(
    DateOnly? fromDate,
    DateOnly? toDate,
    List<string>? plant,
    List<string>? washUnit,
    int pageNumber,
    int pageSize);

    }
}
