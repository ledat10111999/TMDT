using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using WebSiteBanHang.Models;

namespace WebSiteBanHang.ViewModel
{
    public class DatHang : KhachHang
    {
        [DisplayName("Thanh Toán trực tiếp")]
        public bool ThanhToanTrucTiep { get; set; }
        public bool ThanhToanPaypal { get; set; }
    }
}