using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HockeyRinkAPI.Migrations
{
    public partial class SeedSessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Get league IDs (assuming you have 6 leagues with IDs 1-6)
            migrationBuilder.Sql(@"
                INSERT INTO Sessions (Name, StartDate, EndDate, Fee, IsActive, CreatedAt, LeagueId)
                VALUES 
                    ('Leisure Spring Session', '2025-09-01 18:00:00', '2025-09-01 20:00:00', 25.00, 1, GETUTCDATE(), 1),
                    ('Bronze Evening Practice', '2025-09-03 19:00:00', '2025-09-03 21:00:00', 30.00, 1, GETUTCDATE(), 2),
                    ('Silver Weekend Game', '2025-09-05 10:00:00', '2025-09-05 12:00:00', 35.00, 1, GETUTCDATE(), 3),
                    ('Gold Tournament Prep', '2025-09-07 17:00:00', '2025-09-07 19:00:00', 40.00, 1, GETUTCDATE(), 4),
                    ('Platinum Championship', '2025-09-10 20:00:00', '2025-09-10 22:00:00', 50.00, 1, GETUTCDATE(), 5),
                    ('Diamond Elite Training', '2025-09-12 18:30:00', '2025-09-12 20:30:00', 60.00, 1, GETUTCDATE(), 6),
                    ('Leisure Fall Session', '2025-09-15 18:00:00', '2025-09-15 20:00:00', 25.00, 1, GETUTCDATE(), 1),
                    ('Bronze Skills Camp', '2025-09-17 19:00:00', '2025-09-17 21:00:00', 30.00, 1, GETUTCDATE(), 2),
                    ('Silver Power Play Practice', '2025-09-20 10:00:00', '2025-09-20 12:00:00', 35.00, 1, GETUTCDATE(), 3),
                    ('Gold Advanced Drills', '2025-09-22 17:00:00', '2025-09-22 19:00:00', 40.00, 1, GETUTCDATE(), 4);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Sessions WHERE Name LIKE '%Session%' OR Name LIKE '%Practice%' OR Name LIKE '%Game%' OR Name LIKE '%Training%' OR Name LIKE '%Camp%' OR Name LIKE '%Drills%'");
        }
    }
}