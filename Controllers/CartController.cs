using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Northwind.Controllers
{
    public class CartController(DataContext db) : Controller
    {
        // this controller depends on the DataContext
        private readonly DataContext _dataContext = db;
        public ActionResult Index() => View(_dataContext.CartItems.Include(c => c.Product));

        // public IActionResult Add(int id)
        // {
        //     var product = _dataContext.Products.Find(id);
        //     if (product != null)
        //     {
        //         var cartItem = new CartItem { Product = product, Quantity = 1 };
        //         _dataContext.CartItems.Add(cartItem);
        //         _dataContext.SaveChanges();
        //     }
        //     return RedirectToAction("Index");
        // }

        public IActionResult RemoveFromCart(int id)
        {
            var cartItem = _dataContext.CartItems.Find(id);
            if (cartItem != null)
            {
                _dataContext.CartItems.Remove(cartItem);
                _dataContext.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
