using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.BLL.Services;
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
        private readonly ParticipantService _participantService;

        public ParticipantsController(AppDbContext context, ParticipantService participantService)
        {
            _context = context;
            _participantService = participantService;
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
            var result = await _participantService.RegisterAsync(ParticipantMapper.Map(dto));

            switch (result.Status)
            {
                case ParticipantRegistrationStatus.EventNotFound:
                    return NotFound();
                case ParticipantRegistrationStatus.EventFull:
                    return Conflict(new RestApiErrorResponse
                    {
                        Status = HttpStatusCode.Conflict,
                        Error = "Event is full"
                    });
                case ParticipantRegistrationStatus.DuplicateRegistration:
                    return Conflict(new RestApiErrorResponse
                    {
                        Status = HttpStatusCode.Conflict,
                        Error = "Already registered for this event"
                    });
            }

            var mapped = ParticipantMapper.Map(result.Participant!);
            return CreatedAtAction(nameof(GetParticipant), new { id = mapped.Id, version = "1.0" }, mapped);
        }
    }
}
