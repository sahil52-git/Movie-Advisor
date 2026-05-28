using Movie_Advisor.Models;

namespace Movie_Advisor.ViewModels
{
    public class SearchResultViewModel
    {
        public required string SearchTerm { get; set; }
        public List<Movie> Movies { get; set; } = new();
        public int? ReleaseYear { get; set; }
    }

}
