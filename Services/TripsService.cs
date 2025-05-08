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
    
    public async Task<int> CreateClientAsync(CreateClientDTO dto)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var checkCmd = new SqlCommand("SELECT COUNT(1) FROM Client WHERE Pesel = @Pesel", conn);
        checkCmd.Parameters.AddWithValue("@Pesel", dto.Pesel);
        var exists = (int)await checkCmd.ExecuteScalarAsync() > 0;

        if (exists)
            throw new InvalidOperationException("Client with the same PESEL already exists.");

        var insertCmd = new SqlCommand(@"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)", conn);

        insertCmd.Parameters.AddWithValue("@FirstName", dto.FirstName);
        insertCmd.Parameters.AddWithValue("@LastName", dto.LastName);
        insertCmd.Parameters.AddWithValue("@Email", dto.Email);
        insertCmd.Parameters.AddWithValue("@Telephone", dto.Telephone);
        insertCmd.Parameters.AddWithValue("@Pesel", dto.Pesel);

        var newId = (int)await insertCmd.ExecuteScalarAsync();
        return newId;
    }

public async Task<string?> RegisterClientForTripAsync(int clientId, int tripId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var clientCheckCmd = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @IdClient", conn);
    clientCheckCmd.Parameters.AddWithValue("@IdClient", clientId);
    var clientExists = await clientCheckCmd.ExecuteScalarAsync();
    if (clientExists == null)
        return "Client not found";

    var tripCheckCmd = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip", conn);
    tripCheckCmd.Parameters.AddWithValue("@IdTrip", tripId);
    var maxPeopleObj = await tripCheckCmd.ExecuteScalarAsync();
    if (maxPeopleObj == null)
        return "Trip not found";

    var maxPeople = (int)maxPeopleObj;

    var checkExistingCmd = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", conn);
    checkExistingCmd.Parameters.AddWithValue("@IdClient", clientId);
    checkExistingCmd.Parameters.AddWithValue("@IdTrip", tripId);
    var alreadyRegistered = await checkExistingCmd.ExecuteScalarAsync();
    if (alreadyRegistered != null)
        return "Client already registered";

    var countCmd = new SqlCommand("SELECT COUNT(1) FROM Client_Trip WHERE IdTrip = @IdTrip", conn);
    countCmd.Parameters.AddWithValue("@IdTrip", tripId);
    var currentParticipants = (int)await countCmd.ExecuteScalarAsync();

    if (currentParticipants >= maxPeople)
        return "Max participants reached";

    var registeredAt = DateTime.Now.ToString("yyyyMMdd");
    var insertCmd = new SqlCommand(@"
        INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
        VALUES (@IdClient, @IdTrip, @RegisteredAt)", conn);
    insertCmd.Parameters.AddWithValue("@IdClient", clientId);
    insertCmd.Parameters.AddWithValue("@IdTrip", tripId);
    insertCmd.Parameters.AddWithValue("@RegisteredAt", registeredAt);

    await insertCmd.ExecuteNonQueryAsync();
    return null; 
}

public async Task<string?> UnregisterClientFromTripAsync(int clientId, int tripId)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    var checkCmd = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", conn);
    checkCmd.Parameters.AddWithValue("@IdClient", clientId);
    checkCmd.Parameters.AddWithValue("@IdTrip", tripId);
    var exists = await checkCmd.ExecuteScalarAsync();
    if (exists == null)
        return "Registration not found";

    var deleteCmd = new SqlCommand("DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", conn);
    deleteCmd.Parameters.AddWithValue("@IdClient", clientId);
    deleteCmd.Parameters.AddWithValue("@IdTrip", tripId);
    await deleteCmd.ExecuteNonQueryAsync();

    return null; 
}

}