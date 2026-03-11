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

        // GET: Roles
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

            var roles = await _service.GetRoles(searchString, pageIndex);

            ViewData["SearchString"] = searchString ?? "";

            return View("~/Views/UsersMgr/Roles/Index.cshtml", roles);
        }

        // GET: Roles/Create
        public IActionResult Create()
        {
            return View("~/Views/UsersMgr/Roles/Create.cshtml");
        }

        // POST: Roles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Role role)
        {
            _logger.LogInformation("Create POST action called with Role name={RoleName}", role.Name);
            if (ModelState.IsValid)
            {
                await _service.CreateRole(role);
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/UsersMgr/Roles/Create.cshtml", role);
        }

        // GET: Roles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            _logger.LogInformation("Edit GET action called with id={Id}", id);
            if (id == null)
            {
                _logger.LogWarning("Edit GET action called with null id");
                return NotFound();
            }

            var role = await _service.GetRoleById(id.Value);
            if (role == null)
            {
                _logger.LogWarning("Edit GET action: No role found with id={Id}", id);
                return NotFound();
            }

            return View("~/Views/UsersMgr/Roles/Edit.cshtml", role);
        }

        // POST: Roles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Role roleToUpdate)
        {
            _logger.LogInformation("Edit POST action called with id={Id} and role name={RoleName}", id, roleToUpdate.Name);
            if (id != roleToUpdate.Id)
            {
                _logger.LogWarning("Edit POST action: id in URL does not match id in model");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _service.UpdateRole(roleToUpdate);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency error while updating role with id={Id}", id);
                    if (!await _service.RoleExists(roleToUpdate.Id))
                    {
                        _logger.LogWarning("Edit POST action: No role found with id={Id} during update", id);
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/UsersMgr/Roles/Edit.cshtml", roleToUpdate);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Delete POST action called with id={Id}", id);

            var role = await _service.GetRoleByIdWithUsers(id);

            if (role == null)
            {
                _logger.LogWarning("Delete POST action: No role found with id={Id}", id);
                return Json(new { success = false, message = "Role not found." });
            }

            await _service.DeleteRole(role);

            // Optionally, you can return pageNumber and searchString in the response if needed
            return Json(new { success = true });
        }
    }
}
