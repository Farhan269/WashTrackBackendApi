// D:\test c#\wsahRecieveDelivary\DTOs\ApiResponse.cs
namespace wsahRecieveDelivary.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}