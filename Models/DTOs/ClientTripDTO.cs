namespace Tutorial8.Models.DTOs;

public class ClientTripDTO
{
    public int TripId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? PaymentDate { get; set; }
    public List<CountryDTO> Countries { get; set; } = new();
}