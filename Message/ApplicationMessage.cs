using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Message
{
    [BsonIgnoreExtraElements]
    public class ApplicationMessage
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        [BsonElement("attachment")]
        public string Attachment{ get; set; } = string.Empty;

        [BsonElement("createdTime")]
        public DateTime CreatedTime { get; set; } = new DateTime();

        [BsonElement("createdBy" )]
        public CreatedBy CreatedBy { get; set; } = new CreatedBy();
        [BsonElement("updatedBy" )]
        public CreatedBy UpdatedBy { get; set; } = new CreatedBy();

    }
    public class CreatedBy
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("date")]
        public DateTime Date{ get; set; } = new DateTime();

    }
}
