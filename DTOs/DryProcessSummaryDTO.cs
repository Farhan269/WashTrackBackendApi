namespace wsahRecieveDelivary.DTOs
{
    public class DryProcessSummaryDTO
    {
        public string ProcessDescription { get; set; } = string.Empty;

        public string TransactionType { get; set; } = string.Empty;

        public decimal TPL { get; set; }

        public decimal TWL { get; set; }
    }
}
