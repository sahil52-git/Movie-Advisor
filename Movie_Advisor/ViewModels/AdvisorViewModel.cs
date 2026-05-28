using Movie_Advisor.Models;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace Movie_Advisor.Models
{
    public class AdvisorViewModel
    {
        public required string Role { get; set; }
        public required string Error { get; set; }
    }
}
