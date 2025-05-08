using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            var trips = await _tripsService.GetTrips();
            return Ok(trips);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrip(int id)
        {
            // if( await DoesTripExist(id)){
            //  return NotFound();
            // }
            // var trip = ... GetTrip(id);
            // return Ok();
            
            var trips = await _tripsService.GetTrips();
            return Ok(trips);
        }
        
        [HttpGet("/api/clients/{id}/trips")]
        public async Task<IActionResult> GetTripsForClient(int id)
        {
            var trips = await _tripsService.GetTripsByClientId(id);

            if (trips.Count == 0)
            {
                return NotFound($"No trips found for client with ID {id}.");
            }

            return Ok(trips);
        }
        
        [HttpPut("/api/clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClientForTrip(int id, int tripId)
        {
            var result = await _tripsService.RegisterClientForTripAsync(id, tripId);

            if (result is null)
                return Ok("Client successfully registered for the trip.");

            if (result == "Client not found")
                return NotFound("Client not found.");

            if (result == "Trip not found")
                return NotFound("Trip not found.");

            if (result == "Client already registered")
                return Conflict("Client already registered for this trip.");

            if (result == "Max participants reached")
                return Conflict("Maximum number of participants reached.");

            return StatusCode(500, "Unexpected error.");
        }

        [HttpDelete("/api/clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
        {
            var result = await _tripsService.UnregisterClientFromTripAsync(id, tripId);

            if (result == null)
                return Ok("Client successfully unregistered from the trip.");

            if (result == "Registration not found")
                return NotFound("Client is not registered for this trip.");

            return StatusCode(500, "Unexpected error occurred.");
        }

    }
}
