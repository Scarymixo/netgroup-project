using System.Data;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.DTO.v1;
using App.DTO.v1.Mappers;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using ParticipantDto = App.DTO.v1.Participant;

namespace WebApp.ApiControllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ParticipantsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ParticipantsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/v1/Participants
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(IEnumerable<ParticipantDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ParticipantDto>>> GetParticipants()
        {
            var entities = await _context.Participants.ToListAsync();
            return entities.Select(ParticipantMapper.Map).ToList();
        }

        // GET: api/v1/Participants/5
        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ParticipantDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ParticipantDto>> GetParticipant(Guid id)
        {
            var entity = await _context.Participants.FindAsync(id);

            if (entity == null)
            {
                return NotFound();
            }

            return ParticipantMapper.Map(entity);
        }

        // POST: api/v1/Participants
        [HttpPost]
        [ProducesResponseType(typeof(ParticipantDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(RestApiErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ParticipantDto>> PostParticipant(ParticipantDto dto)
        {
            var @event = await _context.Events.FindAsync(dto.EventId);
            if (@event == null)
            {
                return NotFound();
            }

            var entity = ParticipantMapper.Map(dto);
            entity.Id = Guid.NewGuid();

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var currentCount = await _context.Participants.CountAsync(p => p.EventId == dto.EventId);
            if (currentCount >= @event.MaxParticipants)
            {
                return Conflict(new RestApiErrorResponse
                {
                    Status = HttpStatusCode.Conflict,
                    Error = "Event is full"
                });
            }

            _context.Participants.Add(entity);

            try
            {
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new RestApiErrorResponse
                {
                    Status = HttpStatusCode.Conflict,
                    Error = "Already registered for this event"
                });
            }

            var result = ParticipantMapper.Map(entity);
            return CreatedAtAction(nameof(GetParticipant), new { id = result.Id, version = "1.0" }, result);
        }
    }
}
