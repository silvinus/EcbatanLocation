using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanningLocation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationPersonLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AdultCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ChildrenUnder3Count = table.Column<int>(type: "INTEGER", nullable: false),
                    ReservationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationPersonLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationPersonLines_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationPersonLines_ReservationId",
                table: "ReservationPersonLines",
                column: "ReservationId");

            // Migrate existing data: one PersonLine per existing reservation
            migrationBuilder.Sql(@"
                INSERT INTO ReservationPersonLines (ClientType, AdultCount, ChildrenUnder3Count, ReservationId)
                SELECT ClientType, AdultCount, ChildrenUnder3Count, Id
                FROM Reservations
                WHERE AdultCount > 0 OR ChildrenUnder3Count > 0;
            ");

            migrationBuilder.DropColumn(
                name: "AdultCount",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ChildrenUnder3Count",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ClientType",
                table: "Reservations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationPersonLines");

            migrationBuilder.AddColumn<int>(
                name: "AdultCount",
                table: "Reservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ChildrenUnder3Count",
                table: "Reservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClientType",
                table: "Reservations",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
