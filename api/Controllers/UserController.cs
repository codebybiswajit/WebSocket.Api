using api.Config;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<GetListUserResponse> GetUser()
        {
            var uDb = _db.UserDb;
            GetListUserResponse response = new GetListUserResponse();
            try
            {
                var rec = await uDb.GetAllAsync();
                List<GetUserResponse> items = new List<GetUserResponse>();

                foreach (var item in rec)
                {
                    items.Add(new GetUserResponse() {
                        Name = item.Name,
                        Email = item.Email,
                        Id = item.Id,
                        Role = item.Role
                    });
                }
                response.Items = items;
                response.Status = ResStatus.Success;
                response.Message = "Operation fulfilled successfully";
            }
            catch (Exception ex)
            {
                response.Items = [];
                response.Status = ResStatus.Failure;
                response.Message = ex.Message;
            }
            return response;

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
                    res.Role = rec?.Role;
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
