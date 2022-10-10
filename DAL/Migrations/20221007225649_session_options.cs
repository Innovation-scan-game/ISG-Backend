using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    public partial class session_options : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentRound",
                table: "GameSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RoundDurationSeconds",
                table: "GameSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rounds",
                table: "GameSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_GameSessions_GameSessionId",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Cards_GameSessionId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CurrentRound",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "RoundDurationSeconds",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Rounds",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "GameSessionId",
                table: "Cards");
        }
    }
}
