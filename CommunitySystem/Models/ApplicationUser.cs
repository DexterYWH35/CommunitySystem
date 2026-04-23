using Microsoft.AspNetCore.Identity;

namespace CommunitySystem.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
    public ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();
    public ICollection<LostFoundItem> LostFoundItems { get; set; } = new List<LostFoundItem>();
    public ICollection<LostFoundClaim> LostFoundClaims { get; set; } = new List<LostFoundClaim>();
    public ICollection<ComplaintCase> ComplaintCases { get; set; } = new List<ComplaintCase>();
}
