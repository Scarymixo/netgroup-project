using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.Domain;

namespace WebApp.Areas_Admin_Controllers
{
    public class ParticipantsController : Controller
    {
        private readonly AppDbContext _context;

        public ParticipantsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Participants
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Participants.Include(p => p.Event);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Participants/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var participant = await _context.Participants
                .Include(p => p.Event)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (participant == null)
            {
                return NotFound();
            }

            return View(participant);
        }

        // GET: Participants/Create
        public IActionResult Create()
        {
            ViewData["EventId"] = new SelectList(_context.Events, "Id", "EventName");
            return View();
        }

        // POST: Participants/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventId,FirstName,LastName,NationalId,Id")] Participant participant)
        {
            if (ModelState.IsValid)
            {
                var @event = await _context.Events.FindAsync(participant.EventId);
                if (@event == null)
                {
                    return NotFound();
                }

                participant.Id = Guid.NewGuid();

                await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                var currentCount = await _context.Participants.CountAsync(p => p.EventId == participant.EventId);
                if (currentCount >= @event.MaxParticipants)
                {
                    ModelState.AddModelError(string.Empty, "Event is full");
                    ViewData["EventId"] = new SelectList(_context.Events, "Id", "EventName", participant.EventId);
                    return View(participant);
                }

                _context.Add(participant);
                try
                {
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Already registered for this event");
                    ViewData["EventId"] = new SelectList(_context.Events, "Id", "EventName", participant.EventId);
                    return View(participant);
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["EventId"] = new SelectList(_context.Events, "Id", "EventName", participant.EventId);
            return View(participant);
        }

        // GET: Participants/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var participant = await _context.Participants.FindAsync(id);
            if (participant == null)
            {
                return NotFound();
            }
            ViewData["EventId"] = new SelectList(_context.Events, "Id", "EventName", participant.EventId);
            return View(participant);
        }

        // POST: Participants/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("EventId,FirstName,LastName,NationalId,Id")] Participant participant)
        {
            if (id != participant.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(participant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ParticipantExists(participant.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["EventId"] = new SelectList(_context.Events, "Id", "EventName", participant.EventId);
            return View(participant);
        }

        // GET: Participants/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var participant = await _context.Participants
                .Include(p => p.Event)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (participant == null)
            {
                return NotFound();
            }

            return View(participant);
        }

        // POST: Participants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var participant = await _context.Participants.FindAsync(id);
            if (participant != null)
            {
                _context.Participants.Remove(participant);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ParticipantExists(Guid id)
        {
            return _context.Participants.Any(e => e.Id == id);
        }
    }
}
