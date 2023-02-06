using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;

namespace Project.Controllers
{
    public class AccountController : Controller
    {
        private readonly ProjectContext _context;
        private readonly PasswordHasher<string> hasher = new();
        public AccountController(ProjectContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            if (HttpContext.Session.Keys.Contains("User"))
            {
                var user = JsonSerializer.Deserialize<User>(
                        HttpContext.Session.GetString("User")!);
                ViewData["Title"] = user!.FirstName + " " + user.LastName;
                using var context = _context;
                if (IsAdmin())
                {
                    return View(context.Rooms.ToArray());
                }
                else
                {
                    return View(context.Bookings.Where(
                        b => b.User!.Email == user.Email).Include(r => r.Room).ToArray());
                }
            }
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.Keys.Contains("User"))
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            using (var context = _context)
            {
                var user = context.Users.Find(email.ToLower());
                if (user != null && hasher.VerifyHashedPassword(email.ToLower(), user.Password, password)
                    != PasswordVerificationResult.Failed)
                {
                    HttpContext.Session.SetString("User", JsonSerializer.Serialize(user));
                    return RedirectToAction("Index");
                }
            }
            TempData["Email"] = email;
            ViewData["Error"] = "Invalid email or password";
            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public IActionResult Registration()
        {
            if (HttpContext.Session.Keys.Contains("User"))
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [HttpPost]
        public IActionResult Registration(User user)
        {
            var errors = new string[3];
            if (!user.FirstName!.All(Char.IsLetter) || !user.LastName!.All(Char.IsLetter))
            {
                errors[0] = "Only letters are allowed for names";
            }
            using (var context = _context)
            {
                if (context.Users.Any(u => u.Email == user.Email!.ToLower()))
                {
                    errors[1] = "Email is already registered";
                }
                if (user.Password!.Length < 8)
                {
                    errors[2] = "A password must be at least eight characters";
                }
                if (errors.All(e => string.IsNullOrEmpty(e)))
                {
                    context.Users.Add(new User
                    {
                        Email = user.Email!.ToLower(),
                        Password = hasher.HashPassword(user.Email.ToLower(), user.Password),
                        FirstName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(user.FirstName!.ToLower()),
                        LastName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(user.LastName!.ToLower()),
                        PhoneNumber = user.PhoneNumber
                    });
                    context.SaveChanges();
                    TempData["Email"] = user.Email.ToLower();
                    return RedirectToAction("Login");
                }
            }
            ViewData["Errors"] = errors;
            return View();
        }
        [HttpPost]
        public IActionResult Add(Room room)
        {
            if (room.Description == null)
            {
                return View();
            }
            using var context = _context;
            context.Add(room);
            context.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpPost]
        [DataType(DataType.Date)]
        public IActionResult Edit(string edit, string del, Room room,
            DateTime? cin, DateTime? cout, int? cap, bool apply)
        {
            using var context = _context;
            if (!string.IsNullOrEmpty(edit))
            {
                if (IsAdmin())
                {
                    if (room.Description != null)
                    {
                        if (int.Parse(edit) != room.Number
                            && context.Rooms.Any(r => r.Number == room.Number))
                        {
                            ViewData["Error"] = $"There's already a room with number {room.Number}";
                            return View(room);
                        }
                        else
                        {
                            var r = context.Rooms.Find(int.Parse(edit))!;
                            if (r.Number != room.Number)
                            {
                                context.Rooms.Remove(r);
                                context.Rooms.Add(room);
                            }
                            else
                            {
                                r.Floor = room.Floor;
                                r.Capacity = room.Capacity;
                                r.Price = room.Price;
                                r.Wifi = room.Wifi;
                                r.Breakfast = room.Breakfast;
                                r.Smoking = room.Smoking;
                                r.Type = room.Type;
                                r.Description = room.Description;
                                r.Dir = room.Dir;
                            }
                            context.SaveChanges();
                            return RedirectToAction("Index");
                        }
                    }
                    return View(context.Rooms.Find(int.Parse(edit)));
                }
                else
                {
                    if (apply)
                    {
                        var errors = new string[3];
                        var b = context.Bookings.Include(b => b.Room).Where(
                            b => b.Confirmation == edit).FirstOrDefault();
                        if ((cout - cin)!.Value.TotalDays > (b!.CheckOut - b.CheckIn).TotalDays)
                        {
                            errors[0] = "The number of booked nights must be equal to the original booking";
                        }
                        if (cap > b.Room!.Capacity)
                        {
                            errors[1] = $"The booked room can't fit {cap} people";
                        }
                        if ((b.CheckIn != cin || b.CheckOut != cout)
                            && HomeController.IsRoomBookable(context, b.Room.Number,
                            (DateTime)cin!, (DateTime)cout!, (int)cap!))
                        {
                            errors[2] = $"The room {b.Room.Number} isn't available at the specified dates";
                        }
                        if (errors.All(e => string.IsNullOrEmpty(e)))
                        {
                            b.CheckIn = (DateTime)cin!;
                            b.CheckOut = (DateTime)cout!;
                            b.People = (int)cap!;
                            context.SaveChanges();
                            return RedirectToAction("Index");
                        }
                        ViewData["Errors"] = errors;
                    }
                    ViewData["code"] = edit;
                    ViewData["cin"] = ((DateTime)cin!).ToString("yyyy-MM-dd");
                    ViewData["cout"] = ((DateTime)cout!).ToString("yyyy-MM-dd");
                    ViewData["cap"] = cap;
                    return View();
                }
            }
            else
            {
                if (IsAdmin())
                {
                    context.Rooms.Remove(context.Rooms.Find(int.Parse(del))!);
                }
                else
                {
                    context.Bookings.Remove(context.Bookings.Find(del)!);
                }
                context.SaveChanges();
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public IActionResult Records(bool bookings)
        {
            ViewData["Select"] = !bookings;
            using var context = _context;
            if (bookings)
            {
                return View(context.Bookings.Include(b => b.Room).Include(b => b.User).ToArray());
            }
            return View(context.Users.Where(u => !u.Email!.EndsWith("raven.com")).ToArray());
        }
        public static bool IsAdmin(HttpContext context)
        {
            var user = JsonSerializer.Deserialize<User>(context.Session.GetString("User")!);
            return user!.Email!.EndsWith("raven.com");
        }
        public bool IsAdmin()
        {
            return IsAdmin(HttpContext);
        }
    }
}
