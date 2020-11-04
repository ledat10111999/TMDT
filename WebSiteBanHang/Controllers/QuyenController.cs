using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebSiteBanHang.Models;
namespace WebSiteBanHang.Controllers
{
    public class QuyenController : Controller
    {
        QuanLyBanHangEntities db = new QuanLyBanHangEntities();
        // GET: Quyen
        public ActionResult Index()
        {
            ThanhVien tv = (ThanhVien)Session["TaiKhoan"];
            if(tv != null)
            {
                if (tv.MaLoaiTV == 1)
                {
                    return View(db.Quyens.OrderBy(n => n.TenQuyen));
                }
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult ThemQuyen()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ThemQuyen(Quyen quyen)
        {
            if (ModelState.IsValid)
            {
                db.Quyens.Add(quyen);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

    }
}