using api.Config;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;
using User;
using static api.Model.UserResponse;

namespace api.Controllers
{
    [ApiController]
    [Route("/api/user")]
    public class UserController : ControllerBase
    {
        private readonly DbManager _db;
        public UserController(DbManager db) { _db = db; }

        [HttpGet("all")]
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
        [HttpPost]
        public async Task<object> AddUser([FromBody] AddUserRequest user)
        {
            object response;
            var uDb = _db.UserDb;

            try
            {
                ApplicationUser newUser = new()
                {
                    Name = user.Name,
                    Password = user.Password,
                    Email = user.Email,
                    Role = user.Role,
                    
                };
                response = await uDb.AddAsync(newUser);
            }
            catch (Exception ex)
            {
                response = false;
            }
            return response;
        }
        [HttpGet("{id}")]
        public async Task<GetUserResponse> GetUserById(string id) {
            var db = _db.UserDb;
            GetUserResponse res = new();
            try
            {
                var rec = await db.GetByIdAsync(id);
                if (rec != null)
                {
                    res.Id = rec.Id;
                    res.Name = rec.Name;
                    res.Email = rec.Email;
                    res.Role = rec.Role;
                    res.Status = ResStatus.Success;
                }

            }
            catch (Exception ex) { 
                res.Status = ResStatus.Failure;
            }
            return res;

        }

    }
}
