using api.Config;
using api.Middleware;
using api.Model;
using api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
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
        [Authorize]
        [HttpGet("session/{userId}")]
        public async Task<IActionResult> GetSession(string userId) {
            if (userId == null) return Unauthorized();
            var res = await _db.UserDb.GetByIdAsync(userId);
            if (res == null) return NotFound();
            var jwtToken = await _auth.GenerateJwtToken(userId);

            Response.Cookies.Append("WSToken", jwtToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(_auth.GetAccessExpiryMinutes())
            });

            var response = new
            {
                Id = userId,
                Tokens = jwtToken,
                res?.Username,
                res?.Role,
                res?.Email
            };

            return Ok(response);
        }
        [HttpGet("all")]
        public async Task<ApiResponse<object>> GetUser(string search = null)
        {
            var uDb = _db.UserDb;
            ApiResponse<object> response = new ApiResponse<object>();
            try
            {


                var rec = await uDb.GetCollection()
                    .Aggregate()
                    .Match(new BsonDocument {
                        { "name", new BsonDocument {
                            { "$regex", search }
                        }}
                    }).Project(x => new { x.Id, x.Name })
                    .ToListAsync();

                response.Result = rec;
                response.Message = "Operation fulfilled successfully";
            }
            catch (Exception ex)
            {
                return response.AddError(ex?.Message ?? "Operation failed due to one or more reason");
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
                response.AddError(ex.Message, default, ex.Message);
            }
            return response;
        }
        [HttpGet("{id}")]
        public async Task<ApiResponse<GetUserResponse>> GetUserById(string id)
        {
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
                        //Status = rec.                    };
                    };

                    res.Result = resData;
                }

            }
            catch (Exception ex)
            {
                res.AddError(ex.Message ?? "Error While Performing the operation");
            }
            return res;

        }
        [Authorize]
        [HttpGet("GetContact/{userId}")]
        public async Task<ApiResponse<List<GetContactIdNameResponse>>> GetContact(string userId)
        {
            ApiResponse<List<GetContactIdNameResponse>> res = new ApiResponse<List<GetContactIdNameResponse>>();
            List<GetContactIdNameResponse> contactList = new List<GetContactIdNameResponse>();
            try
            {
                var userDb = _db.UserDb;
                var rec = await userDb.GetCollection().Aggregate().Match(x => x.Id == userId).Project(x => new { x.Pairs, x.Groups }).FirstOrDefaultAsync();
                if (rec == null) return res.AddError("Not Found");
                foreach (var item in rec.Pairs)
                {
                    contactList.Add(new GetContactIdNameResponse
                    {
                        Id = item.Id,
                        Name = item.Name
                    });
                }
                return res.AddResult(contactList);
            }
            catch (Exception ex)
            {
                return res.AddError(ex.Message);
            }
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest rq)
        {
            var userDb = _db.UserDb;
            try
            {
                var filter = Builders<ApplicationUser>.Filter.Eq(u => u.Username, rq.UserName);
                var user = await userDb.GetCollection().Find(filter).FirstOrDefaultAsync();
                if (user == null)
                    return NotFound(new { error = "User not found" });

                bool passwordMatched = UserDb.PasswordHelper.VerifyPassword(rq.Password, user.Password);
                if (!passwordMatched)
                    return Unauthorized(new { error = "Wrong password, please try again" });

                var jwtToken = await _auth.GenerateJwtToken(user.Id);
                var rawRefresh = await _auth.GenerateAndStoreRefreshTokenAsync(user.Id);

                Response.Cookies.Append("WSToken", jwtToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
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
            catch (Exception ex) { return BadRequest(); }
        }
        [Authorize]
        [Authorize(Roles = "Admin,User,SuperUser")]
        [HttpPost("logout/{userId}")]
        public async Task<IActionResult> Logout(string userId)
        {
            var userDb = _db.UserDb;
            var user = await userDb.GetByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { error = "User not found" });

            Response.Cookies.Delete("WSToken");

            return Ok(new { message = "Logout successfully" });
        }
        [Authorize]
        [HttpPost("CreatePairChat/{userId}/{contactId}")]
        public async Task<ApiResponse<string>> CreatePairChat(string userId,string contactId)
        {
            ApiResponse<string> res = new ApiResponse<string>();
            var db = _db.UserDb;
            var userRec = await db.GetByIdAsync(contactId);

            try
            {
                Pair newPair = new Pair
                {
                    Id = contactId,
                    Name = userRec.Name,
                };
                var rec = await db.CreatePair(newPair, userId);
                if (rec)
                {
                    res.Result = "Friend Created Succefully";
                    res.StatusCode = 200;
                    res.Message = "Friend Created Succefully";
                }
            }
            catch (Exception ex) { res.AddError(ex.Message); }
            return res;
        }
        [Authorize]
        [HttpPost("CreateGroup/{userId}")]
        public async Task<ApiResponse<string>> CreateGroup(NewGroupRequest rq, string userId)
        {

            ApiResponse<string> res = new ApiResponse<string>();
            var db = _db.UserDb;
            var userRec = await db.GetByIdAsync(userId);

            List<Pair> pairs = new List<Pair>();
            try
            {


                if (rq?.Members != null)
                {
                    foreach (var item in rq.Members)
                    {
                        pairs.Add(new Pair
                        {
                            Name = userRec.Name,
                            Id = item.Id,
                        });
                    }
                }

                Group newGroup = new Group
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = rq.Name,
                    Members = pairs,
                };
                var rec = await db.CreateGroup(newGroup, userId);
                if (rec)
                {
                    res.Result = "Group Created Succefully";
                    res.StatusCode = 200;
                    res.Message = "Group Created Succefully";
                }
            }
            catch (Exception ex) { res.AddError(ex.Message); }
            return res;
        }
        [Authorize]
        [HttpPost("{userId}/AddMemberToGroup/{groupId}")]
        public async Task<ApiResponse<string>> AddMemberToGroup(string userId, NewPairRequest rq, string groupId)
        {
            ApiResponse<string> res = new ApiResponse<string>();
            var db = _db.UserDb;
            var userRec = await db.GetByIdAsync(rq.Id);
            Pair newMember = new Pair
            {
                Name = userRec.Name,
                Id = rq.Id
            };
            try
            {
                var rec = await db.AddMemberToExistingGroupAsync(userId, groupId, newMember);
                if (rec)
                {
                    res.Result = "Member added succefully";
                    res.StatusCode = 200;
                    res.Message = "Group member added succefully";
                }
            }
            catch (Exception ex) { res.AddError(ex.Message); }
            return res;
        }
        [Authorize]
        [HttpPost("{userId}/GetGroup")]
        public async Task<ApiResponse<List<Group>>> GetGroup(string userId)
        {
            ApiResponse<List<Group>> res = new ApiResponse<List<Group>>();
            var db = _db.UserDb;
            try
            {
                var groupRec = await db.GetCollection().Aggregate().Match(x => x.Id == userId).Project(x => x.Groups).FirstOrDefaultAsync();
                if (groupRec == null)
                    throw new Exception("Group was not created for this user");
                res.Result = groupRec;
                res.StatusCode = 200;
                return res;
            }
            catch (Exception ex)
            {
                return res.AddError(ex.Message);
            }
        }

    }
}
