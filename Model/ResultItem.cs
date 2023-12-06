using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreativeTask.Model
{
    internal class ResultItem
    {
        public string? Title { get; set; }
        public Dictionary<string, int> CommentsByDomain { get; } = new Dictionary<string, int>();
    }
}
