using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Northwind.Controllers
{
    public class CartController : Controller
    {
        private readonly DataContext _dataContext;
        public CartController(DataContext db)
        {
            _dataContext = db;
        }

        [Authorize(Roles = "northwind-customer")]
        public IActionResult Index()
        {
            var cartItem = _dataContext.CartItems
                .Include(c => c.Product)
                .Where(c => c.Customer.Email == User.Identity.Name)
                .ToList();
            return View(cartItem);
        }
        
        public IActionResult Remove(int id)
        {
            var itemToRemove = _dataContext.CartItems.FirstOrDefault(c => c.CartItemId == id);
            if (itemToRemove != null)
            {
                _dataContext.CartItems.Remove(itemToRemove);
                _dataContext.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "northwind-customer")]
        public IActionResult UpdateQuantity(int id, int qauntity)
        {
            var cartItem = _dataContext.CartItems.Find(id);
            if (cartItem == null)
            {
                return NotFound();
            }

            if (qauntity > 0)
            {
                cartItem.Quantity = qauntity;
            }
            else
            {
                _dataContext.CartItems.Remove(cartItem);
            }
            _dataContext.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "northwind-customer")]
        public IActionResult Checkout()
        {
            try
            {
                var items = _dataContext.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.Customer.Email == User.Identity.Name)
                    .ToList();
                
                if (!items.Any())
                {
                    return NotFound("No items in the cart");
                }

                var customer = _dataContext.Customers.FirstOrDefault(c => c.Email == User.Identity.Name);
                if (customer == null)
                {
                    return NotFound("Customer not found");
                }

                Order newOrder = new Order
                {
                    CustomerId = customer.CustomerId,
                    OrderDate = DateTime.Now,
                    RequiredDate = DateTime.Now.AddDays(7),
                };

                _dataContext.Orders.Add(newOrder);
                _dataContext.SaveChanges();

                foreach (var item in items)
                {
                    OrderDetail orderDetail = new OrderDetail
                    {
                        OrderId = newOrder.OrderId,
                        ProductId = item.ProductId,
                        UnitPrice = item.Product.UnitPrice,
                        Quantity = item.Quantity
                    };
                    _dataContext.OrderDetails.Add(orderDetail);
                }

                _dataContext.SaveChanges();
                _dataContext.CartItems.RemoveRange(items);
                _dataContext.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }   
    }
}
