using Movie_Advisor.Models;

namespace Movie_Advisor.ViewModels
{
 
        public class MovieManagementViewModel
        {
            public List<Movie> Movies { get; set; } = new List<Movie>();
            public int CurrentPage { get; set; } = 1;
            public int TotalPages { get; set; } = 1;
            public int TotalMovies { get; set; } = 0;
            public int PageSize { get; set; } = 20;
            public string? SearchTerm { get; set; }
            public string? SelectedGenre { get; set; }
        }
}
