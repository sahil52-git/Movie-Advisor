using System.ComponentModel.DataAnnotations;

namespace Movie_Advisor.ViewModels
{
    public class CommentViewModel
    {
        public int Id { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public string UserInitial => string.IsNullOrEmpty(UserName) ? "?" : UserName[0].ToString().ToUpper();
    }

    public class AddCommentViewModel
    {
        [Required(ErrorMessage = "Comment text is required")]
        [MaxLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters")]
        public string CommentText { get; set; } = string.Empty;

        [Required]
        public int MovieId { get; set; }
    }

    public class AdminCommentsViewModel
    {
        public List<CommentViewModel> PendingComments { get; set; } = new List<CommentViewModel>();
        public List<CommentViewModel> ApprovedComments { get; set; } = new List<CommentViewModel>();
        public List<CommentViewModel> RecentComments { get; set; } = new List<CommentViewModel>();
        public int TotalComments { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int TodayCount { get; set; }
    }
}