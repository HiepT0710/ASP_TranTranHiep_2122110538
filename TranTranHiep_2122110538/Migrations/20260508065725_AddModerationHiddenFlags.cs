using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TranTranHiep_2122110538.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationHiddenFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HiddenReason",
                table: "RestaurantReviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "RestaurantReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HiddenReason",
                table: "OrderMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "OrderMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HiddenReason",
                table: "FoodReviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "FoodReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HiddenReason",
                table: "RestaurantReviews");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "RestaurantReviews");

            migrationBuilder.DropColumn(
                name: "HiddenReason",
                table: "OrderMessages");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "OrderMessages");

            migrationBuilder.DropColumn(
                name: "HiddenReason",
                table: "FoodReviews");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "FoodReviews");
        }
    }
}
