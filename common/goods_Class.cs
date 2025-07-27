using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public class goodslist
    {
        public bool selects { get; set; }
        public int id { get; set; }
        public string title { get; set; }
        public string promotion_id { get; set; }
        public string product_id { get; set; }
        //public string price_text { get; set; }
        public decimal price { get; set; }
        public decimal cos_fee { get; set; }
        public int cos_ratio { get; set; }
        public decimal subsidy_amount { get; set; }
        public string subsidy_ratio_set { get; set; }
        public string coupon_meta_id { get; set; }
        public string remark { get; set; }
        public string cos_ratio_update { get; set; }
        public string is_support_coupon { get; set; }
        public int order_count { get; set; }
        public int coupon_count { get; set; }
        public string item_type { get; set; }
        public int is_lock { get; set; }
        public string recent_order_time { get; set; }
        public string new_cover { get; set; }
        public string goods_source { get; set; }
        public decimal profit_fee { get; set; }
        public string is_support_red_packet { get; set; }
    }
}
