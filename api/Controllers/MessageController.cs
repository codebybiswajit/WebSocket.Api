using api.Config;
using api.Middleware;
using api.Model;
using api.Utils;
using Message;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/message/{messageId}")]
    public class MessageController : ControllerBase
    {
        private readonly DbManager _db;
        private readonly FileHandler _f;
        private readonly GetAuth _a;
        public MessageController(DbManager db, FileHandler f, GetAuth a) { _db = db; _f = f;_a = a; }

        [HttpGet]
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
        [HttpPost]
        public async Task<string> AddMessage(MessageRequest rq) {
            string res = null;
            var db = _db.MessageDb;
            string file = "";
            try {
                if (rq?.Attachment?.Length != 0)
                    await _f.UploadPhoto(rq.Attachment);
                ApplicationMessage newmsg = new ApplicationMessage { 
                    Message = rq.Message ?? "",
                    Attachment = file,
                    CreatedBy = new CreatedBy {Id = User?.FindFirst("UserId")?.Value ?? "", Name = User?.FindFirst("UserName")?.Value ?? ""},
                };
                var rec = db.AddAsync(newmsg);
            } catch (Exception ex) { }
            return res;
        }
        [HttpPost("CreatePairChat")]
        public async Task<string> CreatePairChat(MessageRequest rq) {
            string res = null;
            var db = _db.MessageDb;
            string file = "";
            try {
                if (rq?.Attachment?.Length != 0)
                    await _f.UploadPhoto(rq.Attachment);
                ApplicationMessage newmsg = new ApplicationMessage { 
                    Message = rq.Message ?? "",
                    Attachment = file,
                    CreatedBy = new CreatedBy {Id = User?.FindFirst("UserId")?.Value ?? "", Name = User?.FindFirst("UserName")?.Value ?? ""},
                };
                var rec = db.AddAsync(newmsg);
            } catch (Exception ex) { }
            return res;
        }
    }
}
