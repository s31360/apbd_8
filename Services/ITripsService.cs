using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();
    Task<List<ClientTripDTO>> GetTripsByClientId(int clientId);
    Task<int> CreateClientAsync(CreateClientDTO dto);
    Task<string?> RegisterClientForTripAsync(int clientId, int tripId);

    Task<string?> UnregisterClientFromTripAsync(int clientId, int tripId);


}