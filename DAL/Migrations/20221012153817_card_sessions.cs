using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    public partial class card_sessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_GameSessions_GameSessionId",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Cards_GameSessionId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "GameSessionId",
                table: "Cards");

            migrationBuilder.CreateTable(
                name: "CardGameSession",
                columns: table => new
                {
                    CardsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardGameSession", x => new { x.CardsId, x.SessionsId });
                    table.ForeignKey(
                        name: "FK_CardGameSession_Cards_CardsId",
                        column: x => x.CardsId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardGameSession_GameSessions_SessionsId",
                        column: x => x.SessionsId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardGameSession_SessionsId",
                table: "CardGameSession",
                column: "SessionsId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardGameSession");

            migrationBuilder.AddColumn<Guid>(
                name: "GameSessionId",
                table: "Cards",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_GameSessionId",
                table: "Cards",
                column: "GameSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_GameSessions_GameSessionId",
                table: "Cards",
                column: "GameSessionId",
                principalTable: "GameSessions",
                principalColumn: "Id");
        }
    }
}
