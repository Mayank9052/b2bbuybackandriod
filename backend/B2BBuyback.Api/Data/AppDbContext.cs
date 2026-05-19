using System;
using System.Collections.Generic;
using B2BBuyback.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace B2BBuyback.Api.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AreaScootyStock> AreaScootyStocks { get; set; }

    public virtual DbSet<B2bcustomer> B2bcustomers { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<CityArea> CityAreas { get; set; }

    public virtual DbSet<ComparisonConfig> ComparisonConfigs { get; set; }

    public virtual DbSet<CustomerRequest> CustomerRequests { get; set; }

    public virtual DbSet<EmiEnquiry> EmiEnquiries { get; set; }

    public virtual DbSet<ExchangeAdminAction> ExchangeAdminActions { get; set; }

    public virtual DbSet<ExchangeBatterySlab> ExchangeBatterySlabs { get; set; }

    public virtual DbSet<ExchangeCase> ExchangeCases { get; set; }

    public virtual DbSet<ExchangeCaseImage> ExchangeCaseImages { get; set; }

    public virtual DbSet<ExchangeConditionSlab> ExchangeConditionSlabs { get; set; }

    public virtual DbSet<ExchangeInspectionScore> ExchangeInspectionScores { get; set; }

    public virtual DbSet<ExchangeKmSlab> ExchangeKmSlabs { get; set; }

    public virtual DbSet<ExchangeModelBasePrice> ExchangeModelBasePrices { get; set; }

    public virtual DbSet<ExchangeNotificationLog> ExchangeNotificationLogs { get; set; }

    public virtual DbSet<ExchangePricingConfig> ExchangePricingConfigs { get; set; }

    public virtual DbSet<PriceMaster> PriceMasters { get; set; }

    public virtual DbSet<RoadPrice> RoadPrices { get; set; }

    public virtual DbSet<SalesOrder> SalesOrders { get; set; }

    public virtual DbSet<ScootyInventory> ScootyInventories { get; set; }

    public virtual DbSet<ScootySpec> ScootySpecs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserLike> UserLikes { get; set; }

    public virtual DbSet<VehicleBrochure> VehicleBrochures { get; set; }

    public virtual DbSet<VehicleColour> VehicleColours { get; set; }

    public virtual DbSet<VehicleModel> VehicleModels { get; set; }

    public virtual DbSet<VehicleReview> VehicleReviews { get; set; }

    public virtual DbSet<VehicleVariant> VehicleVariants { get; set; }

    public virtual DbSet<VwAreaStockSummary> VwAreaStockSummaries { get; set; }

    public virtual DbSet<VwExchangeDashboardStat> VwExchangeDashboardStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AreaScootyStock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AreaScoo__3214EC071BF5D263");

            entity.ToTable("AreaScootyStock");

            entity.HasIndex(e => e.StockAvailable, "IX_AreaStock_Available");

            entity.HasIndex(e => e.CityAreaId, "IX_AreaStock_CityAreaId");

            entity.HasIndex(e => e.ScootyId, "IX_AreaStock_ScootyID");

            entity.HasIndex(e => new { e.ScootyId, e.CityAreaId }, "UQ_AreaStock_Scooty_Area").IsUnique();

            entity.Property(e => e.ScootyId).HasColumnName("ScootyID");
            entity.Property(e => e.StockAvailable).HasComputedColumnSql("(case when [StockQuantity]>(0) then CONVERT([bit],(1)) else CONVERT([bit],(0)) end)", true);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.CityArea).WithMany(p => p.AreaScootyStocks)
                .HasForeignKey(d => d.CityAreaId)
                .HasConstraintName("FK_AreaStock_CityArea");

            entity.HasOne(d => d.Scooty).WithMany(p => p.AreaScootyStocks)
                .HasForeignKey(d => d.ScootyId)
                .HasConstraintName("FK_AreaStock_Scooty");
        });

        modelBuilder.Entity<B2bcustomer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__B2BCusto__A4AE64B836156658");

            entity.ToTable("B2BCustomers");

            entity.HasIndex(e => e.Email, "UQ_B2BCustomers_Email").IsUnique();

            entity.HasIndex(e => e.Gstnumber, "UQ_B2BCustomers_GST").IsUnique();

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CompanyName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Gstnumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("GSTNumber");
            entity.Property(e => e.LogoPath)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<City>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cities__3214EC073B69A4CB");

            entity.HasIndex(e => e.IsPopular, "IX_Cities_IsPopular");

            entity.HasIndex(e => e.CityName, "UQ_Cities_Name").IsUnique();

            entity.Property(e => e.CityName).HasMaxLength(100);
            entity.Property(e => e.PincodePrefix).HasMaxLength(3);
            entity.Property(e => e.StateName).HasMaxLength(100);
        });

        modelBuilder.Entity<CityArea>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CityArea__3214EC071D347759");

            entity.HasIndex(e => e.CityId, "IX_CityAreas_CityId");

            entity.HasIndex(e => e.Pincode, "IX_CityAreas_Pincode");

            entity.Property(e => e.AreaName).HasMaxLength(200);
            entity.Property(e => e.Pincode).HasMaxLength(10);

            entity.HasOne(d => d.City).WithMany(p => p.CityAreas)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_CityAreas_City");
        });

        modelBuilder.Entity<ComparisonConfig>(entity =>
        {
            entity.HasIndex(e => new { e.Scooty1Id, e.Scooty2Id }, "UQ_ComparisonConfigs").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Scooty1Id).HasColumnName("Scooty1ID");
            entity.Property(e => e.Scooty2Id).HasColumnName("Scooty2ID");

            entity.HasOne(d => d.Scooty1).WithMany(p => p.ComparisonConfigScooty1s)
                .HasForeignKey(d => d.Scooty1Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComparisonConfigs_Scooty1");

            entity.HasOne(d => d.Scooty2).WithMany(p => p.ComparisonConfigScooty2s)
                .HasForeignKey(d => d.Scooty2Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComparisonConfigs_Scooty2");

            entity.HasOne(d => d.Scooty3).WithMany(p => p.ComparisonConfigScooty3s)
                .HasForeignKey(d => d.Scooty3Id)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ComparisonConfigs_Scooty3");
        });

        modelBuilder.Entity<CustomerRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC07B28A4B56");

            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CustomerName).HasMaxLength(150);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.MobileNumber).HasMaxLength(20);
            entity.Property(e => e.PreferredContact).HasMaxLength(50);
            entity.Property(e => e.PreferredModel).HasMaxLength(100);
            entity.Property(e => e.RequestType).HasMaxLength(50);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("New");
        });

        modelBuilder.Entity<EmiEnquiry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EmiEnqui__3214EC071B2B0F16");

            entity.HasIndex(e => e.ScootyId, "IX_EmiEnquiries_ScootyId");

            entity.HasIndex(e => e.UserId, "IX_EmiEnquiries_UserId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.MobileNumber).HasMaxLength(20);
            entity.Property(e => e.PinCode).HasMaxLength(10);
            entity.Property(e => e.ScootyId).HasColumnName("ScootyID");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UserId).HasMaxLength(100);

            entity.HasOne(d => d.Scooty).WithMany(p => p.EmiEnquiries)
                .HasForeignKey(d => d.ScootyId)
                .HasConstraintName("FK_EmiEnquiries_Scooty");
        });

        modelBuilder.Entity<ExchangeAdminAction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exchange__3214EC078B05E19E");

            entity.Property(e => e.Action).HasMaxLength(30);
            entity.Property(e => e.ActionAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.AdminUser).HasMaxLength(100);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.PriceSet).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Case).WithMany(p => p.ExchangeAdminActions)
                .HasForeignKey(d => d.CaseId)
                .HasConstraintName("FK__ExchangeA__CaseI__6166761E");
        });

        modelBuilder.Entity<ExchangeBatterySlab>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exchange__3214EC07C4C9500D");

            entity.Property(e => e.Adjustment).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Label).HasMaxLength(100);
            entity.Property(e => e.ScoreFrom).HasColumnType("decimal(4, 1)");
            entity.Property(e => e.ScoreTo).HasColumnType("decimal(4, 1)");
        });

        modelBuilder.Entity<ExchangeCase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exchange__3214EC079DADD7E4");

            entity.HasIndex(e => e.DealerId, "IX_ExchangeCases_DealerId");

            entity.HasIndex(e => e.Status, "IX_ExchangeCases_Status");

            entity.HasIndex(e => new { e.Status, e.SubmittedAt }, "IX_ExchangeCases_Status_Submitted").IsDescending(false, true);

            entity.HasIndex(e => e.CaseNumber, "UQ__Exchange__103BB8D86D78C9D1").IsUnique();

            entity.Property(e => e.AdminNote).HasMaxLength(500);
            entity.Property(e => e.ApprovedPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CaseNumber).HasMaxLength(20);
            entity.Property(e => e.City).HasMaxLength(80);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CustomerName).HasMaxLength(100);
            entity.Property(e => e.DealerId).HasMaxLength(100);
            entity.Property(e => e.Grade).HasMaxLength(20);
            entity.Property(e => e.MaxPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MinPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MobileNumber).HasMaxLength(15);
            entity.Property(e => e.RecommendedPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.RegistrationNo).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Draft");
            entity.Property(e => e.TotalScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.VehicleModel).HasMaxLength(100);
            entity.Property(e => e.VehicleVariant).HasMaxLength(50);
        });

        modelBuilder.Entity<ExchangeCaseImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exchange__3214EC077EFC6E2D");

            entity.HasIndex(e => e.CaseId, "IX_ExchangeImages_CaseId");

            entity.Property(e => e.ImagePath).HasMaxLength(500);
            entity.Property(e => e.ImageType).HasMaxLength(30);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Case).WithMany(p => p.ExchangeCaseImages)
                .HasForeignKey(d => d.CaseId)
                .HasConstraintName("FK__ExchangeC__CaseI__5D95E53A");
        });

        modelBuilder.Entity<ExchangeConditionSlab>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exchange__3214EC07BAD5CCE2");

            entity.Property(e => e.Adjustment).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Label).HasMaxLength(100);
            entity.Property(e => e.ScoreFrom).HasColumnType("decimal(4, 1)");
            entity.Property(e => e.ScoreTo).HasColumnType("decimal(4, 1)");
        });

        modelBuilder.Entity<ExchangeInspectionScore>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exchange__3214EC07B72FA852");

            entity.HasIndex(e => e.CaseId, "IX_ExchangeScores_CaseId");

            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Parameter).HasMaxLength(100);

            entity.HasOne(d => d.Case).WithMany(p => p.ExchangeInspectionScores)
                .HasForeignKey(d => d.CaseId)
                .HasConstraintName("FK__ExchangeI__CaseI__58D1301D");
        });

        modelBuilder.Entity<ExchangeKmSlab>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exchange__3214EC0791249DB8");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Deduction).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Label).HasMaxLength(50);
        });

        modelBuilder.Entity<ExchangeModelBasePrice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exchange__3214EC07A2016528");

            entity.Property(e => e.BasePrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.ScrapValue)
                .HasDefaultValue(5000m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.VariantName).HasMaxLength(100);
        });

        modelBuilder.Entity<ExchangeNotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exchange__3214EC0789F151A4");

            entity.ToTable("ExchangeNotificationLog");

            entity.HasIndex(e => e.CaseId, "IX_NotificationLog_CaseId");

            entity.HasIndex(e => new { e.DealerId, e.IsRead }, "IX_NotificationLog_DealerId");

            entity.Property(e => e.ActionType).HasMaxLength(30);
            entity.Property(e => e.DealerId).HasMaxLength(100);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.SentAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Case).WithMany(p => p.ExchangeNotificationLogs)
                .HasForeignKey(d => d.CaseId)
                .HasConstraintName("FK__ExchangeN__CaseI__65370702");
        });

        modelBuilder.Entity<ExchangePricingConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Exchange__3214EC07CE6A8463");

            entity.ToTable("ExchangePricingConfig");

            entity.HasIndex(e => e.ConfigKey, "UQ__Exchange__4A3067840C24229F").IsUnique();

            entity.Property(e => e.ConfigKey).HasMaxLength(100);
            entity.Property(e => e.ConfigValue).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
        });

        modelBuilder.Entity<PriceMaster>(entity =>
        {
            entity.HasKey(e => e.PriceId).HasName("PK__PriceMas__4957584F98AE03AF");

            entity.ToTable("PriceMaster");

            entity.HasIndex(e => new { e.ModelId, e.VariantId }, "IDX_PriceMaster_Model_Variant");

            entity.HasIndex(e => new { e.ModelId, e.VariantId, e.EffectiveStartDate }, "UQ_PriceMaster_Model_Variant_StartDate").IsUnique();

            entity.Property(e => e.PriceId).HasColumnName("PriceID");
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PriceListFile)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Model).WithMany(p => p.PriceMasters)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PriceMast__Model__66603565");

            entity.HasOne(d => d.Variant).WithMany(p => p.PriceMasters)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PriceMast__Varia__6754599E");
        });

        modelBuilder.Entity<RoadPrice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RoadPric__3214EC0752F40D8A");

            entity.HasIndex(e => e.CityId, "IX_RoadPrices_CityId");

            entity.HasIndex(e => e.ScootyId, "IX_RoadPrices_ScootyId");

            entity.HasIndex(e => new { e.ScootyId, e.CityId }, "UQ_RoadPrice_Scooty_City").IsUnique();

            entity.Property(e => e.ExShowroomPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.InsuranceAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.OtherCharges).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.RtoCharges).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.ScootyId).HasColumnName("ScootyID");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ValidFrom).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.City).WithMany(p => p.RoadPrices)
                .HasForeignKey(d => d.CityId)
                .HasConstraintName("FK_RoadPrices_City");

            entity.HasOne(d => d.Scooty).WithMany(p => p.RoadPrices)
                .HasForeignKey(d => d.ScootyId)
                .HasConstraintName("FK_RoadPrices_Scooty");
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__SalesOrd__C3905BAF31957483");

            entity.HasIndex(e => e.CustomerId, "IDX_SalesOrders_CustomerID");

            entity.HasIndex(e => e.ScootyId, "IDX_SalesOrders_ScootyID");

            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.ScootyId).HasColumnName("ScootyID");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Customer).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SalesOrde__Custo__6D0D32F4");

            entity.HasOne(d => d.Scooty).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.ScootyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SalesOrde__Scoot__6E01572D");
        });

        modelBuilder.Entity<ScootyInventory>(entity =>
        {
            entity.HasKey(e => e.ScootyId).HasName("PK__ScootyIn__03A960B0B6CC067B");

            entity.ToTable("ScootyInventory", tb => tb.HasTrigger("trg_ScootyInventory_StockAvailable"));

            entity.HasIndex(e => new { e.ModelId, e.VariantId }, "IDX_Scooty_Model_Variant");

            entity.HasIndex(e => new { e.ModelId, e.VariantId, e.ColourId }, "UQ_Scooty_Model_Variant_Color").IsUnique();

            entity.Property(e => e.ScootyId).HasColumnName("ScootyID");
            entity.Property(e => e.BatterySpecs)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.BrakeFront).HasMaxLength(50);
            entity.Property(e => e.BrakeRear).HasMaxLength(50);
            entity.Property(e => e.BrakingType).HasMaxLength(100);
            entity.Property(e => e.ChargingTimeHrs).HasMaxLength(50);
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ImageURL");
            entity.Property(e => e.MaxPowerKw).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Speedometer).HasMaxLength(50);
            entity.Property(e => e.StartingType).HasMaxLength(50);
            entity.Property(e => e.WheelSize).HasMaxLength(100);
            entity.Property(e => e.WheelType).HasMaxLength(50);

            entity.HasOne(d => d.Colour).WithMany(p => p.ScootyInventories)
                .HasForeignKey(d => d.ColourId)
                .HasConstraintName("FK__ScootyInv__Colou__5DCAEF64");

            entity.HasOne(d => d.Model).WithMany(p => p.ScootyInventories)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ScootyInv__Model__5BE2A6F2");

            entity.HasOne(d => d.Variant).WithMany(p => p.ScootyInventories)
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ScootyInv__Varia__5CD6CB2B");
        });

        modelBuilder.Entity<ScootySpec>(entity =>
        {
            entity.HasKey(e => e.ScootyId);

            entity.Property(e => e.ScootyId)
                .ValueGeneratedNever()
                .HasColumnName("ScootyID");
            entity.Property(e => e.BatteryWarranty).HasMaxLength(150);
            entity.Property(e => e.FuelType)
                .HasMaxLength(30)
                .HasDefaultValue("Electric");
            entity.Property(e => e.MotorWarranty).HasMaxLength(150);
            entity.Property(e => e.RidingModes).HasMaxLength(100);
            entity.Property(e => e.WaterWading).HasMaxLength(30);

            entity.HasOne(d => d.Scooty).WithOne(p => p.ScootySpec)
                .HasForeignKey<ScootySpec>(d => d.ScootyId)
                .HasConstraintName("FK_ScootySpecs_ScootyInventory");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C00566E98");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4728F9661").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105342C1BA1EB").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.EmployeeId).HasMaxLength(50);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.PasswordResetToken).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<UserLike>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserLike__3214EC0756A4D506");

            entity.HasIndex(e => new { e.UserId, e.ScootyId }, "UQ_UserLikes_User_Scooty").IsUnique();

            entity.Property(e => e.LikedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ScootyId).HasColumnName("ScootyID");
            entity.Property(e => e.UserId).HasMaxLength(100);

            entity.HasOne(d => d.Scooty).WithMany(p => p.UserLikes)
                .HasForeignKey(d => d.ScootyId)
                .HasConstraintName("FK_UserLikes_ScootyInventory");
        });

        modelBuilder.Entity<VehicleBrochure>(entity =>
        {
            entity.Property(e => e.BrochureUrl).HasMaxLength(500);
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Model).WithMany(p => p.VehicleBrochures)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_VehicleBrochures_VehicleModels");
        });

        modelBuilder.Entity<VehicleColour>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VehicleC__3214EC07F189FD2A");

            entity.Property(e => e.ColourName).HasMaxLength(50);
            entity.Property(e => e.HexCode).HasMaxLength(10);
        });

        modelBuilder.Entity<VehicleModel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VehicleM__3214EC070A7DD962");

            entity.Property(e => e.ModelName).HasMaxLength(100);
        });

        modelBuilder.Entity<VehicleReview>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VehicleR__3214EC0707642560");

            entity.HasIndex(e => e.ScootyId, "IX_VehicleReviews_ScootyID");

            entity.HasIndex(e => e.UserId, "IX_VehicleReviews_UserId");

            entity.HasIndex(e => new { e.ScootyId, e.UserId }, "UQ_Review_User_Scooty").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsApproved).HasDefaultValue(true);
            entity.Property(e => e.ReviewText).HasMaxLength(2000);
            entity.Property(e => e.ScootyId).HasColumnName("ScootyID");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UserId).HasMaxLength(100);

            entity.HasOne(d => d.Scooty).WithMany(p => p.VehicleReviews)
                .HasForeignKey(d => d.ScootyId)
                .HasConstraintName("FK_Reviews_Scooty");
        });

        modelBuilder.Entity<VehicleVariant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VehicleV__3214EC0778F2FB2C");

            entity.Property(e => e.VariantName).HasMaxLength(100);
        });

        modelBuilder.Entity<VwAreaStockSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AreaStockSummary");

            entity.Property(e => e.AreaName).HasMaxLength(200);
            entity.Property(e => e.CityName).HasMaxLength(100);
            entity.Property(e => e.ColourName).HasMaxLength(50);
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.Pincode).HasMaxLength(10);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ScootyId).HasColumnName("ScootyID");
            entity.Property(e => e.StateName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.VariantName).HasMaxLength(100);
        });

        modelBuilder.Entity<VwExchangeDashboardStat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ExchangeDashboardStats");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
