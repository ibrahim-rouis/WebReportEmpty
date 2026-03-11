using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebReport.Models.Entities;
using WebReport.Services;

namespace WebReport.Controllers.Users
{
    public class UsersController : Controller
    {
        private readonly ILogger<UsersController> _logger;
        private readonly UsersService _service;
        private readonly RolesService _profilsService;
        public UsersController(UsersService service, RolesService profilsService, ILogger<UsersController> logger)
        {
            _profilsService = profilsService;
            _service = service;
            _logger = logger;
        }

        // GET: Users
        public async Task<IActionResult> Index(int? pageNumber, string searchString, int? profilId)
        {
            _logger.LogInformation("Accessing Users/Index with pageNumber: {PageNumber}, searchString: {SearchString}, profilId: {ProfilId}", pageNumber, searchString, profilId);

            // Redirect to ensure query parameters are always visible in URL
            if (pageNumber == null)
            {
                return RedirectToAction(nameof(Index), new { pageNumber = 1, searchString = searchString ?? "", profilId });
            }

            // Page index
            int pageIndex = pageNumber.Value;

            // Handle negative page index and reset to default
            if (pageIndex < 1)
            {
                return RedirectToAction(nameof(Index), new { pageNumber = 1, searchString = searchString ?? "", profilId });
            }

            // Get all profils for profil filter dropdown
            var profils = await _profilsService.GetAllProfils();
            ViewBag.ProfilsList = new SelectList(profils, "Id", "profil");

            var users = await _service.GetUsers(searchString ?? "", pageIndex, profilId);

            ViewData["SearchString"] = searchString ?? "";
            ViewData["ProfilId"] = profilId;
            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            _logger.LogInformation("Accessing Users/Details with id: {Id}", id);

            if (id == null)
            {
                _logger.LogWarning("Users/Details called without an id");
                return NotFound();
            }

            var user = await _service.GetUserById(id.Value);

            if (user == null)
            {
                _logger.LogWarning("User with id {Id} not found", id);
                return NotFound();
            }
            return View(user);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Accessing Users/Create");

            // Get all profils for selection during user creation
            var allProfils = await _profilsService.GetAllProfils();
            ViewBag.Profils = allProfils.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.profil
            }).ToList();

            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nom")] User user, int[] selectedProfils)
        {
            _logger.LogInformation("Posting to Users/Create with user: {User}, selectedProfils: {SelectedProfils}", user, selectedProfils);

            if (ModelState.IsValid)
            {
                // Add selected profils to the user
                if (selectedProfils != null && selectedProfils.Length > 0)
                {
                    user.Profils = await _profilsService.GetProfilsByIds([.. selectedProfils]);
                }

                await _service.CreateUser(user);
                return RedirectToAction(nameof(Index), new { pageNumber = 1, searchString = "", profilId = (int?)null });
            }

            // If we got here, something failed, redisplay form
            var allProfils = await _profilsService.GetAllProfils();
            ViewBag.Profils = allProfils.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.profil,
                Selected = selectedProfils?.Contains(p.Id) ?? false
            }).ToList();

            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            _logger.LogInformation("Accessing Users/Edit with id: {Id}", id);
            if (id == null)
            {
                _logger.LogWarning("Users/Edit called without an id");
                return NotFound();
            }

            var user = await _service.GetUserById(id.Value);

            if (user == null)
            {
                _logger.LogWarning("User with id {Id} not found for editing", id);
                return NotFound();
            }

            // Get all profils and mark the ones assigned to this user as selected
            var allProfils = await _profilsService.GetAllProfils();
            var userProfilIds = user.Profils?.Select(p => p.Id).ToList() ?? new List<int>();
            ViewBag.Profils = allProfils.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.profil,
                Selected = userProfilIds.Contains(p.Id)
            }).ToList();
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom")] User user, int[] selectedProfils)
        {
            _logger.LogInformation("Posting to Users/Edit with id: {Id}, user: {User}, selectedProfils: {SelectedProfils}", id, user, selectedProfils);

            if (id != user.Id)
            {
                _logger.LogWarning("User ID in URL does not match user ID in form data");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the existing user with their profils from the database
                    var userToUpdate = await _service.GetUserById(id);

                    if (userToUpdate == null)
                    {
                        return NotFound();
                    }

                    // Update the user's name
                    userToUpdate.Nom = user.Nom;

                    // Clear existing profils
                    userToUpdate.Profils?.Clear();

                    // Add selected profils
                    if (selectedProfils != null && selectedProfils.Length > 0)
                    {
                        var profilsToAdd = await _profilsService.GetProfilsByIds([.. selectedProfils]);

                        userToUpdate.Profils = profilsToAdd;
                    }

                    await _service.UpdateUser(userToUpdate);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency error while updating user with id {Id}", id);
                    if (!await UserExists(id))
                    {
                        _logger.LogWarning("User with id {Id} no longer exists during update", id);
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { pageNumber = 1, searchString = "", profilId = (int?)null });
            }

            // If we got here, something failed, redisplay form
            var allProfils = await _profilsService.GetAllProfils();
            ViewBag.Profils = allProfils.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.profil,
                Selected = selectedProfils?.Contains(p.Id) ?? false
            }).ToList();

            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            _logger.LogInformation("Accessing Users/Delete with id: {Id}", id);

            if (id == null)
            {
                _logger.LogWarning("Users/Delete called without an id");
                return NotFound();
            }

            var user = await _service.GetUserById(id.Value);

            if (user == null)
            {
                _logger.LogWarning("User with id {Id} not found for deletion", id);
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Posting to Users/DeleteConfirmed with id: {Id}", id);
            var user = await _service.GetUserById(id);
            if (user != null)
            {
                await _service.DeleteUser(id);
            }
            else
            {
                _logger.LogWarning("User with id {Id} not found during deletion", id);
                return NotFound();
            }

            return RedirectToAction(nameof(Index), new { pageNumber = 1, searchString = "", profilId = (int?)null });
        }

        private async Task<bool> UserExists(int id)
        {
            return await _service.userExists(id);
        }
    }
}
