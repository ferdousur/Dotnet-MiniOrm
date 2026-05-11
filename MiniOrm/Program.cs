using Microsoft.Extensions.DependencyInjection;
using MiniOrm.Data;
using MiniOrm.Models;

// connection string
var connectionString =
    Environment.GetEnvironmentVariable("MINIORM_CONNECTION_STRING")
    ?? "Host=localhost:5433;Database=miniorm_db;Username=postgres;Password=password";


// DI container
var services = new ServiceCollection();

services.AddSingleton(new DbContext(connectionString));

var serviceProvider = services.BuildServiceProvider();

var dbContext = serviceProvider.GetRequiredService<DbContext>();



// PRODUCTS DBSET
var products = new DbSet<Products>(dbContext, "products");

products.Add(new Products { Product_Name = "Pen", Price = 10.5m });
products.Add(new Products { Product_Name = "Book", Price = 50.0m });



// ORDERS DBSET
var orders = new DbSet<Order>(dbContext, "orders");

orders.Add(new Order
{
    ProductId = 1,
    Quantity = 2,
    TotalPrice = 21.0m
});

orders.Add(new Order
{
    ProductId = 2,
    Quantity = 1,
    TotalPrice = 50.0m
});



// GET PRODUCTS
var allProducts = products.GetAll();

foreach (var p in allProducts)
{
    Console.WriteLine($"{p.Id} | {p.Product_Name} | {p.Price}");
}



// GET ORDERS
var allOrders = orders.GetAll();

foreach (var o in allOrders)
{
    Console.WriteLine($"{o.id} | Product:{o.ProductId} | Qty:{o.Quantity} | Total:{o.TotalPrice}");
}



// FIND PRODUCT
var foundProduct = products.Find(1);

if (foundProduct != null)
{
    Console.WriteLine($"Found Product: {foundProduct.Product_Name}");
}



// UPDATE PRODUCT
products.Update(new Products
{
    Id = 1,
    Product_Name = "Pencil",
    Price = 5.0m
});



// DELETE ORDER
orders.Delete(2);