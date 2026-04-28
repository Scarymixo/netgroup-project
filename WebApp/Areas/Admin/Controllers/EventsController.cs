using System.Security.Claims;
using App.BLL.Services;
using App.DAL.EF;
using App.Domain;
using App.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class EventsController : Controller
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

    // GET: Events
    public async Task<IActionResult> Index()
    {
        var appDbContext = _context.Events.Include(e => e.AppUser);
        return View(await appDbContext.ToListAsync());
    }

    // GET: Events/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var @event = await _context.Events
            .Include(e => e.AppUser)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (@event == null)
        {
            return NotFound();
        }

        return View(@event);
    }

    // GET: Events/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Events/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("EventName,MaxParticipants,StartTime,EndTime")] Event @event)
    {
        if (ModelState.IsValid)
        {
            @event.Id = Guid.NewGuid();
            @event.AppUserId = Guid.Parse(_userManager.GetUserId(User)!);
            @event.StartTime = DateTime.SpecifyKind(@event.StartTime, DateTimeKind.Utc);
            @event.EndTime = DateTime.SpecifyKind(@event.EndTime, DateTimeKind.Utc);
            @event.CreatedAt = DateTime.UtcNow;
            @event.UpdatedAt = null;
            _context.Add(@event);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(@event);
    }

    // GET: Events/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var @event = await _context.Events.FindAsync(id);
        if (@event == null)
        {
            return NotFound();
        }
        return View(@event);
    }

    // POST: Events/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("EventName,MaxParticipants,StartTime,EndTime,Id")] Event @event)
    {
        if (id != @event.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var capacity = await _eventService.CheckMaxParticipantsChangeAsync(id, @event.MaxParticipants);
                if (capacity.Status == EventCapacityChangeStatus.EventNotFound)
                {
                    return NotFound();
                }
                if (capacity.Status == EventCapacityChangeStatus.BelowCurrentRegistered)
                {
                    ModelState.AddModelError(nameof(Event.MaxParticipants),
                        $"MaxParticipants cannot be lower than current registered count ({capacity.CurrentRegistered}).");
                    return View(@event);
                }

                var existing = (await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id))!;

                @event.StartTime = DateTime.SpecifyKind(@event.StartTime, DateTimeKind.Utc);
                @event.EndTime = DateTime.SpecifyKind(@event.EndTime, DateTimeKind.Utc);
                @event.AppUserId = existing.AppUserId;
                @event.CreatedAt = existing.CreatedAt;
                @event.UpdatedAt = DateTime.UtcNow;
                _context.Update(@event);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventExists(@event.Id))
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
        return View(@event);
    }

    // GET: Events/Delete/5
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var @event = await _context.Events
            .Include(e => e.AppUser)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (@event == null)
        {
            return NotFound();
        }

        return View(@event);
    }

    // POST: Events/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var @event = await _context.Events.FindAsync(id);
        if (@event != null)
        {
            _context.Events.Remove(@event);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool EventExists(Guid id)
    {
        return _context.Events.Any(e => e.Id == id);
    }
}
