using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new Dictionary<int, TripDTO>();

        var query = @"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS Country
        FROM Trip t
        JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
        JOIN Country c ON ct.IdCountry = c.IdCountry
        ORDER BY t.IdTrip";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(query, conn);
        await conn.OpenAsync();

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var tripId = reader.GetInt32(0);

            if (!trips.ContainsKey(tripId))
            {
                trips[tripId] = new TripDTO
                {
                    Id = tripId,
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    Countries = new List<CountryDTO>()
                };
            }

            trips[tripId].Countries.Add(new CountryDTO
            {
                Name = reader.GetString(6)
            });
        }

        return trips.Values.ToList();
    }

    public async Task<List<ClientTripDTO>> GetTripsByClientId(int clientId)
    {
        var trips = new Dictionary<int, ClientTripDTO>();

        var query = @"
            SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                   ct.RegisteredAt, ct.PaymentDate,
                   c.Name AS Country
            FROM Client_Trip ct
            JOIN Trip t ON ct.IdTrip = t.IdTrip
            LEFT JOIN Country_Trip ctr ON t.IdTrip = ctr.IdTrip
            LEFT JOIN Country c ON ctr.IdCountry = c.IdCountry
            WHERE ct.IdClient = @IdClient
            ORDER BY t.IdTrip";

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@IdClient", clientId);

        await conn.OpenAsync();

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var tripId = reader.GetInt32(0);

            if (!trips.ContainsKey(tripId))
            {
                trips[tripId] = new ClientTripDTO
                {
                    TripId = tripId,
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    RegisteredAt = ParseIntToDate(reader.GetInt32(6)) ?? DateTime.MinValue, // required
                    PaymentDate = reader.IsDBNull(7) ? null : ParseIntToDate(reader.GetInt32(7)),
                    Countries = new List<CountryDTO>()
                };
            }

            if (!reader.IsDBNull(8))
            {
                trips[tripId].Countries.Add(new CountryDTO
                {
                    Name = reader.GetString(8)
                });
            }
        }

        return trips.Values.ToList();
    }
    
    private DateTime? ParseIntToDate(int dateInt)
    {
        var dateStr = dateInt.ToString();
        if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
        {
            return date;
        }
        return null;
    }
    
}