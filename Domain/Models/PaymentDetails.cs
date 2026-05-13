namespace Domain.Models;

public class PaymentDetails
{
    public string? Method { get; set; }
    public string? Number { get; set; }
    public string? Name { get; set; }
    public decimal? Amount { get; set; }
    public string? Comment { get; set; }
}
