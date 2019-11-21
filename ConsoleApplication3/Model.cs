using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication3
{
    public class Model
    {
        public string Type { get; set; }
        public string Code { get; set; }

        public string Goods { get; set; }

        public string TypeGoods { get; set; }

        public string Value01 { get; set; }

        public string Value02 { get; set; }

        public string Value03 { get; set; }

        public string Value04 { get; set; }

        public Model(string type, string code, string goods, string typeGoods, string value01, string value02, string value03, string value04)
        {
            this.Type = type;
            this.Code = code;
            this.Goods = goods;
            this.TypeGoods = typeGoods;
            this.Value01 = value01;
            this.Value02 = value02;
            this.Value03 = value03;
            this.Value04 = value04;
        }

    }
}
