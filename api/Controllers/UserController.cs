using api.Config;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [ApiController]
    [Route("/api/user")]
    public class UserController : ControllerBase
    {
        private readonly DbManager _db;
        public UserController(DbManager db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> GetUser()
        {

            var uDb = _db.UserDb;
            try
            {
                var rec = await uDb.GetAllAsync();
                return Ok(rec);
            }
            catch
            {
                return NotFound();
            }

        }

    }
}
