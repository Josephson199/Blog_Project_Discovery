using Infrastructure.Interfaces;
using System;

namespace Blog_Project_Discovery.Models.Home
{
    public class PostSummary
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset PubDate { get; set; }
        public string Url { get; set; }

        public IOutputStrategy OutputStrategy { get; set; }
        public string Parse()
        {
            return OutputStrategy.Transform(this.Description);
        }
    }
}
