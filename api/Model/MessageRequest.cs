using Message;


namespace api.Model
{
    public class MessageRequest
    {
        public string? Message{ get; set; }
        public IFormFile? Attachment{ get; set; }
    }
    public class  CreatePairChat
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
    public class MessageResponse {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Attachment { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public CreatedBy CreatedBy { get; set; } = new CreatedBy();
        public CreatedBy UpdatedBy { get; set; } = new CreatedBy();
    }; 
}
