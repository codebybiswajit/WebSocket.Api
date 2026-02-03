using api.Config;
using api.Utils;
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
        public async Task<ApiResponse<List<GetUserResponse>>> GetUser()
        {
            var uDb = _db.UserDb;
            ApiResponse<List<GetUserResponse>> response = new ApiResponse<List<GetUserResponse>>();
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
                        Role = item.Role,
                        UserName = item.Username,
                        Status = null
                        
                    });
                }
                response.Result = items;
                response.Message = "Operation fulfilled successfully";
            }
            catch (Exception ex)
            {
                response.AddError(ex?.Message ?? "Operation failed due to one or more reason");
            }
            return response;

        }
        [HttpPost]
        public async Task<ApiResponse<object>> AddUser([FromBody] AddUserRequest user)
        {
            ApiResponse<object> response = new ApiResponse<object>();
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
                var res = await uDb.AddAsync(newUser);
                response.Result = res;
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.AddError(ex.Message);
            }
            return response;
        }
        [HttpGet("{id}")]
        public async Task<ApiResponse<GetUserResponse>> GetUserById(string id) {
            var db = _db.UserDb;
            ApiResponse<GetUserResponse> res = new ApiResponse<GetUserResponse>();
            try
            {
                var rec = await db.GetByIdAsync(id);
                if (rec != null)
                {
                    var resData = new GetUserResponse
                    {
                        Id = rec.Id,
                        Name = rec.Name,
                        Email = rec.Email,
                        Role = rec?.Role,
                        UserName = rec.Username ?? "Guest-User",
                        Status = ResStatus.Success
                    };

                    res.Result = resData;
                }

            }
            catch (Exception ex) {
                res.AddError(ex.Message ?? "Error While Performing the operation");
            }
            return res;

        }

    }
}
