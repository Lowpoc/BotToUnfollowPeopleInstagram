using System;
using System.Collections.Generic;
using System.Text;

namespace AutoInsta
{
    public class Setup
    {
        public Enviorment Enviorment { get; set; }
        public ICollection<string> WhiteList { get; set; }
        public string SavePath { get; set; }
    }
}
