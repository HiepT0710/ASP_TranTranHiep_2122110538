using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        await SeedDefaultSystemSettingsAsync(db);
        await SeedUsersRestaurantsAndFoodsAsync(db, passwordHasher);
    }

    private static async Task SeedDefaultSystemSettingsAsync(AppDbContext db)
    {
        db.SystemSettings.AddRange(
            new SystemSetting { Key = "Shipping:DefaultFee", Value = "15000", Description = "Phi ship mac dinh cho moi don hang" },
            new SystemSetting { Key = "Shipping:FreeShipThreshold", Value = "150000", Description = "Mien phi ship cho don dat nguong nay" },
            new SystemSetting { Key = "Order:CancelWindowMinutes", Value = "15", Description = "So phut cho phep khach huy don" });

        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersRestaurantsAndFoodsAsync(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        var now = DateTime.UtcNow;

        var admin = CreateUser(passwordHasher, "admin", "Admin@123", "Quan tri vien", Roles.Admin, "admin@food.local", "0900000000", "Ha Noi", now);
        var users = new[]
        {
            CreateUser(passwordHasher, "user01", "User@123", "Nguyen Minh Anh", Roles.User, "user01@food.local", "0911000001", "Quan 1, TP.HCM", now),
            CreateUser(passwordHasher, "user02", "User@123", "Tran Hoang Long", Roles.User, "user02@food.local", "0911000002", "Quan 7, TP.HCM", now),
            CreateUser(passwordHasher, "user03", "User@123", "Le Thu Ha", Roles.User, "user03@food.local", "0911000003", "Hai Chau, Da Nang", now),
            CreateUser(passwordHasher, "user04", "User@123", "Pham Thanh Tung", Roles.User, "user04@food.local", "0911000004", "Cau Giay, Ha Noi", now),
            CreateUser(passwordHasher, "user05", "User@123", "Vu Nhat Linh", Roles.User, "user05@food.local", "0911000005", "Ninh Kieu, Can Tho", now)
        };
        var sellers = new List<User>
        {
            CreateUser(passwordHasher, "seller01", "Seller@123", "Quan Pho 24h", Roles.Seller, "seller01@food.local", "0922000001", "Hai Chau, Da Nang", now),
            CreateUser(passwordHasher, "seller02", "Seller@123", "Bep Nha Me Nau", Roles.Seller, "seller02@food.local", "0922000002", "Binh Thanh, TP.HCM", now),
            CreateUser(passwordHasher, "seller03", "Seller@123", "Com Tam Sai Gon", Roles.Seller, "seller03@food.local", "0922000003", "Quan 3, TP.HCM", now),
            CreateUser(passwordHasher, "seller04", "Seller@123", "Lau Nuong Ha Thanh", Roles.Seller, "seller04@food.local", "0922000004", "Cau Giay, Ha Noi", now),
            CreateUser(passwordHasher, "seller05", "Seller@123", "Banh Mi Sai Gon", Roles.Seller, "seller05@food.local", "0922000005", "Quan 10, TP.HCM", now),
            CreateUser(passwordHasher, "seller06", "Seller@123", "Bun Cha Pho Co", Roles.Seller, "seller06@food.local", "0922000006", "Ba Dinh, Ha Noi", now),
            CreateUser(passwordHasher, "seller07", "Seller@123", "Mi Cay Han Quoc", Roles.Seller, "seller07@food.local", "0922000007", "Ninh Kieu, Can Tho", now),
            CreateUser(passwordHasher, "seller08", "Seller@123", "Hai San Bien Xanh", Roles.Seller, "seller08@food.local", "0922000008", "Son Tra, Da Nang", now),
            CreateUser(passwordHasher, "seller09", "Seller@123", "Ga Ran Tokbokki", Roles.Seller, "seller09@food.local", "0922000009", "Thu Duc, TP.HCM", now),
            CreateUser(passwordHasher, "seller10", "Seller@123", "Chay An Nhien", Roles.Seller, "seller10@food.local", "0922000010", "Tay Ho, Ha Noi", now)
        };

        db.Users.Add(admin);
        db.Users.AddRange(users);
        db.Users.AddRange(sellers);
        await db.SaveChangesAsync();

        var restaurantSpecs = new[]
        {
            new RestaurantSeed("Pho Bo 24h Da Nang", "12 Tran Phu, Hai Chau, Da Nang", "02363888881", true, 10, "06:00 - 22:00"),
            new RestaurantSeed("Bep Nha Me Nau", "201 Dien Bien Phu, Binh Thanh, TP.HCM", "02838888882", true, 15, "08:00 - 21:30"),
            new RestaurantSeed("Com Tam Sai Gon Xua", "45 Vo Van Tan, Quan 3, TP.HCM", "02838888883", false, 0, "07:00 - 23:00"),
            new RestaurantSeed("Lau Nuong Ha Thanh", "88 Xuan Thuy, Cau Giay, Ha Noi", "02438888884", true, 12, "10:00 - 23:30"),
            new RestaurantSeed("Banh Mi Sai Gon", "102 Le Hong Phong, Quan 10, TP.HCM", "02838888885", true, 8, "06:30 - 20:30"),
            new RestaurantSeed("Bun Cha Pho Co", "39 Hang Manh, Hoan Kiem, Ha Noi", "02438888886", true, 10, "07:00 - 21:00"),
            new RestaurantSeed("Mi Cay Han Quoc", "12 Nguyen Trai, Ninh Kieu, Can Tho", "02923888887", true, 18, "09:00 - 22:30"),
            new RestaurantSeed("Hai San Bien Xanh", "15 Ho Nghinh, Son Tra, Da Nang", "02363888888", false, 0, "10:00 - 23:00"),
            new RestaurantSeed("Ga Ran Tokbokki", "11 Kha Van Can, Thu Duc, TP.HCM", "02838888889", true, 20, "09:00 - 23:00"),
            new RestaurantSeed("Chay An Nhien", "66 Nhat Chieu, Tay Ho, Ha Noi", "02438888890", true, 10, "07:30 - 21:30")
        };

        for (var i = 0; i < restaurantSpecs.Length; i++)
        {
            await CreateRestaurantWithMenuAsync(db, sellers[i].Id, restaurantSpecs[i], BuildMenuTemplate(restaurantSpecs[i].Name));
        }
    }

    private static User CreateUser(
        IPasswordHasher<User> passwordHasher,
        string username,
        string rawPassword,
        string fullName,
        string role,
        string email,
        string phone,
        string address,
        DateTime createdAt)
    {
        var user = new User
        {
            Username = username,
            FullName = fullName,
            Email = email,
            Phone = phone,
            Address = address,
            Role = role,
            CreatedAt = createdAt
        };
        user.Password = passwordHasher.HashPassword(user, rawPassword);
        return user;
    }

    private static async Task CreateRestaurantWithMenuAsync(
        AppDbContext db,
        int ownerId,
        RestaurantSeed spec,
        List<MenuItemSeed> menuItems)
    {
        var restaurant = new Restaurant
        {
            Name = spec.Name,
            OwnerId = ownerId,
            Address = spec.Address,
            Phone = spec.Phone,
            CoverImage = "/images/restaurants/placeholder.svg",
            GalleryImage1 = "/images/restaurants/placeholder.svg",
            GalleryImage2 = "/images/restaurants/placeholder.svg",
            GalleryImage3 = "/images/restaurants/placeholder.svg",
            IsOnSale = spec.IsOnSale,
            SalePercent = spec.SalePercent,
            IsOpen = true,
            IsAcceptingOrders = true,
            OpeningHours = spec.OpeningHours,
            Status = RestaurantStatuses.Approved,
            StatusUpdatedAt = DateTime.UtcNow
        };

        db.Restaurants.Add(restaurant);
        await db.SaveChangesAsync();

        var categories = new List<Category>
        {
            new() { Name = "Mon chinh", Description = "Mon an chinh cua quan", RestaurantId = restaurant.Id },
            new() { Name = "Mon phu", Description = "Mon phu an kem mon chinh", RestaurantId = restaurant.Id },
            new() { Name = "Nuoc uong", Description = "Do uong va nuoc giai khat", RestaurantId = restaurant.Id },
            new() { Name = "Mon them", Description = "Topping hoac phan an them", RestaurantId = restaurant.Id },
            new() { Name = "Dung cu an uong", Description = "Muong, dua, khan giay, hop dung", RestaurantId = restaurant.Id }
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var categoryIdByName = categories.ToDictionary(c => c.Name, c => c.Id);
        var foods = new List<Food>();
        for (var i = 0; i < menuItems.Count; i++)
        {
            var item = menuItems[i];
            foods.Add(new Food
            {
                Name = item.Name,
                Price = item.Price,
                Image = "/images/foods/placeholder.svg",
                IsOnSale = spec.IsOnSale && i < 4,
                SalePercent = spec.IsOnSale && i < 4 ? Math.Max(5, spec.SalePercent - 2) : 0,
                Description = $"{item.Description} Du lieu da tao san, ban chi can cap nhat anh cho mon an.",
                RestaurantId = restaurant.Id,
                CategoryId = categoryIdByName[item.Category],
                IsAvailable = true,
                IsHidden = false,
                StockQuantity = item.StockQuantity
            });
        }

        db.Foods.AddRange(foods);
        await db.SaveChangesAsync();
    }

    private static List<MenuItemSeed> BuildMenuTemplate(string restaurantName)
    {
        return new List<MenuItemSeed>
        {
            new("Mon chinh", $"{restaurantName} dac biet", 69000m, "Mon signature cua quan, ban chay nhat.", 120),
            new("Mon chinh", "Com suon nuong", 55000m, "Com nong an kem suon nuong dam vi.", 110),
            new("Mon chinh", "Bun bo dac biet", 59000m, "To bun day du topping thit va cha.", 100),
            new("Mon phu", "Khoai tay chien", 32000m, "Khoai chien gion, dung kem sot.", 95),
            new("Mon phu", "Salad rau tron", 35000m, "Salad tuoi, sot me nhe.", 90),
            new("Mon phu", "Soup trong ngay", 28000m, "Soup nong, phuc vu kem mon chinh.", 85),
            new("Nuoc uong", "Tra dao cam sa", 30000m, "Tra trai cay mat lanh.", 130),
            new("Nuoc uong", "Nuoc ep cam tuoi", 32000m, "Nuoc ep nguyen chat khong duong.", 120),
            new("Nuoc uong", "Cafe sua da", 26000m, "Cafe phin dam da truyen thong.", 115),
            new("Mon them", "Trung op la", 12000m, "Them trung op la cho mon chinh.", 150),
            new("Mon them", "Them thit bo", 25000m, "Phan thit bo them giau dinh duong.", 140),
            new("Mon them", "Them pho mai", 15000m, "Pho mai an kem mon chien.", 130),
            new("Dung cu an uong", "Bo dua muong", 3000m, "Bo dung cu an uong ve sinh.", 500),
            new("Dung cu an uong", "Khan giay uot", 2000m, "Khan giay uot dung 1 lan.", 500),
            new("Dung cu an uong", "Hop dung mang ve", 5000m, "Hop giu nhiet cho mon mang di.", 500)
        };
    }

    private sealed record RestaurantSeed(
        string Name,
        string Address,
        string Phone,
        bool IsOnSale,
        int SalePercent,
        string OpeningHours);

    private sealed record MenuItemSeed(
        string Category,
        string Name,
        decimal Price,
        string Description,
        int StockQuantity);
}
