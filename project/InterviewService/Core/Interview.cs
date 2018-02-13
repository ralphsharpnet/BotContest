using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechToTextWPF.Core
{
    public class Quiz
    {
        public string question { get; set; }
        public string answer { get; set; }
    }

    public class Interview
    {
        public Interview()
        {
            quiz = new List<Quiz>();
        }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public List<Quiz> quiz { get; set; }
    }
}
