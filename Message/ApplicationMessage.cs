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
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        [BsonRepresentation(BsonType.ObjectId),BsonIgnoreIfDefault, BsonIgnoreIfNull]
        public string MessageFrom { get; set; } = string.Empty;
        [BsonRepresentation(BsonType.ObjectId), BsonIgnoreIfDefault, BsonIgnoreIfNull]
        public string MessageTo { get; set; } = string.Empty;
        [ BsonRepresentation(BsonType.ObjectId), BsonIgnoreIfDefault, BsonIgnoreIfNull]
        public string MessageIn { get; set; } = string.Empty;

        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        [BsonElement("attachment")]
        public string Attachment{ get; set; } = string.Empty;
        [BsonElement("type")]
        public ChatType ChatType{ get; set; }

        [BsonElement("createdTime"),BsonIgnoreIfNull]
        public DateTime CreatedTime => DateTime.UtcNow;

        [BsonElement("createdBy" )]
        public CreatedBy CreatedBy { get; set; } = new CreatedBy();
        [BsonElement("updatedBy" )]
        public CreatedBy UpdatedBy { get; set; } = new CreatedBy();

    }
    public class CreatedBy
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]

        public string Id { get; set; }
        
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("date")]
        public DateTime Date => DateTime.UtcNow;

    }

    public enum ChatType { 
        Pair,
        Group
    }
}
