using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNetCore.Pages
{
	public class HomeModel : PageModel
    {
        public void OnGet()
        {
			RedirectToPage("First");
        }
    }
}