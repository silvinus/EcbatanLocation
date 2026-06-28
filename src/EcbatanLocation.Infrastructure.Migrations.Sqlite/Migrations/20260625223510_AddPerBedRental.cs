using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcbatanLocation.Infrastructure.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddPerBedRental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberOfBeds",
                table: "Studios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RentalMode",
                table: "Studios",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "PerLodging");

            migrationBuilder.AddColumn<int>(
                name: "BedCount",
                table: "Reservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfBeds",
                table: "Studios");

            migrationBuilder.DropColumn(
                name: "RentalMode",
                table: "Studios");

            migrationBuilder.DropColumn(
                name: "BedCount",
                table: "Reservations");
        }
    }
}
