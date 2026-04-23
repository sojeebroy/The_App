using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace The_App.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDesignationToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyDesignation",
                table: "AspNetUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyDesignation",
                table: "AspNetUsers");
        }
    }
}
