using System;
using System.Collections.Generic;
using System.Text;

namespace Msg.Core.Model
{
    public class ProduceMsgModel
    {
        public List<long> UserIds { get; set; }
        public long? EquId { get; set; }
        public long Id { get; set; }
        public string Content { get; set; }
        public long? ProjectId { get; set; }
        public string Title { get; set; }
        public string Extend { get; set; }
        public List<int> SendTypes { get; set; }
        public string MsgTopic { get; set; }
    }
}
