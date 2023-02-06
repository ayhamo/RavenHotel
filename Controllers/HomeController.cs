using Microsoft.AspNetCore.Mvc;
using Project.Data;
using Project.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProjectContext _context;
        public HomeController(ProjectContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        [DataType(DataType.Date)]
        public IActionResult Search(DateTime cin, DateTime cout, int cap,
            bool wifi, bool brk, bool smk, string? sort)
        {
            if (cin >= cout || cap <= 0)
            {
                return RedirectToAction("Index");
            }
            HttpContext.Session.SetString("cin", cin.ToString());
            HttpContext.Session.SetString("cout", cout.ToString());
            HttpContext.Session.SetInt32("cap", cap);
            ViewData["wifi"] = wifi;
            ViewData["brk"] = brk;
            ViewData["smk"] = smk;
            ViewData["lp"] = sort == "lp";
            ViewData["hp"] = sort == "hp";
            ViewData["lc"] = sort == "lc";
            ViewData["hc"] = sort == "hc";
            using var context = _context;
            var except = context.Bookings.Where(
                b => cin >= b.CheckIn && cin <= b.CheckOut ||
                cout >= b.CheckIn && cout <= b.CheckOut).Select(b => b.Room!.Number);
            var query = context.Rooms.Where(r => !except.Contains(r.Number) && r.Capacity >= cap);
            if (wifi)
            {
                query = query.Where(r => r.Wifi);
            }
            if (brk)
            {
                query = query.Where(r => r.Breakfast);
            }
            if (smk)
            {
                query = query.Where(r => r.Smoking);
            }
            switch (sort)
            {
                case "lp":
                    query = query.OrderBy(r => r.Price);
                    break;
                case "hp":
                    query = query.OrderByDescending(r => r.Price);
                    break;
                case "lc":
                    query = query.OrderBy(r => r.Capacity);
                    break;
                case "hc":
                    query = query.OrderByDescending(r => r.Capacity);
                    break;
            }
            return View(query.ToArray());
        }
        public IActionResult Details(int number)
        {
            using (var context = _context)
            {
                var r = context.Rooms.Find(number);
                if (r != null)
                {
                    if (HttpContext.Session.Keys.Contains("cin"))
                    {
                        var cin = DateTime.Parse(HttpContext.Session.GetString("cin")!);
                        var cout = DateTime.Parse(HttpContext.Session.GetString("cout")!);
                        if (IsRoomBookable(context, number, cin, cout,
                            (int)HttpContext.Session.GetInt32("cap")!))
                        {
                            ViewData["tp"] = r.Price * ((int)(cout - cin).TotalDays);
                        }
                    }
                    return View(r);
                }
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public IActionResult Payment(int rn, decimal tp, string? exp)
        {
            if (HttpContext.Session.Keys.Contains("User"))
            {
                if (AccountController.IsAdmin(HttpContext))
                {
                    return RedirectToAction("Index");
                }
                var cin = DateTime.Parse(HttpContext.Session.GetString("cin")!);
                var cout = DateTime.Parse(HttpContext.Session.GetString("cout")!);
                ViewData["rn"] = rn;
                ViewData["tp"] = tp;
                ViewData["cin"] = cin.ToShortDateString();
                ViewData["cout"] = cout.ToShortDateString();
                if (exp != null)
                {
                    var now = DateTime.Now;
                    var month = int.Parse(exp[..2]);
                    var year = int.Parse(exp[3..]);
                    if (year < int.Parse(now.ToString("yy")) || month < now.Month)
                    {
                        ViewData["Error"] = "Your credit card is expired";
                        return View();
                    }
                    using var context = _context;
                    if (IsRoomBookable(context, rn, cin, cout,
                            (int)HttpContext.Session.GetInt32("cap")!))
                    {
                        var alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                        var num = "0123456789";
                        var rng = new Random();
                        while (true)
                        {
                            var code = "";
                            for (int i = 0; i < 3; i++)
                            {
                                code += alpha[rng.Next(alpha.Length)];
                            }
                            for (int i = 0; i < 3; i++)
                            {
                                code += num[rng.Next(num.Length)];
                            }
                            var array = code.ToArray();
                            for (int i = 0; i < array.Length - 1; i++)
                            {
                                var r = i + rng.Next(array.Length - i);
                                (array[i], array[r]) = (array[r], array[i]);
                            }
                            code = new string(array);
                            if (context.Bookings.All(b => b.Confirmation != code))
                            {
                                context.Bookings.Add(new Booking
                                {
                                    Confirmation = code,
                                    CheckIn = cin,
                                    CheckOut = cout,
                                    People = (int)HttpContext.Session.GetInt32("cap")!,
                                    Price = tp,
                                    Room = context.Rooms.Find(rn),
                                    User = context.Users.Find(JsonSerializer.Deserialize<User>(
                                        HttpContext.Session.GetString("User")!)!.Email)
                                });
                                context.SaveChanges();
                                return RedirectToAction("Index", "Account");
                            }
                        }
                    }
                    else
                    {
                        return RedirectToAction("Index");
                    }
                }
                return View();
            }
            return RedirectToAction("Login", "Account");
        }
        public IActionResult Support()
        {
            if (Request.Method == "POST")
            {
                return RedirectToAction("Index");
            }
            ViewData["Email"] = JsonSerializer.Deserialize<User>(HttpContext.Session.GetString("User")!)!.Email;
            return View();
        }
        public static bool IsRoomBookable(ProjectContext context, int rn, DateTime cin, DateTime cout, int cap)
        {
            var except = context.Bookings.Where(
                b => cin >= b.CheckIn && cin <= b.CheckOut ||
                cout >= b.CheckIn && cout <= b.CheckOut).Select(b => b.Room!.Number);
            var query = context.Rooms.Where(r => !except.Contains(r.Number)
            && rn == r.Number && r.Capacity >= cap);
            return query.Any();
        }
    }
}
