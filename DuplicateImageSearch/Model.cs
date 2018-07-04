using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateImageSearch
{
    class ImageGroup
    {

        public String id;
        public String left_url;
        public String left_modify_time;
        public String left_size;
        public String left_resolution;
        public String right_url;
        public String right_modify_time;
        public String right_size;
        public String right_resolution;  
        public double simliar_val;


    }

    class ImageHashObj
    {
        public String name = null;
        public String url = null;
        public String h = null;
        public String w = null; 
        public String size = null;
        public String modify_time = null;
        public String dHash = null;
        public String hisogram = null;
        //public String aHash;



    }
}
