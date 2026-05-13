using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalSchemaFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventTaskAssignments_EventTasks_TaskId",
                table: "EventTaskAssignments");

            migrationBuilder.RenameColumn(
                name: "OwnerTitle",
                table: "EventTasks",
                newName: "OriginalTitle");

            migrationBuilder.RenameColumn(
                name: "OwnerTaskTime",
                table: "EventTasks",
                newName: "OriginalTaskTime");

            migrationBuilder.RenameColumn(
                name: "OwnerTaskLocation",
                table: "EventTasks",
                newName: "OriginalTaskLocation");

            migrationBuilder.RenameColumn(
                name: "OwnerSignupMode",
                table: "EventTasks",
                newName: "OriginalSignupMode");

            migrationBuilder.RenameColumn(
                name: "TaskId",
                table: "EventTaskAssignments",
                newName: "EventTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_EventTaskAssignments_TaskId",
                table: "EventTaskAssignments",
                newName: "IX_EventTaskAssignments_EventTaskId");

            migrationBuilder.RenameColumn(
                name: "OwnerTitle",
                table: "EventItems",
                newName: "OriginalTitle");

            migrationBuilder.RenameColumn(
                name: "OwnerSignupMode",
                table: "EventItems",
                newName: "OriginalSignupMode");

            migrationBuilder.RenameColumn(
                name: "OwnerAmount",
                table: "EventItems",
                newName: "OriginalAmount");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OriginalScheduledAt",
                table: "EventTasks",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScheduledAt",
                table: "EventTasks",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserEntityId",
                table: "EventTaskAssignments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserEntityId",
                table: "EventItemAssignments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventTaskAssignments_UserEntityId",
                table: "EventTaskAssignments",
                column: "UserEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_EventItemAssignments_UserEntityId",
                table: "EventItemAssignments",
                column: "UserEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventItemAssignments_AspNetUsers_UserEntityId",
                table: "EventItemAssignments",
                column: "UserEntityId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventTaskAssignments_AspNetUsers_UserEntityId",
                table: "EventTaskAssignments",
                column: "UserEntityId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EventTaskAssignments_EventTasks_EventTaskId",
                table: "EventTaskAssignments",
                column: "EventTaskId",
                principalTable: "EventTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventItemAssignments_AspNetUsers_UserEntityId",
                table: "EventItemAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_EventTaskAssignments_AspNetUsers_UserEntityId",
                table: "EventTaskAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_EventTaskAssignments_EventTasks_EventTaskId",
                table: "EventTaskAssignments");

            migrationBuilder.DropIndex(
                name: "IX_EventTaskAssignments_UserEntityId",
                table: "EventTaskAssignments");

            migrationBuilder.DropIndex(
                name: "IX_EventItemAssignments_UserEntityId",
                table: "EventItemAssignments");

            migrationBuilder.DropColumn(
                name: "OriginalScheduledAt",
                table: "EventTasks");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "EventTasks");

            migrationBuilder.DropColumn(
                name: "UserEntityId",
                table: "EventTaskAssignments");

            migrationBuilder.DropColumn(
                name: "UserEntityId",
                table: "EventItemAssignments");

            migrationBuilder.RenameColumn(
                name: "OriginalTitle",
                table: "EventTasks",
                newName: "OwnerTitle");

            migrationBuilder.RenameColumn(
                name: "OriginalTaskTime",
                table: "EventTasks",
                newName: "OwnerTaskTime");

            migrationBuilder.RenameColumn(
                name: "OriginalTaskLocation",
                table: "EventTasks",
                newName: "OwnerTaskLocation");

            migrationBuilder.RenameColumn(
                name: "OriginalSignupMode",
                table: "EventTasks",
                newName: "OwnerSignupMode");

            migrationBuilder.RenameColumn(
                name: "EventTaskId",
                table: "EventTaskAssignments",
                newName: "TaskId");

            migrationBuilder.RenameIndex(
                name: "IX_EventTaskAssignments_EventTaskId",
                table: "EventTaskAssignments",
                newName: "IX_EventTaskAssignments_TaskId");

            migrationBuilder.RenameColumn(
                name: "OriginalTitle",
                table: "EventItems",
                newName: "OwnerTitle");

            migrationBuilder.RenameColumn(
                name: "OriginalSignupMode",
                table: "EventItems",
                newName: "OwnerSignupMode");

            migrationBuilder.RenameColumn(
                name: "OriginalAmount",
                table: "EventItems",
                newName: "OwnerAmount");

            migrationBuilder.AddForeignKey(
                name: "FK_EventTaskAssignments_EventTasks_TaskId",
                table: "EventTaskAssignments",
                column: "TaskId",
                principalTable: "EventTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
