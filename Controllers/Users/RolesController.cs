using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebReport.Models.Entities;
using WebReport.Services;

namespace WebReport.Controllers.Users
{
    public class RolesController : Controller
    {
        private readonly RolesService _service;
        private readonly ILogger<RolesController> _logger;

        public RolesController(RolesService service, ILogger<RolesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: Profils
        public async Task<IActionResult> Index(int? pageNumber, string searchString)
        {
            _logger.LogInformation("Index action called with pageNumber={PageNumber} and searchString={SearchString}", pageNumber, searchString);
            // Page index (1 by default)
            int pageIndex = pageNumber ?? 1;

            // negative values reset to default
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            var profils = await _service.GetProfils(searchString, pageIndex);

            ViewData["SearchString"] = searchString ?? "";

            return View(profils);
        }

        // GET: Profils/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            _logger.LogInformation("Details action called with id={Id}", id);
            if (id == null)
            {
                _logger.LogWarning("Details action called with null id");
                return NotFound();
            }

            var profil = await _service.GetProfilById(id.Value);

            if (profil == null)
            {
                _logger.LogWarning("Details action: No profil found with id={Id}", id);
                return NotFound();
            }
            return View(profil);
        }

        // GET: Profils/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Profils/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,profil")] Profil profilModel)
        {
            _logger.LogInformation("Create POST action called with profil name={ProfilName}", profilModel.profil);
            if (ModelState.IsValid)
            {
                await _service.CreateProfil(profilModel);
                return RedirectToAction(nameof(Index));
            }
            return View(profilModel);
        }

        // GET: Profils/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            _logger.LogInformation("Edit GET action called with id={Id}", id);
            if (id == null)
            {
                _logger.LogWarning("Edit GET action called with null id");
                return NotFound();
            }

            var profil = await _service.GetProfilById(id.Value);
            if (profil == null)
            {
                _logger.LogWarning("Edit GET action: No profil found with id={Id}", id);
                return NotFound();
            }

            return View(profil);
        }

        // POST: Profils/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,profil")] Profil profilToUpdate)
        {
            _logger.LogInformation("Edit POST action called with id={Id} and profil name={ProfilName}", id, profilToUpdate.profil);
            if (id != profilToUpdate.Id)
            {
                _logger.LogWarning("Edit POST action: id in URL does not match id in model");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _service.UpdateProfil(profilToUpdate);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency error while updating profil with id={Id}", id);
                    if (!await _service.ProfilExists(profilToUpdate.Id))
                    {
                        _logger.LogWarning("Edit POST action: No profil found with id={Id} during update", id);
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(profilToUpdate);
        }

        // GET: Profils/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            _logger.LogInformation("Delete GET action called with id={Id}", id);
            if (id == null)
            {
                _logger.LogWarning("Delete GET action called with null id");
                return NotFound();
            }

            // Get profil and include Users assigned with it
            var profil = await _service.GetProfilByIdWithUsers(id.Value);

            if (profil == null)
            {
                _logger.LogWarning("Delete GET action: No profil found with id={Id}", id);
                return NotFound();
            }

            // Check if profil is assigned to users and warn before delete
            if (profil.Users != null && profil.Users.Any())
            {
                ModelState.AddModelError("", "This profil is assigned to one or more users");
            }

            return View(profil);
        }

        // POST: Profils/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Delete POST action called with id={Id}", id);
            // Get profil and Users assigned with it
            var profil = await _service.GetProfilByIdWithUsers(id);

            if (profil != null)
            {
                await _service.DeleteProfil(profil);
            }
            else
            {
                _logger.LogWarning("Delete POST action: No profil found with id={Id}", id);
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
