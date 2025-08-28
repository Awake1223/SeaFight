using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeaFight.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Game",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Player1Id = table.Column<Guid>(type: "uuid", nullable: true),
                    Player2Id = table.Column<Guid>(type: "uuid", nullable: true),
                    WinnerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Game_User_Player1Id",
                        column: x => x.Player1Id,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Game_User_Player2Id",
                        column: x => x.Player2Id,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Game_User_WinnerId",
                        column: x => x.WinnerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ship",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsHorizontal = table.Column<bool>(type: "boolean", nullable: false),
                    StartX = table.Column<int>(type: "integer", nullable: false),
                    StartY = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ship", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ship_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Ship_User_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameShot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShotTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CoordinateX = table.Column<int>(type: "integer", nullable: false),
                    CoordinateY = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<string>(type: "text", nullable: false),
                    GameId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipModelId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameShot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameShot_Game_GameId",
                        column: x => x.GameId,
                        principalTable: "Game",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameShot_Ship_ShipModelId",
                        column: x => x.ShipModelId,
                        principalTable: "Ship",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GameShot_User_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Game_Player1Id",
                table: "Game",
                column: "Player1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Game_Player2Id",
                table: "Game",
                column: "Player2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Game_WinnerId",
                table: "Game",
                column: "WinnerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameShot_GameId",
                table: "GameShot",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameShot_PlayerId",
                table: "GameShot",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameShot_ShipModelId",
                table: "GameShot",
                column: "ShipModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Ship_GameId",
                table: "Ship",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Ship_PlayerId",
                table: "Ship",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameShot");

            migrationBuilder.DropTable(
                name: "Ship");

            migrationBuilder.DropTable(
                name: "Game");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
