using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace qaire1
{
    //struct to wrap the data for the question pages
    class questionStructure
    {
        public string header = "header";
        public string footer = "footer";
        public string title = "title";
        public string shortName = "shortName";

        public List<Object> body = new List<object>();

        public ConcurrentDictionary<CheckBox, string> cbData = new ConcurrentDictionary<CheckBox, string>();

        public questionStructure(questionStructure qs)
        {
            header = qs.header;
            footer = qs.footer;
            title = qs.title;
            body = qs.body;
            cbData = qs.cbData;
        }

        public questionStructure()
        {
            header = "header";
            footer = "footer";
            title = "title";

            body = new List<object>();

            cbData = new ConcurrentDictionary<CheckBox, string>();


        }
    }
}
