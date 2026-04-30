using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.BLL.Services;
using App.DAL.EF;
using App.Domain.Identity;
using App.DTO.v1;
using App.DTO.v1.Mappers;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using EventDto = App.DTO.v1.Event;
using ParticipantDto = App.DTO.v1.Participant;

namespace WebApp.ApiControllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class EventsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly EventService _eventService;

        public EventsController(AppDbContext context, UserManager<AppUser> userManager, EventService eventService)
        {
            _context = context;
            _userManager = userManager;
            _eventService = eventService;
        }

        // GET: api/v1/Events
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
        {
            var entities = await _context.Events
                .Select(e => new { Entity = e, RegisteredCount = e.Participants!.Count })
                .ToListAsync();
            return entities.Select(x => EventMapper.Map(x.Entity, x.RegisteredCount)).ToList();
        }

        // GET: api/v1/Events/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventDto>> GetEvent(Guid id)
        {
            var entity = await _context.Events.FindAsync(id);

            if (entity == null)
            {
                return NotFound();
            }

            var registeredCount = await _context.Participants.CountAsync(p => p.EventId == id);
            return EventMapper.Map(entity, registeredCount);
        }

        // GET: api/v1/Events/5/Participants
        [HttpGet("{id}/Participants")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(IEnumerable<ParticipantDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ParticipantDto>>> GetEventParticipants(Guid id)
        {
            if (!await _context.Events.AnyAsync(e => e.Id == id))
            {
                return NotFound();
            }

            var entities = await _context.Participants
                .Where(p => p.EventId == id)
                .ToListAsync();
            return entities.Select(ParticipantMapper.Map).ToList();
        }

        // PUT: api/v1/Events/5
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PutEvent(Guid id, EventDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest();
            }

            var capacity = await _eventService.CheckMaxParticipantsChangeAsync(id, dto.MaxParticipants);
            if (capacity.Status == EventCapacityChangeStatus.EventNotFound)
            {
                return NotFound();
            }
            if (capacity.Status == EventCapacityChangeStatus.BelowCurrentRegistered)
            {
                return BadRequest(new RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = $"MaxParticipants below current registered count ({capacity.CurrentRegistered})"
                });
            }

            var existing = (await _context.Events.FindAsync(id))!;
            existing.EventName = dto.EventName;
            existing.MaxParticipants = dto.MaxParticipants;
            existing.StartTime = dto.StartTime;
            existing.EndTime = dto.EndTime;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/v1/Events
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<EventDto>> PostEvent(EventDto dto)
        {
            var entity = EventMapper.Map(dto);
            entity.Id = Guid.NewGuid();
            entity.AppUserId = Guid.Parse(_userManager.GetUserId(User)!);
            entity.CreatedAt = DateTime.UtcNow;

            _context.Events.Add(entity);
            await _context.SaveChangesAsync();

            var result = EventMapper.Map(entity);
            return CreatedAtAction(nameof(GetEvent), new { id = result.Id, version = "1.0" }, result);
        }

        // DELETE: api/v1/Events/5
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var entity = await _context.Events.FindAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            _context.Events.Remove(entity);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
