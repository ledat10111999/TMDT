using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebSiteBanHang.Models;
using CaptchaMvc.HtmlHelpers;
using CaptchaMvc;
using System.Web.Security;
using WebSiteBanHang;
using Microsoft.Ajax.Utilities;
using System.Threading.Tasks;
using System.Net.Mail;

namespace WebSiteBanHang.Controllers
{
    public class HomeController : Controller
    {
        
        QuanLyBanHangEntities db = new QuanLyBanHangEntities();

        public ActionResult Index()
        {
            
            
            // List điện thoại mới nhất
            var lstDTM = db.SanPhams.Where(n => n.MaLoaiSP == 1 && n.Moi == 1 && n.DaXoa == false);
            ViewBag.ListDTM = lstDTM;
            // List laptop mới nhất 
            var lstLTM = db.SanPhams.Where(n => n.MaLoaiSP == 2 && n.Moi == 1 && n.DaXoa == false);
            ViewBag.ListLTM = lstLTM;
            //List Máy tính bảng mới
            var lstMTBM = db.SanPhams.Where(n => n.MaLoaiSP == 3 && n.Moi == 1 && n.DaXoa == false);
            ViewBag.ListMTBM = lstMTBM;
            return View();
        }



        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }


        public ActionResult MenuPartial()
        {
            var lstSP = db.SanPhams; 
            return PartialView(lstSP);
        }
        [HttpGet]
        public ActionResult DangKy()
        {
            
            ViewBag.CauHoi = new SelectList(LoadCauHoi());
            return View();
        }

        public ActionResult DangKy1()
        {
            return View();
        }
        public ActionResult ConfirmEmail(int Token, string Email)
        {
            var user = db.ThanhViens.FirstOrDefault(c => c.MaThanhVien == Token);
            if (user != null)
            {
                if (user.Email == Email)
                {
                    var lstQuyen = db.LoaiThanhVien_Quyen.Where(n => n.MaLoaiTV == user.MaLoaiTV);
                    string Quyen = "";
                    foreach (var item in lstQuyen)
                    {
                        Quyen += item.MaQuyen + ",";
                    }
                    // Cắt dấu ","
                    Quyen = Quyen.Substring(0, Quyen.Length - 1);
                    PhanQuyen(user.TaiKhoan, Quyen);
                    Session["TaiKhoan"] = user;
                    user.EmailConfirmed = true;
                    db.SaveChanges();
                    //await SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home", new { ConfirmedEmail = user.Email });
                }
                else
                {
                    return RedirectToAction("Confirm", "Home", new { Email = user.Email });
                }
            }
            else
            {
                return RedirectToAction("Confirm", "Home", new { Email = "" });
            }
        }




        public ActionResult Confirm(string Email)
        {
            ViewBag.Email = Email;
            return View();
        }



        [HttpPost]
        public ActionResult DangKy(ThanhVien tv, FormCollection f)
        {
            tv.EmailConfirmed = false;
            ViewBag.CauHoi = new SelectList(LoadCauHoi());
            //Kiểm tra Captcha hợp lệ
            if (this.IsCaptchaValid("Captcha is not valid"))
            {
                if (ModelState.IsValid)
                {
                    var us = db.ThanhViens.FirstOrDefault(c => c.TaiKhoan == tv.TaiKhoan);
                    if (us == null)
                    {
                        tv.MaLoaiTV = 3;
                        ViewBag.ThongBao = "Thêm thành công";
                        db.ThanhViens.Add(tv);
                        db.SaveChanges();
                        
                        MailMessage mail = new MailMessage();
                        mail.To.Add(tv.Email); // Địa chỉ nhận
                        mail.From = new MailAddress(tv.Email); // Địa chửi gửi
                        mail.Subject = "Email confirmation";  // tiêu đề gửi
                        mail.Body = string.Format("Dear {0} <br/> Thank you for your registration, please click on the  below link to complete your registration: <a href =\"{1}\"  title =\"User Email Confirm\">{1}</a>", tv.HoTen, Url.Action("ConfirmEmail", "Home", new { Token = tv.MaThanhVien, Email = tv.Email }, Request.Url.Scheme));                  // Nội dung
                        mail.IsBodyHtml = true;
                        SmtpClient smtp = new SmtpClient();
                        smtp.Host = "smtp.gmail.com"; // host gửi của Gmail
                        smtp.Port = 587;               //port của Gmail
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new System.Net.NetworkCredential
                        ("superkutex0@gmail.com", "anhdatvip0x");//Tài khoản password người gửi
                        smtp.EnableSsl = true;   //kích hoạt giao tiếp an toàn SSL
                        smtp.Send(mail);   //Gửi mail đi
                        return RedirectToAction("Confirm", "Home", new { Email = tv.Email });

                    }
                    ModelState.AddModelError("", "Tài khoản đã tồn tại");
                    return View(tv);
                  
                }
                else
                {
                    ViewBag.ThongBao = "Thêm thất bại";
                }
                
                return View();
            }
            
            ViewBag.ThongBao = "Sai mã Captcha";
            return View();
        }
        public List<string> LoadCauHoi()
        {
            List<string> lstCauHoi = new List<string>();
            lstCauHoi.Add("Con vật mà bạn yêu thích?");
            lstCauHoi.Add("Ca sĩ mà bạn yêu thích?");
            lstCauHoi.Add("Nghề nghiệp của bạn là gì?");
            return lstCauHoi;
        }

        //Xây dựng Action đăng nhập
        [HttpPost]
        public ActionResult DangNhap( FormCollection f)
        {
            ////Kiểm tra tên đăng nhập và mật khẩu
            string sTaiKhoan = f["Email"].ToString();
            string sMatKhau = f["Password"].ToString();

           
           
                ThanhVien tv = db.ThanhViens.SingleOrDefault(n=>n.TaiKhoan==sTaiKhoan && n.MatKhau==sMatKhau);
                if(tv!= null)
                {
                    if(tv.EmailConfirmed == false)
                {
                    return Content("Tài khoản bạn chưa xác thực, vui lòng truy cập gmail " + "<a href=\"https://mail.google.com/\" target=\"_blank\"> " + tv.Email +"</a>"  + " để xác thực tài khoản ! ");
                }
                    var lstQuyen = db.LoaiThanhVien_Quyen.Where(n => n.MaLoaiTV == tv.MaLoaiTV);
                    //Duyệt list quyền
                    string Quyen = "";
                    foreach (var item in lstQuyen)
                    {
                        Quyen += item.MaQuyen + ",";
                    }
                    // Cắt dấu ","
                    Quyen = Quyen.Substring(0, Quyen.Length - 1);
                    PhanQuyen(tv.TaiKhoan, Quyen);
                    Session["TaiKhoan"] = tv;
                return Content("<script>window.location.reload()</script>");
            }
                else
                {
                 
                    return Content("Tài khoản hoặc mật khẩu không đúng!");
                }
               
           

            



        }

        public void PhanQuyen(string TaiKhoan, string Quyen)
        {
            FormsAuthentication.Initialize();
            var ticket = new FormsAuthenticationTicket(1,
                                          TaiKhoan, //user
                                          DateTime.Now, //Thời gian bắt đầu
                                          DateTime.Now.AddHours(3), //Thời gian kết thúc
                                          false, //Ghi nhớ?
                                          Quyen, // "DangKy,QuanLyDonHang,QuanLySanPham"
                                          FormsAuthentication.FormsCookiePath);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket));
            if (ticket.IsPersistent) cookie.Expires = ticket.Expiration;
            Response.Cookies.Add(cookie);
        }

        // Tạo trang ngăn chặn truy cập
        public ActionResult LoiPhanQuyen()
        {
            return View();
        }

        public ActionResult Dangxuat()
        {
            //Gán về null
            Session["TaiKhoan"] = null;
            FormsAuthentication.SignOut();
            return RedirectToAction("Index");
        }
        //[AllowAnonymous]
        //public async Task<ActionResult> ConfirmEmail(string Token, string Email)
        //{
        //    ApplicationUser user = this.UserManager.FindById(Token);
        //    if (user != null)
        //    {
        //        if (user.Email == Email)
        //        {
        //            user.ConfirmedEmail = true;
        //            await UserManager.UpdateAsync(user);
        //            await SignInAsync(user, isPersistent: false);
        //            return RedirectToAction("Index", "Home", new { ConfirmedEmail = user.Email });
        //        }
        //        else
        //        {
        //            return RedirectToAction("Confirm", "Account", new { Email = user.Email });
        //        }
        //    }
        //    else
        //    {
        //        return RedirectToAction("Confirm", "Account", new { Email = "" });
        //    }
        //}

        // POST: /Account/Register
        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Register(RegisterViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var user = new ApplicationUser() { UserName = model.UserName };
        //        user.Email = model.Email;
        //        user.ConfirmedEmail = false;
        //        var result = await UserManager.CreateAsync(user, model.Password);
        //        if (result.Succeeded)
        //        {
        //            System.Net.Mail.MailMessage m = new System.Net.Mail.MailMessage(
        //            new System.Net.Mail.MailAddress("sender@mydomain.com", "Web Registration"),
        //            new System.Net.Mail.MailAddress(user.Email));
        //            m.Subject = "Email confirmation";
        //            m.Body = string.Format("Dear {0} " +
        //           " < BR /> Thank you for your registration, please click on the below link to complete your registration: < a href =\"{1}\ title =\"User Email Confirm\">{1}</a>",   user.UserName, Url.Action("ConfirmEmail", "Account",
        //               new { Token = user.Id, Email = user.Email }, Request.Url.Scheme)) ;
        //            m.IsBodyHtml = true;
        //            System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient("smtp.mydomain.com");
        //            smtp.Credentials = new System.Net.NetworkCredential("sender@mydomain.com", "password");
        //            smtp.ServerCertificateValidationCallback = () => true; //Solution for client certificate error
        //            smtp.EnableSsl = true;
        //            smtp.Send(m);
        //            return RedirectToAction("Confirm", "Account", new { Email = user.Email });
        //        }
        //        else
        //        {
        //            AddErrors(result);
        //        }
        //    }
        //    // If we got this far, something failed, redisplay form
        //    return View(model);
        //}
 
    }
}