using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        await db.Database.MigrateAsync();

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

        await AddRestaurantMenuForSellerAsync(db, seller.Id);
    }

    private static async Task EnsureDemoSellerAndRestaurantAsync(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
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
                Description = "Trà đào mát lạnh",
                RestaurantId = restaurant.Id,
                CategoryId = catDrink.Id,
                IsAvailable = true
            },
            new Food
            {
                Name = "Cà phê sữa đá",
                Price = 25000,
                Image = "/images/foods/placeholder.svg",
                Description = "Cà phê phin truyền thống",
                RestaurantId = restaurant.Id,
                CategoryId = catDrink.Id,
                IsAvailable = true
            },
            new Food
            {
                Name = "Cơm tấm sườn",
                Price = 55000,
                Image = "/images/foods/placeholder.svg",
                Description = "Sườn nướng, bì, chả",
                RestaurantId = restaurant.Id,
                CategoryId = catMain.Id,
                IsAvailable = true
            },
            new Food
            {
                Name = "Phở bò",
                Price = 60000,
                Image = "/images/foods/placeholder.svg",
                Description = "Nước dùng đậm đà",
                RestaurantId = restaurant.Id,
                CategoryId = catMain.Id,
                IsAvailable = true
            },
            new Food
            {
                Name = "Gỏi cuốn",
                Price = 40000,
                Image = "/images/foods/placeholder.svg",
                Description = "6 cuốn / phần",
                RestaurantId = restaurant.Id,
                CategoryId = catSnack.Id,
                IsAvailable = true
            });

        await db.SaveChangesAsync();
    }
}
