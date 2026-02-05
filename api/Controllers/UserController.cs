using api.Config;
using api.Middleware;
using api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using User;
using static api.Model.UserResponse;

namespace api.Controllers
{
    [ApiController]
    [Route("/api/user")]
    public class UserController : ControllerBase
    {
        private readonly DbManager _db;
        private readonly GetAuth _auth;
        public UserController(DbManager db, GetAuth get) { _db = db; _auth = get; }

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
                response.AddError(ex.Message, default,ex.Message);
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
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(string userName, string password)
        {
            var userDb = _db.UserDb;
            var filter = Builders<ApplicationUser>.Filter.Eq(u => u.Username, userName);
            var user = await userDb.GetCollection().Find(filter).FirstOrDefaultAsync();
            if (user == null)
                return NotFound(new { error = "User not found" });

            bool passwordMatched = UserDb.PasswordHelper.VerifyPassword(password, user.Password);
            if (!passwordMatched)
                return Unauthorized(new { error = "Wrong password, please try again" });

            var jwtToken = await _auth.GenerateJwtToken(user.Id);
            var rawRefresh = await _auth.GenerateAndStoreRefreshTokenAsync(user.Id);

            Response.Cookies.Append("PFToken", jwtToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(_auth.GetAccessExpiryMinutes())
            });

            var response = new 
            {
                Tokens = jwtToken,
                UserName = user.Username,
                Role = user.Role,
                Email = user.Email
            };

            return Ok(new { res = response, userId = user.Id });
        }
        [Authorize]
        [Authorize(Roles = "Admin,User,SuperUser")]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(string userId)
        {
            var userDb = _db.UserDb;
            var user = await userDb.GetByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { error = "User not found" });

            Response.Cookies.Delete("PFToken");

            return Ok(new { message = "Logout successfully" });
        }

    }
}
