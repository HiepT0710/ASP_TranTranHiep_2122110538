using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        if (!await db.Users.AnyAsync())
        {
            await SeedFullDemoAsync(db, passwordHasher);
            return;
        }

        // DB đã có user từ trước (seed cũ) → vẫn tạo seller demo nếu chưa có username "seller"
        await EnsureDemoSellerAndRestaurantAsync(db, passwordHasher);
    }

    private static async Task SeedFullDemoAsync(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        var admin = new User
        {
            Username = "admin",
            FullName = "Quản trị viên",
            Email = "admin@food.local",
            Phone = "0900000000",
            Address = "Hà Nội",
            Role = Roles.Admin,
            CreatedAt = DateTime.UtcNow
        };
        admin.Password = passwordHasher.HashPassword(admin, "Admin@123");
        db.Users.Add(admin);

        var customer = new User
        {
            Username = "user",
            FullName = "Khách hàng mẫu",
            Email = "user@food.local",
            Phone = "0911111111",
            Address = "TP.HCM",
            Role = Roles.User,
            CreatedAt = DateTime.UtcNow
        };
        customer.Password = passwordHasher.HashPassword(customer, "User@123");
        db.Users.Add(customer);

        var seller = new User
        {
            Username = "seller",
            FullName = "Chủ quán mẫu",
            Email = "seller@food.local",
            Phone = "0922222222",
            Address = "Đà Nẵng",
            Role = Roles.Seller,
            CreatedAt = DateTime.UtcNow
        };
        seller.Password = passwordHasher.HashPassword(seller, "Seller@123");
        db.Users.Add(seller);

        await db.SaveChangesAsync();

        await SeedDefaultSystemSettingsAsync(db);
        await AddRestaurantMenuForSellerAsync(db, seller.Id);
        await SeedDemoOrdersAndReviewsAsync(db, customer.Id, seller.Id);
    }

    private static async Task EnsureDemoSellerAndRestaurantAsync(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        await SeedDefaultSystemSettingsAsync(db);

        if (await db.Users.AnyAsync(u => u.Username == "seller"))
            return;

        var seller = new User
        {
            Username = "seller",
            FullName = "Chủ quán mẫu",
            Email = "seller@food.local",
            Phone = "0922222222",
            Address = "Đà Nẵng",
            Role = Roles.Seller,
            CreatedAt = DateTime.UtcNow
        };
        seller.Password = passwordHasher.HashPassword(seller, "Seller@123");
        db.Users.Add(seller);
        await db.SaveChangesAsync();

        await AddRestaurantMenuForSellerAsync(db, seller.Id);
    }

    private static async Task SeedDefaultSystemSettingsAsync(AppDbContext db)
    {
        var defaults = new[]
        {
            new SystemSetting { Key = "Shipping:DefaultFee", Value = "15000", Description = "Phí ship mặc định cho mỗi đơn hàng" },
            new SystemSetting { Key = "Shipping:FreeShipThreshold", Value = "150000", Description = "Từ giá trị đơn hàng này trở lên thì miễn phí ship" },
            new SystemSetting { Key = "Order:CancelWindowMinutes", Value = "15", Description = "Số phút cho phép khách hủy đơn sau khi đặt" }
        };

        foreach (var setting in defaults)
        {
            var existing = await db.SystemSettings.FirstOrDefaultAsync(x => x.Key == setting.Key);
            if (existing == null)
            {
                db.SystemSettings.Add(setting);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(existing.Value)) existing.Value = setting.Value;
                if (string.IsNullOrWhiteSpace(existing.Description)) existing.Description = setting.Description;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task AddRestaurantMenuForSellerAsync(AppDbContext db, int sellerUserId)
    {
        if (await db.Restaurants.AnyAsync(r => r.OwnerId == sellerUserId))
            return;

        var restaurant = new Restaurant
        {
            Name = "Quán cơm nhà",
            OwnerId = sellerUserId,
            Address = "123 Lê Lợi, Q1",
            Phone = "0283888999",
            CoverImage = "/images/foods/placeholder.svg",
            GalleryImage1 = "/images/foods/placeholder.svg",
            GalleryImage2 = "/images/foods/placeholder.svg",
            GalleryImage3 = "/images/foods/placeholder.svg",
            IsOnSale = true,
            SalePercent = 15,
            Status = RestaurantStatuses.Approved
        };
        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        var catDrink = new Category { Name = "Đồ uống", Description = "Nước, trà, cà phê", RestaurantId = restaurant.Id };
        var catMain = new Category { Name = "Món chính", Description = "Cơm, mì, phở", RestaurantId = restaurant.Id };
        var catSnack = new Category { Name = "Ăn vặt", Description = "Gỏi cuốn, nem", RestaurantId = restaurant.Id };
        db.Categories.AddRange(catDrink, catMain, catSnack);
        await db.SaveChangesAsync();

        db.Foods.AddRange(
            new Food
            {
                Name = "Trà đào",
                Price = 35000,
                Image = "/images/foods/placeholder.svg",
                IsOnSale = true,
                SalePercent = 10,
                Description = "Trà đào mát lạnh",
                RestaurantId = restaurant.Id,
                CategoryId = catDrink.Id,
                IsAvailable = true,
                StockQuantity = 100
            },
            new Food
            {
                Name = "Cà phê sữa đá",
                Price = 25000,
                Image = "/images/foods/placeholder.svg",
                IsOnSale = false,
                SalePercent = 0,
                Description = "Cà phê phin truyền thống",
                RestaurantId = restaurant.Id,
                CategoryId = catDrink.Id,
                IsAvailable = true,
                StockQuantity = 100
            },
            new Food
            {
                Name = "Cơm tấm sườn",
                Price = 55000,
                Image = "/images/foods/placeholder.svg",
                IsOnSale = true,
                SalePercent = 12,
                Description = "Sườn nướng, bì, chả",
                RestaurantId = restaurant.Id,
                CategoryId = catMain.Id,
                IsAvailable = true,
                StockQuantity = 100
            },
            new Food
            {
                Name = "Phở bò",
                Price = 60000,
                Image = "/images/foods/placeholder.svg",
                IsOnSale = false,
                SalePercent = 0,
                Description = "Nước dùng đậm đà",
                RestaurantId = restaurant.Id,
                CategoryId = catMain.Id,
                IsAvailable = true,
                StockQuantity = 100
            },
            new Food
            {
                Name = "Gỏi cuốn",
                Price = 40000,
                Image = "/images/foods/placeholder.svg",
                IsOnSale = false,
                SalePercent = 0,
                Description = "6 cuốn / phần",
                RestaurantId = restaurant.Id,
                CategoryId = catSnack.Id,
                IsAvailable = true,
                StockQuantity = 100
            });

        await db.SaveChangesAsync();
    }

    /// <summary>Đơn hàng và đánh giá mẫu để demo và báo cáo (chỉ khi chưa có đơn).</summary>
    private static async Task SeedDemoOrdersAndReviewsAsync(AppDbContext db, int customerId, int sellerUserId)
    {
        if (await db.Orders.AnyAsync())
            return;

        var restaurant = await db.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == sellerUserId);
        if (restaurant == null)
            return;

        var foods = await db.Foods.Where(f => f.RestaurantId == restaurant.Id).OrderBy(f => f.Id).Take(3).ToListAsync();
        if (foods.Count == 0)
            return;

        var t = DateTime.UtcNow;
        var f0 = foods[0];
        var f1 = foods[1];
        var f2 = foods.Count > 2 ? foods[2] : foods[0];

        var completed = new Order
        {
            UserId = customerId,
            RestaurantId = restaurant.Id,
            OrderDate = t.AddDays(-3),
            TotalAmount = f0.Price * 2 + f1.Price,
            Status = OrderStatuses.Completed,
            Address = "TP.HCM",
            Phone = "0911111111",
            PaymentMethod = PaymentMethods.COD,
            PaymentStatus = PaymentStatuses.Paid,
            PaymentSource = "Seed",
            PaidAt = t.AddDays(-3).AddHours(2)
        };
        db.Orders.Add(completed);
        await db.SaveChangesAsync();

        db.OrderDetails.AddRange(
            new OrderDetail { OrderId = completed.Id, FoodId = f0.Id, Quantity = 2, Price = f0.Price },
            new OrderDetail { OrderId = completed.Id, FoodId = f1.Id, Quantity = 1, Price = f1.Price });

        var food0 = await db.Foods.FindAsync(f0.Id);
        var food1 = await db.Foods.FindAsync(f1.Id);
        if (food0 != null)
            food0.StockQuantity = Math.Max(0, food0.StockQuantity - 2);
        if (food1 != null)
            food1.StockQuantity = Math.Max(0, food1.StockQuantity - 1);

        db.OrderStatusHistories.AddRange(
            new OrderStatusHistory
            {
                OrderId = completed.Id,
                FromStatus = null,
                ToStatus = OrderStatuses.Pending,
                ActorUserId = customerId,
                ActorRole = Roles.User,
                Note = "Tạo đơn (dữ liệu mẫu)",
                CreatedAt = t.AddDays(-3)
            },
            new OrderStatusHistory
            {
                OrderId = completed.Id,
                FromStatus = OrderStatuses.Pending,
                ToStatus = OrderStatuses.Preparing,
                ActorUserId = sellerUserId,
                ActorRole = Roles.Seller,
                CreatedAt = t.AddDays(-3).AddMinutes(15)
            },
            new OrderStatusHistory
            {
                OrderId = completed.Id,
                FromStatus = OrderStatuses.Preparing,
                ToStatus = OrderStatuses.Delivering,
                ActorUserId = sellerUserId,
                ActorRole = Roles.Seller,
                Note = "Mã vận đơn DEMO-001",
                CreatedAt = t.AddDays(-3).AddHours(1)
            },
            new OrderStatusHistory
            {
                OrderId = completed.Id,
                FromStatus = OrderStatuses.Delivering,
                ToStatus = OrderStatuses.Completed,
                ActorUserId = sellerUserId,
                ActorRole = Roles.Seller,
                CreatedAt = t.AddDays(-3).AddHours(2)
            });

        db.OrderPayments.Add(new OrderPayment
        {
            OrderId = completed.Id,
            Amount = completed.TotalAmount,
            Kind = PaymentKinds.CodCapture,
            Method = PaymentMethods.COD,
            Status = PaymentStatuses.Paid,
            Note = "Thu COD khi hoàn thành (dữ liệu mẫu)",
            CreatedAt = t.AddDays(-3).AddHours(2)
        });

        db.FoodReviews.AddRange(
            new FoodReview
            {
                OrderId = completed.Id,
                FoodId = f0.Id,
                UserId = customerId,
                Rating = 5,
                Comment = "Rất ngon, đóng gói cẩn thận.",
                CreatedAt = t.AddDays(-2)
            },
            new FoodReview
            {
                OrderId = completed.Id,
                FoodId = f1.Id,
                UserId = customerId,
                Rating = 4,
                Comment = "Khá ổn.",
                CreatedAt = t.AddDays(-2)
            });

        var pending = new Order
        {
            UserId = customerId,
            RestaurantId = restaurant.Id,
            OrderDate = t,
            TotalAmount = f2.Price,
            Status = OrderStatuses.Pending,
            Address = "TP.HCM",
            Phone = "0911111111",
            PaymentMethod = PaymentMethods.VNPay,
            PaymentStatus = PaymentStatuses.Pending
        };
        db.Orders.Add(pending);
        await db.SaveChangesAsync();

        db.OrderDetails.Add(new OrderDetail { OrderId = pending.Id, FoodId = f2.Id, Quantity = 1, Price = f2.Price });
        var food2 = await db.Foods.FindAsync(f2.Id);
        if (food2 != null)
            food2.StockQuantity = Math.Max(0, food2.StockQuantity - 1);

        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = pending.Id,
            FromStatus = null,
            ToStatus = OrderStatuses.Pending,
            ActorUserId = customerId,
            ActorRole = Roles.User,
            Note = "Tạo đơn (dữ liệu mẫu — chờ thanh toán VNPay)",
            CreatedAt = t
        });

        await db.SaveChangesAsync();
    }
}
