using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly ITripsService _tripsService;

    public ClientsController(ITripsService tripsService)
    {
        _tripsService = tripsService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) ||
            string.IsNullOrWhiteSpace(dto.LastName) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Telephone) ||
            string.IsNullOrWhiteSpace(dto.Pesel))
        {
            return BadRequest("All fields are required.");
        }

        try
        {
            var newClientId = await _tripsService.CreateClientAsync(dto);
            return Created($"/api/clients/{newClientId}", new { Id = newClientId });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while creating the client.");
        }
    }
}