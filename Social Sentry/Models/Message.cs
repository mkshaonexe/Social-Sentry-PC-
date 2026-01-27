using System;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Social_Sentry.Models
{
    [Table("messages")]
    public class Message : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("content")]
        public string Content { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("avatar_url")]
        public string AvatarUrl { get; set; }

        [Column("rank")]
        public string Rank { get; set; }

        [Column("role")]
        public string Role { get; set; }

        [Column("strike_time")]
        public string StrikeTime { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("is_pinned")]
        public bool IsPinned { get; set; }

        [Column("is_verified")]
        public bool IsVerified { get; set; }
    }
}
