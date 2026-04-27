using Microsoft.EntityFrameworkCore;
using ErpBackend.Entities;
using ErpBackend.Services;

namespace ErpBackend.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<AlertNotification> AlertNotifications { get; set; }
    
    // New entities
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<GroupPermission> GroupPermissions { get; set; }
    
    // Multi-branch & Multi-currency
    public DbSet<Branch> Branches { get; set; }
    public DbSet<BranchStock> BranchStocks { get; set; }
    public DbSet<StockTransfer> StockTransfers { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerBranch> CustomerBranches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Kích hoạt tự động tăng ID chuẩn PostgreSQL
        modelBuilder.UseIdentityByDefaultColumns();

        base.OnModelCreating(modelBuilder);

        // Many-to-many configuration
        modelBuilder.Entity<UserGroup>().HasKey(ug => new { ug.UserId, ug.GroupId });
        modelBuilder.Entity<GroupPermission>().HasKey(gp => new { gp.GroupId, gp.PermissionId });
        modelBuilder.Entity<CustomerBranch>().HasKey(cb => new { cb.CustomerId, cb.BranchId });

        modelBuilder.Entity<CustomerBranch>()
            .HasOne(cb => cb.Customer)
            .WithMany(c => c.CustomerBranches)
            .HasForeignKey(cb => cb.CustomerId);

        modelBuilder.Entity<CustomerBranch>()
            .HasOne(cb => cb.Branch)
            .WithMany()
            .HasForeignKey(cb => cb.BranchId);

        // Row-Level Security (RLS) simulation via Global Query Filters
        // If Admin, see everything. If User, see only their branch.
        modelBuilder.Entity<Invoice>().HasQueryFilter(i => 
            _currentUserService.IsAdmin || 
            !i.BranchId.HasValue || 
            i.BranchId == _currentUserService.BranchId);

        modelBuilder.Entity<BranchStock>().HasKey(bs => new { bs.ProductId, bs.BranchId });
        modelBuilder.Entity<BranchStock>().HasQueryFilter(bs => 
            _currentUserService.IsAdmin || 
            bs.BranchId == _currentUserService.BranchId);

        modelBuilder.Entity<StockTransfer>().HasQueryFilter(st => 
            _currentUserService.IsAdmin || 
            st.FromBranchId == _currentUserService.BranchId || 
            st.ToBranchId == _currentUserService.BranchId);

        modelBuilder.Entity<Customer>().HasQueryFilter(c => 
            _currentUserService.IsAdmin || 
            c.CustomerBranches.Any(cb => cb.BranchId == _currentUserService.BranchId));

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.CustomerCode)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Phone)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.ProductCode)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<StockTransaction>().HasQueryFilter(st => 
            _currentUserService.IsAdmin || 
            st.BranchId == _currentUserService.BranchId);

        // Seed Permissions
        modelBuilder.Entity<Permission>().HasData(
            new Permission { Id = 1, Name = "product.view", Description = "Xem sản phẩm" },
            new Permission { Id = 2, Name = "product.create", Description = "Thêm sản phẩm" },
            new Permission { Id = 3, Name = "invoice.approve", Description = "Duyệt hóa đơn" },
            new Permission { Id = 4, Name = "report.export", Description = "Xuất báo cáo" },
            new Permission { Id = 5, Name = "stock.import", Description = "Nhập kho" },
            new Permission { Id = 6, Name = "invoice.create", Description = "Tạo hóa đơn" },
            new Permission { Id = 7, Name = "invoice.payment", Description = "Thanh toán" }
        );

        // Seed Groups
        modelBuilder.Entity<Group>().HasData(
            new Group { Id = 1, Name = "Kế toán kho Hà Nội", BranchId = 1 },
            new Group { Id = 2, Name = "Sales Hà Nội", BranchId = 1 },
            new Group { Id = 3, Name = "Admin Sài Gòn", BranchId = 2 },
            new Group { Id = 4, Name = "Admin Hà Nội", BranchId = 1 },
            new Group { Id = 5, Name = "Sales Sài Gòn", BranchId = 2 },
            new Group { Id = 6, Name = "Kế toán kho Sài Gòn", BranchId = 2 }
        );

        // Seed GroupPermissions
        modelBuilder.Entity<GroupPermission>().HasData(
            new GroupPermission { GroupId = 1, PermissionId = 1 }, // Kho -> View Product
            new GroupPermission { GroupId = 1, PermissionId = 2 }, // Kho -> Create Product
            new GroupPermission { GroupId = 1, PermissionId = 5 }, // Kho -> Stock Import
            new GroupPermission { GroupId = 2, PermissionId = 1 }, // Sales -> View Product
            new GroupPermission { GroupId = 2, PermissionId = 6 }, // Sales -> Create Invoice
            new GroupPermission { GroupId = 2, PermissionId = 7 }, // Sales -> Payment
            // Admin Sài Gòn has ALL permissions
            new GroupPermission { GroupId = 3, PermissionId = 1 },
            new GroupPermission { GroupId = 3, PermissionId = 2 },
            new GroupPermission { GroupId = 3, PermissionId = 3 },
            new GroupPermission { GroupId = 3, PermissionId = 4 },
            new GroupPermission { GroupId = 3, PermissionId = 5 },
            new GroupPermission { GroupId = 3, PermissionId = 6 },
            new GroupPermission { GroupId = 3, PermissionId = 7 },
            // Admin Hà Nội has ALL permissions
            new GroupPermission { GroupId = 4, PermissionId = 1 },
            new GroupPermission { GroupId = 4, PermissionId = 2 },
            new GroupPermission { GroupId = 4, PermissionId = 3 },
            new GroupPermission { GroupId = 4, PermissionId = 4 },
            new GroupPermission { GroupId = 4, PermissionId = 5 },
            new GroupPermission { GroupId = 4, PermissionId = 6 },
            new GroupPermission { GroupId = 4, PermissionId = 7 },
            // Sales Sài Gòn
            new GroupPermission { GroupId = 5, PermissionId = 1 },
            new GroupPermission { GroupId = 5, PermissionId = 6 },
            new GroupPermission { GroupId = 5, PermissionId = 7 },
            // Kho Sài Gòn
            new GroupPermission { GroupId = 6, PermissionId = 1 },
            new GroupPermission { GroupId = 6, PermissionId = 2 },
            new GroupPermission { GroupId = 6, PermissionId = 5 }
        );

        // Seed Users
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "admin", Password = "123", Role = "Admin" },
            new User { Id = 2, Username = "sales_hn", Password = "123", Role = "Sales", BranchId = 1 },
            new User { Id = 3, Username = "kho_hn", Password = "123", Role = "User", BranchId = 1 },
            new User { Id = 4, Username = "manager_sg", Password = "123", Role = "Manager", BranchId = 2 },
            new User { Id = 5, Username = "manager_hn", Password = "123", Role = "Manager", BranchId = 1 },
            new User { Id = 6, Username = "sales_sg", Password = "123", Role = "Sales", BranchId = 2 },
            new User { Id = 7, Username = "kho_sg", Password = "123", Role = "User", BranchId = 2 }
        );

        // Seed UserGroups
        modelBuilder.Entity<UserGroup>().HasData(
            new UserGroup { UserId = 2, GroupId = 2 }, // sales_hn
            new UserGroup { UserId = 3, GroupId = 1 }, // kho_hn
            new UserGroup { UserId = 4, GroupId = 3 }, // manager_sg
            new UserGroup { UserId = 5, GroupId = 4 }, // manager_hn
            new UserGroup { UserId = 6, GroupId = 5 }, // sales_sg
            new UserGroup { UserId = 7, GroupId = 6 }  // kho_sg
        );

        // Seed Branches
        modelBuilder.Entity<Branch>().HasData(
            new Branch { Id = 1, Name = "Hà Nội", Address = "123 Cầu Giấy, Hà Nội", Phone = "024.111.222" },
            new Branch { Id = 2, Name = "Sài Gòn", Address = "456 Quận 1, TP.HCM", Phone = "028.333.444" }
        );

        // Seed Exchange Rates
        modelBuilder.Entity<ExchangeRate>().HasData(
            new ExchangeRate { Id = 1, CurrencyCode = "USD", Rate = 25450.0m, Date = DateTime.UtcNow.Date },
            new ExchangeRate { Id = 2, CurrencyCode = "EUR", Rate = 27120.0m, Date = DateTime.UtcNow.Date }
        );
    }
}
