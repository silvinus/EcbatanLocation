using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanningLocation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationOverlapIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Database-level backstop against booking the exact same studio + date range twice.
            // Partial overlaps are guarded transactionally in ReservationRepository; SQLite has no
            // native range-exclusion constraint, so this catches the identical-range duplicate case.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_Reservations_StudioId_StartDate_EndDate\" " +
                "ON \"Reservations\" (\"StudioId\", \"StartDate\", \"EndDate\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX \"IX_Reservations_StudioId_StartDate_EndDate\";");
        }
    }
}
