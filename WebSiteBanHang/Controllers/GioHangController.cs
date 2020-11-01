using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebSiteBanHang.Models;
using PayPal.Api;
using WebSiteBanHang.helper;
namespace WebSiteBanHang.Controllers
{
    public class GioHangController : Controller
    {
        private Payment payment;
        QuanLyBanHangEntities db = new QuanLyBanHangEntities();
        // Lây danh sách giỏ hàng

       
        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution() { payer_id = payerId };
            this.payment = new Payment() { id = paymentId };
            return this.payment.Execute(apiContext, paymentExecution);
        }

        public List<itemGioHang> LayGioHang()
        {
            List<itemGioHang> lstGioHang = Session["GioHang"] as List<itemGioHang>;
            if(lstGioHang == null)
            {
                //Nếu session bằng  null thì khởi tạo gio hàng
                lstGioHang = new List<itemGioHang>();
                // Gán lại giỏ hàng cho session
                Session["GioHang"] = lstGioHang;
            }
            // nếu giỏ hàng khác null ( đã có sản phẩm trong giỏ ) thì trả về  list
            return lstGioHang;
        }
        //  Thêm sản phẩm bằng cách thông thường ( Load lại trang bằng URL)
        public ActionResult ThemGioHang(int MaSP,string strURL)
        {
            // Kiểm tra trong csdl 
            SanPham sp = db.SanPhams.SingleOrDefault(n => n.MaSP == MaSP);
            if (sp == null)
            {
                //Trả về trang đường dẫn không hợp lệ
                Response.StatusCode = 404;
                return null;
            }
            // nếu != null thì Lấy giỏ hàng
            List<itemGioHang> lstGioHang = LayGioHang();
            // Xét trường hợp sản phẩm được chọn đã có trong giỏ hàng -> tăng số lượng và cập nhật thành tiền
            itemGioHang spCheck = lstGioHang.SingleOrDefault(n => n.MaSP == MaSP);
            if(spCheck != null)
            {
                // Kiểm tra số lượng tồn kho
                if(spCheck.SoLuong > sp.SoLuongTon)
                {
                    // trả về thông báo hết hàng
                    return View("ThongBao");
                }
                spCheck.SoLuong++;
                spCheck.ThanhTien = spCheck.SoLuong * spCheck.DonGia;
                // trả về trang URL hiện tại
                return Redirect(strURL);
            }
            // nếu sp không có trong giỏ hàng -> tạo sp theo MaSP mới rồi add vào giỏ hàng hiện tại
            itemGioHang itemGH = new itemGioHang(MaSP);
            // Kiểm tra số lượng tồn kho
            if (itemGH.SoLuong > sp.SoLuongTon)
            {
                // trả về thông báo hết hàng
                return View("ThongBao");
            }
            lstGioHang.Add(itemGH);
            return Redirect(strURL);

        }
        // Tính tổng số lượng
        public double TinhTongSoLuong()
        {
            // Lấy giỏ hàng từ Session 
            List<itemGioHang> lstGioHang = Session["GioHang"] as List<itemGioHang>;
            if(lstGioHang == null)
            {
                return 0;
            }
            return lstGioHang.Sum(n => n.SoLuong);
        }

        // Tính tổng tiền
        public decimal TinhTongTien()
        {
            List<itemGioHang> lstGioHang = Session["GioHang"] as List<itemGioHang>;
            if (lstGioHang == null)
            {
                return 0;
            }
            return lstGioHang.Sum(n => n.ThanhTien);
        }

        public ActionResult GioHangPartial()
        {
           
            //Kiểm tra nếu tổng số lượng = 0 thì trả về View 0
            if (TinhTongSoLuong() == 0)
            {
                ViewBag.TongSoLuong = 0;
                ViewBag.TongTien = 0;
                return PartialView();
            }
            // Gán trả về ViewBag
            ViewBag.TongSoLuong = TinhTongSoLuong();
            ViewBag.TongTien = TinhTongTien();

            return PartialView();
        }


        // GET: GioHang
        public ActionResult XemGioHang()
        {
            List<itemGioHang> lstGioHang = LayGioHang();
            return View(lstGioHang);
        }


        //Chỉnh sửa giỏ hàng
        public ActionResult SuaGioHang(int maSP)
        {
            // Kiểm tra giỏ hàng tồn tại hay chưa
            if (Session["GioHang"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            //Kiểm tra sp có trong csdl 
            SanPham sp = db.SanPhams.SingleOrDefault(n => n.MaSP == maSP);
            if (sp == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            // Lấy list giỏ hàng từ Session
            List<itemGioHang> lstGioHang = LayGioHang();
            // Kiểm tra sp sửa có tồn tại trong list hay không
            itemGioHang spCheck = lstGioHang.SingleOrDefault(n => n.MaSP == maSP);
            if(spCheck == null)
            {
                return RedirectToAction("Index", "Home");
            }
            // Gán lstGioHang qua ViewBag để tạo giao diện chỉnh sửa
            ViewBag.GioHang = lstGioHang;


            //Nếu tồn tại rồi
            return View(spCheck);
        }

        // Chức năng xử lý cập nhật giỏ hàng CapNhatGioHang
        // nhận 1 biến itemGioHang
        [HttpPost]
        public ActionResult CapNhatGioHang(itemGioHang itemGH)
        {
            // Kiểm tra tồn kho
            SanPham spCheck = db.SanPhams.Single(n => n.MaSP == itemGH.MaSP);
            if(spCheck.SoLuongTon< itemGH.SoLuong)
            {
                return View("ThongBao");
            }
            // Cập nhật số lượng trong session giỏ hàng
            List<itemGioHang> lstGioHang = LayGioHang();
            // tìm itemGH trong lstGioHang
            itemGioHang itemGHUpdate = lstGioHang.Find(n => n.MaSP == itemGH.MaSP);
            itemGHUpdate.SoLuong = itemGH.SoLuong;
            // Cập nhật số lượng --> cập nhật thành tiền
            itemGHUpdate.ThanhTien = itemGHUpdate.DonGia * itemGHUpdate.SoLuong;


            //return RedirectToAction("SuaGioHang",new { @maSP = itemGHUpdate.MaSP});
            return RedirectToAction("XemGioHang");
        }

        public ActionResult XoaGioHang(int maSP)
        {
            // Kiểm tra giỏ hàng tồn tại hay chưa
            if (Session["GioHang"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            //Kiểm tra sp có trong csdl 
            SanPham sp = db.SanPhams.SingleOrDefault(n => n.MaSP == maSP);
            if (sp == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            // Lấy list giỏ hàng từ Session
            List<itemGioHang> lstGioHang = LayGioHang();
            // Kiểm tra sp sửa có tồn tại trong list hay không
            itemGioHang spCheck = lstGioHang.SingleOrDefault(n => n.MaSP == maSP);
            if (spCheck == null)
            {
                return RedirectToAction("Index", "Home");
            }
            //Xóa item trong giỏ hàng
            lstGioHang.Remove(spCheck);

            return RedirectToAction("XemGioHang");

        }

        //Xây dựng chức năng đặt hàng
        public ActionResult DatHang(KhachHang kh)
        {
            // Kiểm tra giỏ hàng tồn tại hay chưa
            if (Session["GioHang"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            KhachHang khang = new KhachHang();
            if(Session["TaiKhoan"] == null)
            {
                //Thêm kh vào bảng KhachHang ...khi chưa đăng nhập
                khang = kh;
                db.KhachHangs.Add(khang);
                db.SaveChanges();
            }
            else
            {
                // Thêm kh bằng session Taikhoan
                ThanhVien tv = Session["TaiKhoan"] as ThanhVien;
                khang.TenKH = tv.HoTen;
                khang.DiaChi = tv.DiaChi;
                khang.Email = tv.Email;
                khang.SoDienThoai = tv.SoDienThoai;
                khang.MaThanhVien = tv.MaThanhVien;
                db.KhachHangs.Add(khang);
                db.SaveChanges();
            }
            //Thêm đơn hàng
            DonDatHang ddh = new DonDatHang();
            ddh.MaKH = khang.MaKH;
            ddh.NgayDat = DateTime.Now;
            ddh.TinhTrangGiaoHang = false;
            ddh.DaThanhToan = false;
            ddh.UuDai = 0;
            ddh.DaHuy = false;
            ddh.DaXoa = false;
            db.DonDatHangs.Add(ddh);
            db.SaveChanges();
            // Thêm chi tiết đơn hàng
            List<itemGioHang> lstGioHang = LayGioHang();
            foreach(var item in lstGioHang)
            {
                ChiTietDonDatHang ctdh = new ChiTietDonDatHang();
                ctdh.MaDDH = ddh.MaDDH;
                ctdh.TenSP = item.TenSP;
                ctdh.MaSP = item.MaSP;
                ctdh.SoLuong = item.SoLuong;
                ctdh.DonGia = item.DonGia;
                db.ChiTietDonDatHangs.Add(ctdh);
            }
            db.SaveChanges();
            Session["GioHang"] = null;
            return RedirectToAction("XemGioHang");

        }


        // Thêm giỏ hàng Ajax
        public ActionResult ThemGioHangAjax(int MaSP, string strURL)
        {
            // Kiểm tra trong csdl 
            SanPham sp = db.SanPhams.SingleOrDefault(n => n.MaSP == MaSP);
            if (sp == null)
            {
                //Trả về trang đường dẫn không hợp lệ
                Response.StatusCode = 404;
                return null;
            }
            // nếu != null thì Lấy giỏ hàng
            List<itemGioHang> lstGioHang = LayGioHang();
            // Xét trường hợp sản phẩm được chọn đã có trong giỏ hàng -> tăng số lượng và cập nhật thành tiền
            itemGioHang spCheck = lstGioHang.SingleOrDefault(n => n.MaSP == MaSP);
            if (spCheck != null)
            {
                // Kiểm tra số lượng tồn kho
                if (spCheck.SoLuong > sp.SoLuongTon)
                {
                    // trả về thông báo hết hàng
                    return Content("<script>alert(\"Sản phẩm đã hết hàng\")</script>");
                }
                spCheck.SoLuong++;
                spCheck.ThanhTien = spCheck.SoLuong * spCheck.DonGia;
                // trả về trang URL hiện tại
                //return Redirect(strURL);
                // Trả về PartialView GioHangPartial --> cập nhật lại ViewBag
                ViewBag.TongTien = TinhTongTien();
                ViewBag.TongSoLuong = TinhTongSoLuong();
                return PartialView("GioHangPartial");
            } 
            // nếu sp không có trong giỏ hàng -> tạo sp theo MaSP mới rồi add vào giỏ hàng hiện tại
            itemGioHang itemGH = new itemGioHang(MaSP);
            // Kiểm tra số lượng tồn kho
            if (itemGH.SoLuong > sp.SoLuongTon)
            {
                // trả về thông báo hết hàng
                return Content("<script>alert(\"Sản phẩm đã hết hàng\")</script>");
            }
            lstGioHang.Add(itemGH);
            ViewBag.TongTien = TinhTongTien();
            ViewBag.TongSoLuong = TinhTongSoLuong();
            return PartialView("GioHangPartial");

        }
        public ActionResult PaymentWithPaypal(WebSiteBanHang.ViewModel.DatHang kh)
        {


          

            //getting the apiContext as earlier
            APIContext apiContext = Configuration.GetAPIContext();
            try
            {
                string payerId = Request.Params["PayerID"];
                if (string.IsNullOrEmpty(payerId))
                {

                    if (Session["GioHang"] == null)
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    KhachHang khang = new KhachHang();
                    var khachhang = new KhachHang()
                    {
                        DiaChi = kh.DiaChi,
                        Email = kh.Email,
                        SoDienThoai = kh.SoDienThoai,
                        TenKH = kh.TenKH,
                        DonDatHangs = kh.DonDatHangs,
                        MaThanhVien = kh.MaThanhVien
                    };
                    if (Session["TaiKhoan"] == null)
                    {
                        //Thêm kh vào bảng KhachHang ...khi chưa đăng nhập
                        khang = khachhang;
                        db.KhachHangs.Add(khang);
                        db.SaveChanges();
                    }
                    else
                    {
                        // Thêm kh bằng session Taikhoan
                        ThanhVien tv = Session["TaiKhoan"] as ThanhVien;
                        khang.TenKH = tv.HoTen;
                        khang.DiaChi = tv.DiaChi;
                        khang.Email = tv.Email;
                        khang.SoDienThoai = tv.SoDienThoai;
                        khang.MaThanhVien = tv.MaThanhVien;
                        db.KhachHangs.Add(khang);
                        db.SaveChanges();
                    }
                    //Thêm đơn hàng
                    DonDatHang ddh = new DonDatHang();
                    ddh.MaKH = khang.MaKH;
                    ddh.NgayDat = DateTime.Now;
                    ddh.TinhTrangGiaoHang = false;
                    ddh.DaThanhToan = false;
                    ddh.UuDai = 0;
                    ddh.DaHuy = false;
                    ddh.DaXoa = false;
                    db.DonDatHangs.Add(ddh);
                    db.SaveChanges();
                    // Thêm chi tiết đơn hàng
                    List<itemGioHang> lstGioHang = LayGioHang();
                    decimal price = 0.0m;
                    string tenSp = "";
                    foreach (var item in lstGioHang)
                    {
                        ChiTietDonDatHang ctdh = new ChiTietDonDatHang();
                        ctdh.MaDDH = ddh.MaDDH;
                        ctdh.TenSP = item.TenSP;
                        ctdh.MaSP = item.MaSP;
                        ctdh.SoLuong = item.SoLuong;
                        ctdh.DonGia = item.DonGia;
                        price += item.SoLuong * item.DonGia;
                        tenSp += item.TenSP +" ";
                        db.ChiTietDonDatHangs.Add(ctdh);
                    }
                   
                    Session["GioHang"] = null;
                    
                    //this section will be executed first because PayerID doesn't exist
                    //it is returned by the create function call of the payment class
                    // Creating a payment
                    // baseURL is the url on which paypal sendsback the data.
                    // So we have provided URL of this controller only
                    string baseURI = Request.Url.Scheme + "://" + Request.Url.Authority + "/GioHang/PaymentWithPayPal?";
                    //guid we are generating for storing the paymentID received in session
                    //after calling the create function and it is used in the payment execution
                    var guid = Convert.ToString((new Random()).Next(100000));
                    //CreatePayment function gives us the payment approval url
                    //on which payer is redirected for paypal account payment
                    var createdPayment = this.CreatePayment(apiContext, baseURI + "guid=" + guid, price,tenSp );
                    //get links returned from paypal in response to Create function call
                    var links = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = null;
                    while (links.MoveNext())
                    {
                        Links lnk = links.Current;
                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            //saving the payapalredirect URL to which user will be redirected for payment
                            paypalRedirectUrl = lnk.href;
                        }
                    }
                    // saving the paymentID in the key guid
                    Session.Add(guid, createdPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    // This section is executed when we have received all the payments parameters
                    // from the previous call to the function Create
                    // Executing a payment
                    var guid = Request.Params["guid"];
                    var executedPayment = ExecutePayment(apiContext, payerId, Session[guid] as string);
                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View("FailureView");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error" + ex.Message);
                return View("FailureView");
            }
            //db.SaveChanges();
            return RedirectToAction("Index", "Home");
            
        }
        private Payment CreatePayment(APIContext apiContext, string redirectUrl, decimal? price, string tenSp)
        {
            var itemList = new ItemList() { items = new List<Item>() };
            //Các giá trị bao gồm danh sách sản phẩm, thông tin đơn hàng
            //Sẽ được thay đổi bằng hành vi thao tác mua hàng trên website

            var items = new Item()
            {
                //Thông tin đơn hàng
                name = tenSp,
                currency = "USD",
                price = String.Format("{0:0.00}", price/23000) ,
                quantity = "1",
                sku = "sku"

            };

            itemList.items.Add(items);
          

            //Hình thức thanh toán qua paypal
            var payer = new Payer() { payment_method = "paypal" };
            // Configure Redirect Urls here with RedirectUrls object
            var redirUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl,
                return_url = redirectUrl
            };
            //các thông tin trong đơn hàng

            var subtotal = float.Parse(items.price) * float.Parse(items.quantity);

            var details = new Details()
            {
                tax = "1",
                shipping = "1",
                subtotal = subtotal.ToString()
            };
            //Đơn vị tiền tệ và tổng đơn hàng cần thanh toán
            var total = subtotal + float.Parse(details.tax) + float.Parse(details.shipping);
            var amount = new Amount()
            {
                currency = "USD",
                total = total.ToString(), // Total must be equal to sum of shipping, tax and subtotal.
                details = details
            };
            var transactionList = new List<Transaction>();
            //Tất cả thông tin thanh toán cần đưa vào transaction
            transactionList.Add(new Transaction()
            {
                description = "Transaction description.",
                invoice_number = Guid.NewGuid().ToString(),
                amount = amount,
                item_list = itemList

            }); ;
            this.payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrls
            };
            // Create a payment using a APIContext
            return this.payment.Create(apiContext);
        }
    }

}