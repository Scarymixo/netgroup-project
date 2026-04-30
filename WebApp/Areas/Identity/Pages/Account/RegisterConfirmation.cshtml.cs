using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Areas.Identity.Pages.Account;

public class RegisterConfirmationModel : PageModel
{
    public IActionResult OnGet() => NotFound();
}