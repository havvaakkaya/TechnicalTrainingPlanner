using Microsoft.AspNetCore.Mvc;

namespace TechnicalTrainingPlanner.Controllers;

public sealed class HomeController : Controller
{
    // Program.cs içindeki mevcut /Home/Error hata yönlendirmesi çalışmaya devam eder.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
