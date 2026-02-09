using api.Config;
using api.Middleware;
using api.Model;
using api.Utils;
using Message;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/message/{userId}")]
    public class MessageController : ControllerBase
    {
        private readonly DbManager _db;
        private readonly FileHandler _f;
        private readonly GetAuth _a;
        public MessageController(DbManager db, FileHandler f, GetAuth a) { _db = db; _f = f; _a = a; }

        [HttpGet("{messageId}")]
        public async Task<ApiResponse<MessageResponse>> GetMessage(string messageId)
        {
            ApiResponse<MessageResponse> res = new ApiResponse<MessageResponse>();

            try
            {
                var db = _db.MessageDb;
                var message = db.GetByIdAsync(messageId).Result;

                res.Result = new MessageResponse
                {
                    Id = message.Id,
                    Message = message.Message,
                    Attachment = message.Attachment,
                    CreatedTime = message.CreatedTime,
                    CreatedBy = message.CreatedBy,
                    UpdatedBy = message.UpdatedBy
                };
            }
            catch (Exception ex)
            {
                res.AddError("Failed to get message", 500, ex.Message);
            }
            return res;
        }
        [HttpGet("GetMessageForGroup/{groupId}")]
        public async Task<ApiResponse<object>> GetMessageForGroup(string userId, string groupId)
        {
            ApiResponse<object> msgRec = new ApiResponse<object>();
            var db = _db.MessageDb;
            try
            {
                var messageRec = await db.GetCollection().Aggregate().Match(x => x.MessageFrom == userId && x.MessageIn == groupId).FirstOrDefaultAsync();
                msgRec.Result = messageRec;

            }
            catch (Exception ex) { return msgRec.AddError(ex.Message); }
            return msgRec;
        }
        [HttpGet("GetMessageForFriend/{recieverId}")]
        public async Task<ApiResponse<object>> GetMessageForFrirnd(string userId, string recieverId)
        {
            ApiResponse<object> msgRec = new ApiResponse<object>();
            var db = _db.MessageDb;
            try
            {
                var messageRec = await db.GetCollection().Aggregate().Match(x => x.MessageFrom == userId && x.MessageTo == recieverId).FirstOrDefaultAsync();
                msgRec.Result = messageRec;

            }
            catch (Exception ex) { return msgRec.AddError(ex.Message); }
            return msgRec;
        }
        [HttpPost]
        public async Task<IActionResult> PostMessage(string userId,string recieverId, MessageRequest rq)
        {
            string res = null;
            var userName = User?.FindFirst("UserName")?.Value;
            var db = _db.MessageDb;
            string file = "";
            try
            {
                if (rq?.Attachment!= null && rq?.Attachment?.Length != 0)
                {
                    file = await _f.UploadPhoto(rq?.Attachment!);
                }
                ApplicationMessage newmsg = new ApplicationMessage
                {
                    MessageFrom = userId,
                    MessageTo = recieverId,
                    Message = rq?.Message ?? "",
                    Attachment = file,
                    CreatedBy = new CreatedBy { Id = userId!, Name = userName! },
                    ChatType = ChatType.Pair
                };
                res = await db.AddAsync(newmsg);
                
            }
            catch (Exception ex) { res = ex.Message; }
            return Ok(res);
        }
        [HttpPost("{groupId}")]
        public async Task<IActionResult> PostGroupMessage(string  userId ,string groupId, string recieverId,  MessageRequest rq)
        {
            string res = null;
            var userName = User?.FindFirst("UserName")?.Value;
            var db = _db.MessageDb;
            var userDb = _db.UserDb;
            var rec = userDb.GetCollection().Aggregate().Match(x => x.Id == userId).FirstOrDefaultAsync();
            var groupRec = rec.Result.Groups?.Find(g => g.Id == groupId);
            string file = "";
            try
            {
                if (rq?.Attachment!= null && rq?.Attachment?.Length != 0)
                {
                    file = await _f.UploadPhoto(rq?.Attachment!);
                }
                ApplicationMessage newmsg = new ApplicationMessage
                {
                    MessageFrom = userId,
                    MessageTo = recieverId,
                    MessageIn = groupId,
                    Message = rq?.Message ?? "",
                    Attachment = file,
                    CreatedBy = new CreatedBy { Id = userId!, Name = groupRec?.Name ?? ""},
                    ChatType = ChatType.Group
                };
                res = await db.AddAsync(newmsg);
            }
            catch (Exception ex) { return NotFound(ex.Message); }
            return Ok(res);
        }

    }
}
