using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XptProxy
{
    public class XptWorker
    {
        public string WorkerName { get; set; }
        public string WorkerPass { get; set; }

        public WorkData WorkerWork { get; set; }

        public XptWorker() {}

        public XptWorker(string userName, string passWord)
        {
            WorkerName = userName;
            WorkerPass = passWord;
        }
    }
}
