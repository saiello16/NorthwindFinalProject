using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
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
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cartItem = _dataContext.CartItems.Find(id);
            if (cartItem == null)
            {
                return NotFound();
            }

            else if (quantity > 0)
            {
                cartItem.Quantity = quantity;
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
        public IActionResult ApplyDiscount(int discountCode)
        {
            // first, find all cart items for the current user
            var items = _dataContext.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.Customer.Email == User.Identity.Name)
                    .ToList();

            // loop thru list of cart items and determine iof the code entered appluies to any produstcs in the cart
            foreach (var item in items)
            {
                var product = _dataContext.Products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (product != null)
                {
                    var productDiscount = _dataContext.Discounts.FirstOrDefault(d => d.Code == discountCode && d.ProductId == product.ProductId);
                    if (productDiscount != null)
                    {
                        item.DiscountPercent = productDiscount.DiscountPercent;
                        // update cart items table
                        _dataContext.ApplyCartItemDiscount(item);
                    }
                    break;
                }
            }
            // return to cart
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
                    decimal price = item.Product.UnitPrice;
                    if (item.DiscountPercent != null){
                        price -= (decimal)(item.DiscountPercent * item.Product.UnitPrice);
                    }
                    OrderDetail orderDetail = new OrderDetail
                    {
                        OrderId = newOrder.OrderId,
                        ProductId = item.ProductId,
                        UnitPrice = price,
                        Discount = (decimal)item.DiscountPercent,
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
