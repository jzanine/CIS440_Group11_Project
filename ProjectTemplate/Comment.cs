using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProjectTemplate
{
    public class Comment
    {
        public int commentID;
        //public int accountID;
        public string comment_content;
        public string comment_firstname;
        public string comment_lastname;
        public int replyID;
        public string reply_content;
        public string reply_firstname;
        public string reply_lastname;
        public int priority;
    }
}